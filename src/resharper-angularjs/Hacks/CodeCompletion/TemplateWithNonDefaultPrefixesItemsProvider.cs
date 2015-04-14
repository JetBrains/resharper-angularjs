#region license
// Copyright 2014 JetBrains s.r.o.
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

using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Plugins.AngularJS.Hacks.LiveTemplates.Scope;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.AngularJS.Hacks.CodeCompletion
{
    // ReSharper doesn't correctly handle templates with a non-default prefix, i.e.
    // it only correctly handles templates that start with characters, digits or '_'.
    // We have templates that start with '$'
    // This class ensures templates starting with '$' appear in the code completion lists.
    // It does this by wrapping the normal IncludeTemplatesRule. Whenever that class
    // declares itself unavailable, we check to see if the prefix is one that should be
    // handled, declare ourselves available, and let IncludeTemplatesRule do the rest of
    // the work
    [Language(typeof(KnownLanguage))]
    public class TemplateWithNonDefaultPrefixesItemsProvider : ItemsProviderOfSpecificContext<ISpecificCodeCompletionContext>
    {
        private readonly ICodeCompletionItemsProvider includeTemplatesRule;
        private readonly HotspotSessionExecutor hotspotSessionExecutor;

        public TemplateWithNonDefaultPrefixesItemsProvider(IncludeTemplatesRule includeTemplatesRule, HotspotSessionExecutor hotspotSessionExecutor)
        {
            this.includeTemplatesRule = includeTemplatesRule;
            this.hotspotSessionExecutor = hotspotSessionExecutor;
        }

        protected override bool IsAvailable(ISpecificCodeCompletionContext context)
        {
            if (IsNormalProviderAvailable(context))
                return false;
            if (!CanNormalProviderWork(context))
                return false;

            var prefix = LiveTemplatesManager.GetPrefix(context.BasicContext.Document, context.BasicContext.CaretDocumentRange.TextRange.StartOffset, JsAllowedPrefixes.Chars);
            return prefix.Length > 0 && prefix.TrimStart(JsAllowedPrefixes.Chars).Length != prefix.Length;
        }

        private bool CanNormalProviderWork(ISpecificCodeCompletionContext context)
        {
            if (context.BasicContext.Parameters.IsAutomaticCompletion)
                return false;
            if (hotspotSessionExecutor.CurrentSession != null)
                return false;
            return true;
        }

        private bool IsNormalProviderAvailable(ISpecificCodeCompletionContext context)
        {
            return includeTemplatesRule.IsAvailable(context) != null;
        }

        protected override void TransformItems(ISpecificCodeCompletionContext context, GroupedItemsCollector collector)
        {
            includeTemplatesRule.TransformItems(context, collector, null);
        }
    }
}