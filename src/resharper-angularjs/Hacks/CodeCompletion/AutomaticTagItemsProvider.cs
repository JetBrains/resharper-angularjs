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

using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.Html;
using JetBrains.ReSharper.Plugins.AngularJS.Psi.Html;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Html;
using JetBrains.Text;

namespace JetBrains.ReSharper.Plugins.AngularJS.Hacks.CodeCompletion
{
    // When HtmlReferencedItemsProvider is called for automatic completion, it
    // only adds tags that are valid at that location, as per a set of schema,
    // that can't be extended. Any custom tags added by IHtmlDeclaredElementsProvider
    // are filtered out, meaning our AngularJs custom directives are not shown in
    // automatic completion. Basic completion is fine. This provider adds our tags
    // back in for automatic completion.
    [Language(typeof(HtmlLanguage))]
    public class AutomaticTagItemsProvider : ItemsProviderOfSpecificContext<HtmlCodeCompletionContext>
    {
        protected override bool IsAvailable(HtmlCodeCompletionContext context)
        {
            return context.BasicContext.Parameters.IsAutomaticCompletion;
        }

        protected override bool AddLookupItems(HtmlCodeCompletionContext context, GroupedItemsCollector collector)
        {
            var elementProvider = context.BasicContext.Solution.GetComponent<AngularJsHtmlElementsProvider>();
            var matcher = GetIdentifierMatcher(context.BasicContext, context.Ranges);
            var symbolTable = elementProvider.GetAllTagsSymbolTable();
            symbolTable.ForAllSymbolInfos(info =>
            {
                var declaredElement = info.GetDeclaredElement();
                if (declaredElement.IsSynthetic() ||
                    !(declaredElement is IAngularJsDeclaredElement) && !matcher.Matches(context.GetDisplayNameByDeclaredElement(declaredElement)))
                {
                    return;
                }

                var lookupItem = context.CreateDeclaredElementLookupItem(info.ShortName, declaredElement);
                if (lookupItem.IsObsolete)
                {
                    lookupItem.Placement.Relevance |= (long) HtmlLookupItemRelevance.ObsoleteItem;
                    collector.Add(lookupItem.WithLowSelectionPriority());
                }
                else
                {
                    lookupItem.Placement.Relevance |= (long) HtmlLookupItemRelevance.Item;
                    collector.Add(lookupItem);
                }
            });

            return true;
        }

        private IdentifierMatcher GetIdentifierMatcher(CodeCompletionContext context, TextLookupRanges ranges)
        {
            var text = context.Document.GetText(ranges.GetPrefixRange(context));
            if (string.IsNullOrEmpty(text))
                return null;
            return LookupUtil.CreateMatcher(text, context.IdentifierMatchingStyle);
        }
    }
}