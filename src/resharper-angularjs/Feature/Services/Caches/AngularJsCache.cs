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

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.Caches
{
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
            private readonly AngularJsCacheItemsBuilder cacheItemsBuilder;
            private readonly JsDocFileProcessor jsDocFileProcessor;

            public Processor()
            {
                cacheItemsBuilder = new AngularJsCacheItemsBuilder();
                jsDocFileProcessor = new JsDocFileProcessor(cacheItemsBuilder);
            }

            public bool InteriorShouldBeProcessed(ITreeNode element)
            {
                return true;
            }

            public void ProcessBeforeInterior(ITreeNode element)
            {
                var jsDocComment = element as IJavaScriptCommentNode;
                if (jsDocComment != null && jsDocComment.JsDocPsi != null && jsDocComment.JsDocPsi.JsDocFile != null)
                    jsDocFileProcessor.ProcessJsDocFile(jsDocComment.JsDocPsi.JsDocFile);
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
                get { return cacheItemsBuilder.Build(); }
            }
        }
    }
}