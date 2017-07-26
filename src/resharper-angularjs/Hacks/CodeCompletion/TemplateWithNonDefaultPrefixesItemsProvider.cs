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

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.BaseInfrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Hotspots;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Util;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Plugins.AngularJS.Hacks.LiveTemplates.Scope;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
using JetBrains.Util.Special;

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
        private static readonly Key<JetHashSet<string>> TemplateNamesKey = new Key<JetHashSet<string>>("AddedTemplateNames");
        
        private readonly PsiLanguageType language;
        private readonly ICodeCompletionItemsProvider includeTemplatesRule;
        private readonly HotspotSessionExecutor hotspotSessionExecutor;

        public TemplateWithNonDefaultPrefixesItemsProvider(PsiLanguageType language, IncludeTemplatesRule includeTemplatesRule, HotspotSessionExecutor hotspotSessionExecutor)
        {
            this.language = language;
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
            if (hotspotSessionExecutor.CurrentSession != null)
                return false;
            if (context.BasicContext.CodeCompletionType != CodeCompletionType.BasicCompletion)
                return false;
            return true;
        }

        private bool IsNormalProviderAvailable(ISpecificCodeCompletionContext context)
        {
            return includeTemplatesRule.IsAvailable(context) != null;
        }

        public override bool IsFinal
        {
            get { return true; }
        }

        protected override bool AddLookupItems(ISpecificCodeCompletionContext context, IItemsCollector collector)
        {
            var languageCaseProvider = LanguageManager.Instance.TryGetService<LanguageCaseProvider>(language);
            var templateNames = new JetHashSet<string>(languageCaseProvider.IfNotNull(cp => cp.IsCaseSensitive()
                ? StringComparer.Ordinal
                : StringComparer.OrdinalIgnoreCase));
            IEnumerable<TemplateLookupItem> templateItems = TemplateActionsUtil.GetLookupItems(context.BasicContext.TextControl, context.BasicContext.CompletionManager.Solution, false, false);

            var prefix = LiveTemplatesManager.GetPrefix(context.BasicContext.Document, context.BasicContext.CaretDocumentRange.TextRange.StartOffset, JsAllowedPrefixes.Chars);
            if (collector.Ranges == null)
            {
                var caretOffset = context.BasicContext.CaretDocumentRange.TextRange.StartOffset;
                var prefixRange = new TextRange(caretOffset - prefix.Length, caretOffset);
                collector.AddRanges(new TextLookupRanges(prefixRange, prefixRange, context.BasicContext.Document));
            }

            if (!string.IsNullOrEmpty(prefix))
            {
                var identifierMatcher = LookupUtil.CreateMatcher(prefix, context.BasicContext.IdentifierMatchingStyle);
                templateItems = templateItems.Where(item => identifierMatcher.Matches(item.Template.Shortcut));
            }

            foreach (var templateItem in templateItems)
                templateNames.Add(templateItem.DisplayName.Text);

            if (templateItems.IsEmpty())
                return false;

            context.BasicContext.PutData(TemplateNamesKey, templateNames);

            foreach (var templateItem in templateItems)
                collector.Add(templateItem);

            return true;
        }

        protected override void TransformItems(ISpecificCodeCompletionContext context, IItemsCollector collector)
        {
            JetHashSet<string> templateNames = context.BasicContext.GetData(TemplateNamesKey);
            if (templateNames == null || templateNames.Count == 0)
                return;

            List<ILookupItem> toRemove = collector.Items.Where(lookupItem => (lookupItem.IsKeyword()) && templateNames.Contains(lookupItem.GetText())).ToList();

            foreach (var lookupItem in toRemove)
                collector.Remove(lookupItem);
        }
    }
}