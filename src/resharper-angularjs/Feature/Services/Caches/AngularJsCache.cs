#region license
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

using System.Collections.Generic;
using System.Linq;
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
        public string Name;
        public string Restrictions;
        public int Offset;

        public Directive(string name, string restrictions, int offset)
        {
            Name = name;
            Restrictions = restrictions;
            Offset = offset;
        }
    }

    public class Filter
    {
        public string Name;
        public int Offset;

        public Filter(string name, int offset)
        {
            Name = name;
            Offset = offset;
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
            var directives = reader.ReadCollection(r =>
            {
                var name = r.ReadString();
                var restrictions = r.ReadString();
                var offset = r.ReadInt();
                return new Directive(name, restrictions, offset);
            }, count => new List<Directive>(count));
            var filters = reader.ReadCollection(r =>
            {
                var name = r.ReadString();
                var offset = r.ReadInt();
                return new Filter(name, offset);
            }, count => new List<Filter>(count));
            return new AngularJsCacheItems(directives, filters);
        }

        private static void Write(UnsafeWriter writer, AngularJsCacheItems value)
        {
            writer.Write<Directive, ICollection<Directive>>((w, directive) =>
            {
                w.Write(directive.Name);
                w.Write(directive.Restrictions);
                w.Write(directive.Offset);
            }, value.Directives.ToList());
            writer.Write<Filter, ICollection<Filter>>((w, filter) =>
            {
                w.Write(filter.Name);
                w.Write(filter.Offset);
            }, value.Filters.ToList());
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
        }

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
            cachedItems = null;
            base.Drop(sourceFile);
        }

        public override void Merge(IPsiSourceFile sourceFile, object builtPart)
        {
            cachedItems = null;
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
                }

                if (ngdocTag != null && nameTag != null)
                {
                    var nameValue = nameTag.DescriptionText;
                    var name = string.IsNullOrEmpty(nameValue) ? null : nameTag.DescriptionText;
                    if (!string.IsNullOrEmpty(name))
                        name = name.Substring(name.IndexOf(':') + 1);

                    var nameOffset = nameTag.GetDocumentStartOffset().TextRange.StartOffset;

                    var ngdocValue = ngdocTag.DescriptionText;
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(ngdocValue))
                    {
                        if (ngdocValue == "directive")
                        {
                            // TODO: I don't know what "D" means. It's used in the IJ plugin, but not mentioned anywhere in the Angular docs
                            var restrictions = restrictTag != null ? restrictTag.DescriptionText : "D";

                            directives.Add(new Directive(name, restrictions, nameOffset));
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