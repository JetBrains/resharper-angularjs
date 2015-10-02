﻿#region license
// Copyright 2015 JetBrains s.r.o.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Tree.JsDoc;
using JetBrains.ReSharper.Psi.JavaScript.Util.JsDoc;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.Caches
{
    public class Directive
    {
        public const string AnyTagName = "ANY";

        public readonly string OriginalName;
        public readonly string Name;
        public readonly string Restrictions;
        public readonly string[] Tags;
        public readonly int Offset;

        public Directive(string originalName, string name, string restrictions, string[] tags, int offset)
        {
            OriginalName = originalName;
            Name = name;
            Restrictions = restrictions;
            Tags = tags;
            Offset = offset;

            IsAttribute = restrictions.Contains('A');
            IsElement = restrictions.Contains('E');
            IsClass = restrictions.Contains('C');
        }

        public bool IsAttribute { get; private set; }
        public bool IsElement { get; private set; }
        public bool IsClass { get; private set; }

        public bool IsForTag(string tagName)
        {
            foreach (var t in Tags)
            {
                if (t.Equals(AnyTagName, StringComparison.InvariantCultureIgnoreCase) ||
                    t.Equals(tagName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsForTagSpecific(string tagName)
        {
            return Tags.Any(t => t.Equals(tagName, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool IsForAnyTag()
        {
            foreach (var t in Tags)
            {
                if (t.Equals(AnyTagName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        public void Write(UnsafeWriter writer)
        {
            writer.Write(OriginalName);
            writer.Write(Name);
            writer.Write(Restrictions);
            writer.Write(UnsafeWriter.StringDelegate, Tags);
            writer.Write(Offset);
        }

        public static Directive Read(UnsafeReader reader)
        {
            var originalName = reader.ReadString();
            var name = reader.ReadString();
            var restrictions = reader.ReadString();
            var tags = reader.ReadArray(UnsafeReader.StringDelegate);
            var offset = reader.ReadInt();
            return new Directive(originalName, name, restrictions, tags, offset);
        }
    }

    public class Filter
    {
        public readonly string Name;
        public readonly int Offset;

        public Filter(string name, int offset)
        {
            Name = name;
            Offset = offset;
        }

        public void Write(UnsafeWriter writer)
        {
            writer.Write(Name);
            writer.Write(Offset);
        }

        public static Filter Read(UnsafeReader reader)
        {
            var name = reader.ReadString();
            var offset = reader.ReadInt();
            return new Filter(name, offset);
        }
    }

    // All the cached items in a file, directives + filters
    public class AngularJsCacheItems
    {
        public static readonly IUnsafeMarshaller<AngularJsCacheItems> Marshaller =
            new UniversalMarshaller<AngularJsCacheItems>(Read, Write);

        private readonly IList<Directive> directives;
        private readonly IList<Filter> filters;

        public AngularJsCacheItems(IList<Directive> directives, IList<Filter> filters)
        {
            this.directives = directives;
            this.filters = filters;
        }

        public IEnumerable<Directive> Directives { get { return directives; } }
        public IEnumerable<Filter> Filters { get { return filters; } }

        public bool IsEmpty
        {
            get { return directives.Count == 0 && filters.Count == 0; }
        }

        private static AngularJsCacheItems Read(UnsafeReader reader)
        {
            var directives = reader.ReadCollection(Directive.Read, count => new List<Directive>(count));
            var filters = reader.ReadCollection(Filter.Read, count => new List<Filter>(count));
            return new AngularJsCacheItems(directives, filters);
        }

        private static void Write(UnsafeWriter writer, AngularJsCacheItems value)
        {
            writer.Write<Directive, ICollection<Directive>>((w, directive) => directive.Write(w), value.Directives.ToList());
            writer.Write<Filter, ICollection<Filter>>((w, filter) => filter.Write(w), value.Filters.ToList());
        }
    }

    [PsiComponent]
    public class AngularJsCache : SimpleICache<AngularJsCacheItems>
    {
        private IDictionary<IPsiSourceFile, AngularJsCacheItems> cachedItems;

        public AngularJsCache(Lifetime lifetime, IPersistentIndexManager persistentIndexManager)
            : base(lifetime, persistentIndexManager, AngularJsCacheItems.Marshaller)
        {
            // TODO: Useful for testing. Remove for release
            ClearOnLoad = true;

            CacheUpdated = new SimpleSignal(lifetime, "AngularJsCache");
        }

        public ISimpleSignal CacheUpdated { get; private set; }

        // TODO: Override Version when the format changes

        protected override bool IsApplicable(IPsiSourceFile sf)
        {
            return sf.IsLanguageSupported<JavaScriptLanguage>();
        }

        public override object Build(IPsiSourceFile sourceFile, bool isStartup)
        {
            // Walk the AST, find the comments, use comment.JsDocPsi, look for ngdoc
            // add a directive, or add a filter
            // TODO: Check if this works for JS islands in HTML
            var jsFile = sourceFile.GetDominantPsiFile<JavaScriptLanguage>() as IJavaScriptFile;
            if (jsFile == null)
                return null;

            var processor = new Processor();
            jsFile.ProcessDescendants(processor);
            return processor.CacheObject.IsEmpty ? null : processor.CacheObject;
        }

        public override void Drop(IPsiSourceFile sourceFile)
        {
            ClearCache();
            base.Drop(sourceFile);
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            ClearCache();
            base.Merge(sourceFile, builtPart);
        }

        public IEnumerable<Directive> Directives
        {
            get
            {
                if (!LoadCompleted)
                    return EmptyArray<Directive>.Instance;
                return CachedItems.Values.SelectMany(v => v.Directives);
            }
        }

        public IEnumerable<Filter> Filters
        {
            get
            {
                if (!LoadCompleted)
                    return EmptyArray<Filter>.Instance;
                return CachedItems.Values.SelectMany(v => v.Filters);
            }
        }

        private IDictionary<IPsiSourceFile, AngularJsCacheItems> CachedItems
        {
            get
            {
                if (cachedItems == null)
                    cachedItems = new ChunkHashMap<IPsiSourceFile, AngularJsCacheItems>(Map);
                return cachedItems;
            }
        }

        private void ClearCache()
        {
            cachedItems = null;
            CacheUpdated.Fire();
        }

        private class Processor : IRecursiveElementProcessor
        {
            private readonly IList<Directive> directives = new List<Directive>();
            private readonly IList<Filter> filters = new List<Filter>();
            private AngularJsCacheItems cacheObject;

            public bool InteriorShouldBeProcessed(ITreeNode element)
            {
                return true;
            }

            public void ProcessBeforeInterior(ITreeNode element)
            {
                var jsDocComment = element as IJavaScriptCommentNode;
                if (jsDocComment != null && jsDocComment.JsDocPsi != null && jsDocComment.JsDocPsi.JsDocFile != null)
                    ProcessJsDocFile(jsDocComment.JsDocPsi.JsDocFile);
            }

            private void ProcessJsDocFile(IJsDocFile jsDocFile)
            {
                ISimpleTag ngdocTag = null;
                ISimpleTag nameTag = null;
                ISimpleTag restrictTag = null;
                ISimpleTag elementTag = null;
                IList<IParameterTag> paramTags = null;

                foreach (var simpleTag in jsDocFile.GetTags<ISimpleTag>())
                {
                    if (simpleTag.Keyword == null)
                        continue;

                    if (simpleTag.Keyword.GetText() == "@ngdoc")
                        ngdocTag = simpleTag;
                    else if (simpleTag.Keyword.GetText() == "@name")
                        nameTag = simpleTag;
                    else if (simpleTag.Keyword.GetText() == "@restrict")
                        restrictTag = simpleTag;
                    else if (simpleTag.Keyword.GetText() == "@element")
                        elementTag = simpleTag;
                }

                foreach (var parameterTag in jsDocFile.GetTags<IParameterTag>())
                {
                    if (paramTags == null)
                        paramTags = new List<IParameterTag>();
                    paramTags.Add(parameterTag);
                }

                if (ngdocTag != null && nameTag != null)
                {
                    var nameValue = nameTag.DescriptionText;
                    var name = string.IsNullOrEmpty(nameValue) ? null : nameTag.DescriptionText;

                    // TODO: Should we strip off the module?
                    // What about 3rd party documented code?
                    if (!string.IsNullOrEmpty(name))
                        name = name.Substring(name.IndexOf(':') + 1);

                    var nameOffset = nameTag.GetDocumentStartOffset().TextRange.StartOffset;

                    var ngdocValue = ngdocTag.DescriptionText;
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(ngdocValue))
                    {
                        // TODO: Could support "event", "function", etc.
                        if (ngdocValue == "directive")
                        {
                            // Default is AE for 1.3 and above, just A for 1.2
                            // This why the IntelliJ plugin uses "D", and resolves when required
                            // Also checks angular version by presence of known directives for those
                            // versions
                            var restrictions = restrictTag != null ? restrictTag.DescriptionText : "AE";
                            var element = elementTag != null ? elementTag.DescriptionText : "ANY";
                            var tags = element.Split(new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries);

                            // Pull the attribute/element type from the param tag(s). Optional parameters
                            // are specified with a trailing equals sign, e.g. {string=}
                            // There can be more than one parameter, especially for E directives, e.g. textarea
                            // (Presumably these parameters are named attributes)
                            // Type can also be another directive, e.g. textarea can have an ngModel parameter
                            // Might be worth setting up special attribute types, e.g. string, expression, template
                            // For attributes, the parameter name is the same as the directive name

                            // TODO: Parameters

                            name = StringUtil.Unquote(name);
                            var formattedName = Regex.Replace(name, @"(\B[A-Z])", "-$1").ToLowerInvariant();

                            directives.Add(new Directive(name, formattedName, restrictions, tags, nameOffset));
                        }
                        else if (ngdocValue == "filter")
                        {
                            filters.Add(new Filter(name, nameOffset));
                        }
                    }
                }
            }

            public void ProcessAfterInterior(ITreeNode element)
            {
            }

            public bool ProcessingIsFinished
            {
                get { return false; }
            }

            public AngularJsCacheItems CacheObject
            {
                get
                {
                    if (cacheObject == null)
                        cacheObject = new AngularJsCacheItems(directives, filters);
                    return cacheObject;
                }
            }
        }
    }
}