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
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.BaseRules;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Features.Intellisense.CodeCompletion.Html;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Html;
using JetBrains.ReSharper.Psi.Html.References;
using JetBrains.ReSharper.Psi.Html.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Web.Resolve;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.CodeCompletion
{
    [Language(typeof(HtmlLanguage))]
    public class AbbreviatedItemsProvider : ItemsProviderOfSpecificContext<HtmlCodeCompletionContext>
    {
        private static readonly string[] Abbreviations = {"ng-", "data-ng-", "x-ng-"};
        private static readonly Key IdentityKey = new Key("NgCodeCompletionItem");
        private static readonly object IdentityObject = new object();

        protected override bool IsAvailable(HtmlCodeCompletionContext context)
        {
            var codeCompletionType = context.BasicContext.CodeCompletionType;
            var isAvailable = codeCompletionType == CodeCompletionType.BasicCompletion
                              || codeCompletionType == CodeCompletionType.SmartCompletion
                              || context.BasicContext.Parameters.IsAutomaticCompletion;

            // Only if the current token is expected to be an attribute
            return isAvailable && context.Reference is IHtmlAttributeReference;
        }

        protected override TextLookupRanges GetDefaultRanges(HtmlCodeCompletionContext context)
        {
            return context.Ranges;
        }

        public override bool IsDynamic
        {
            get { return true; }
        }

        // So, the behaviour we want is this:
        // 1. Hide all existing *ng-* attributes from the default list
        // 2. Get prefix
        // 3. For single completion:
        //    If no prefix, just display abbreviations
        //    else if prefix is one of the abbreviations (exact match), add all items for that abbreviation
        //      (e.g. "ng-" should show all "ng-*" items)
        //    else if prefix pattern matches an abbreviation (inc. last char), add all items for that abbreviation
        //      (e.g. "dng-" should show all "data-ng-*" items)
        //    else if prefix pattern matches an abrreviation, add those abbreviations
        //      (e.g. "dng" should match "data-ng-" abbreviation. "ng" should match "data-ng-", "ng-" and "x-ng-")
        //    else if prefix pattern matches an item minus abbreviation, add those items for all abbreviations
        //      (e.g. "controller" should match "data-ng-controller", "ng-controller", "x-ng-controller")
        //    else if prefix pattern matches item with abrbeviation, add just those items
        //      (e.g. "ngc" matches "ng-controller" but not "data-ng-controller". "dnc" matches "data-ng-controller")
        // 4. Double completion...
        protected override bool AddLookupItems(HtmlCodeCompletionContext context,
            GroupedItemsCollector collector)
        {
            var completionPrefix = GetCompletionPrefix(context);

            var matcher = string.IsNullOrEmpty(completionPrefix) ? null : LookupUtil.CreateMatcher(completionPrefix, context.BasicContext.IdentifierMatchingStyle);

            if (IsSingleCompletion(context))
            {
                // Return value is ignored. Just a cute way of calling each of these in turn without loads of if statements
                // ReSharper disable once UnusedVariable
                var ignored = TryAddAllAbbreviations(completionPrefix, context, collector)
                    || TryAddAllItemsForExactAbbreviation(completionPrefix, context, collector)
                    || TryAddAllItemsForMatchedAbbreviation(matcher, context, collector)
                    || TryAddMatchingAbbreviations(matcher, context, collector)
                    || TryAddMatchingUnprefixedItems(completionPrefix, context, collector)
                    || TryAddMatchingItemsForMatchedAbbreviation(completionPrefix, context, collector);
            }
            else if (IsDoubleCompletion(context))
            {
                foreach (var abbreviation in Abbreviations)
                    AddAllItemsForSpecificAbbreviation(abbreviation, context, collector);
            }

            // Return true so ReSharper knows we're dynamic
            return true;
        }

        private static string GetCompletionPrefix(HtmlCodeCompletionContext context)
        {
            // InsertRange contains the "word" at the caret position, which works well for
            // most purposes, but causes us problems when the user moves the caret back on
            // itself (e.g. "ng-{caret}contr"). If we use the prefix to generate our items,
            // we only ever see the whole "word" (e.g. "ng-contr") and so miss out on other
            // expansion items we should be showing (e.g. "ng-*", "data-ng-*", etc). So,
            // we use the InsertRange start offset and the current caret position
            var range = new TextRange(context.Ranges.InsertRange.StartOffset,
                context.BasicContext.CaretDocumentRange.TextRange.StartOffset);

            return context.BasicContext.Document.GetText(range);
        }

        #region Add all abbreviations if no prefix

        private bool TryAddAllAbbreviations(string prefix, HtmlCodeCompletionContext context,
            GroupedItemsCollector collector)
        {
            if (HasNoPrefix(prefix))
            {
                AddAllAbbreviations(context, collector);
                return true;
            }
            return false;
        }

        private bool HasNoPrefix(string prefix)
        {
            return string.IsNullOrEmpty(prefix);
        }

        #endregion

        #region Add all items for an abbreviation if prefix matches exact abbreviation

        private bool TryAddAllItemsForExactAbbreviation(string prefix, HtmlCodeCompletionContext context,
            GroupedItemsCollector collector)
        {
            return MatchesExactAbbreviation(prefix)
                && AddAllItemsForSpecificAbbreviation(prefix, context, collector);
        }

        private bool MatchesExactAbbreviation(string prefix)
        {
            return Abbreviations.Any(a => a.Equals(prefix));
        }

        #endregion

        #region Add all items for an abbreviation if prefix pattern matches to end of abbreviation

        private bool TryAddAllItemsForMatchedAbbreviation(IdentifierMatcher matcher,
            HtmlCodeCompletionContext context, GroupedItemsCollector collector)
        {
            var matchedAbbreviation = GetMatchedAbbreviation(matcher);
            if (matchedAbbreviation != null)
            {
                AddAllItemsForSpecificAbbreviation(matchedAbbreviation, context, collector);
                return true;
            }
            return false;
        }

        private static string GetMatchedAbbreviation(IdentifierMatcher matcher)
        {
            if (matcher == null)
                return null;

            foreach (var abbreviation in Abbreviations)
            {
                var matches = GetMatchingIndicies(matcher, abbreviation);
                if (matches != null && matches.Length > 0)
                {
                    // TODO: This could match multiple? E.g. n- matches ng- and data-ng-
                    // This is relying on ordering of the Abbreviations
                    // How to work out best fit? Most number of matched indices in
                    // shortest string?
                    if (matches.Last() == abbreviation.Length - 1)
                        return abbreviation;
                }
            }
            return null;
        }

        #endregion

        #region Add abbreviation if prefix pattern matches abbreviation

        private bool TryAddMatchingAbbreviations(IdentifierMatcher matcher,
            HtmlCodeCompletionContext context, GroupedItemsCollector collector)
        {
            if (matcher == null)
                return false;

            var added = false;
            foreach (var abbreviation in Abbreviations)
            {
                if (matcher.Matches(abbreviation))
                    added |= AddAbbreviation(context, collector, abbreviation);
            }
            return added;
        }

        #endregion

        #region Add all items where item minus abbreviation pattern matches the prefix

        private bool TryAddMatchingUnprefixedItems(string prefix, HtmlCodeCompletionContext context,
            GroupedItemsCollector collector)
        {
            if (string.IsNullOrEmpty(prefix))
                return false;

            var added = false;
            foreach (var abbreviation in Abbreviations)
            {
                var matcher = LookupUtil.CreateMatcher(abbreviation + prefix,
                    context.BasicContext.IdentifierMatchingStyle);

                added |= AddItems(abbreviation, context, collector, matcher.Matches);
            }
            return added;
        }

        #endregion

        #region Add all items where abbreviation and item pattern match prefix

        private bool TryAddMatchingItemsForMatchedAbbreviation(string prefix, HtmlCodeCompletionContext context,
            GroupedItemsCollector collector)
        {
            if (string.IsNullOrEmpty(prefix))
                return false;

            var matcher = LookupUtil.CreateMatcher(prefix, IdentifierMatchingStyle.BeginingOfIdentifier);

            var added = false;
            foreach (var abbreviation in Abbreviations)
            {
                added |= AddItems(abbreviation, context, collector, matcher.Matches);
            }
            return added;
        }

        #endregion

        private bool AddAllItemsForSpecificAbbreviation(string abbreviation, HtmlCodeCompletionContext context,
            GroupedItemsCollector collector)
        {
            return AddItems(abbreviation, context, collector, _ => true);
        }

        private bool AddItems(string abbreviation, HtmlCodeCompletionContext context,
            GroupedItemsCollector collector, [InstantHandle] Func<string, bool> shouldAdd)
        {
            var added = false;
            var symbolTable = GetSymbolTable(context);
            symbolTable.ForAllSymbolInfos(symbol =>
            {
                var declaredElement = symbol.GetDeclaredElement();
                if (declaredElement.IsSynthetic())
                    return;

                var displayName = context.GetDisplayNameByDeclaredElement(declaredElement);
                if (displayName.StartsWith(abbreviation) && shouldAdd(displayName))
                {
                    AddItem(abbreviation, symbol.ShortName, declaredElement, context, collector);
                    added = true;
                }
            });
            return added;
        }

        private static void AddItem(string abbreviation, string name, IDeclaredElement declaredElement, HtmlCodeCompletionContext context, GroupedItemsCollector collector)
        {
            var item = new WrappedDynamicLookupItem(context.CreateDeclaredElementLookupItem(name, declaredElement));
            item.PutData(IdentityKey, IdentityObject);
            item.PutData(BaseDynamicRule.PrefixKey, abbreviation);
            SortItem(item, abbreviation, name);
            collector.Add(item);
        }

        private static void SortItem(ILookupItem item, string abbreviation, string name)
        {
            // The tilde pushes us to the bottom of the list, lexicographically, then
            // order by ng- first, then data-ng-, then x-ng-
            switch (abbreviation)
            {
                case "ng-":
                    item.Placement.OrderString = "~0" + name;
                    break;

                case "data-ng-":
                    item.Placement.OrderString = "~1" + name;
                    break;

                case "x-ng-":
                    item.Placement.OrderString = "~2" + name;
                    break;
            }
        }

        private static void AddAllAbbreviations(HtmlCodeCompletionContext context,
            GroupedItemsCollector collector)
        {
            foreach (var abbreviation in Abbreviations)
                AddAbbreviation(context, collector, abbreviation);
        }

        private static bool AddAbbreviation(HtmlCodeCompletionContext context, GroupedItemsCollector collector,
            string text)
        {
            collector.Add(CreateAbbreviatedLookupItem(text, context.Ranges, context.BasicContext));
            return true;
        }

        private static AbbreviatedTextLookupItem CreateAbbreviatedLookupItem(string text, TextLookupRanges ranges, CodeCompletionContext context)
        {
            var item = new AbbreviatedTextLookupItem(text, context);
            item.InitializeRanges(ranges, context);

            // We're a HTML item too, sort us in with the other HTML items
            item.Placement.Relevance |= (long) HtmlLookupItemRelevance.Item;
            item.Placement.OrderString = text;
            return item;
        }

        private ISymbolTable GetSymbolTable(HtmlCodeCompletionContext context)
        {
            var completeableReference = context.Reference as ICompleteableReference;
            var smartCompleteableReference = completeableReference as IHtmlSmartCompleteableReference;

            ISymbolTable symbolTable;
            if (context.BasicContext.CodeCompletionType == CodeCompletionType.SmartCompletion
                && smartCompleteableReference != null)
            {
                symbolTable = smartCompleteableReference.GetSmartCompletionSymbolTable();
            }
            else if (completeableReference != null)
                symbolTable = completeableReference.GetCompletionSymbolTable();
            else
                return EmptySymbolTable.INSTANCE;

            if (context.Reference is IHtmlAttributeReference && context.TreeNode != null)
            {
                var header = context.TreeNode.GetContainingNode<IHtmlTagHeader>();
                var node = context.BasicContext.File.FindNodeAt(context.BasicContext.Document,
                    new TextRange(context.Ranges.InsertRange.StartOffset));
                if (node != null && header != null)
                {
                    node = node.GetContainingNode<ITagAttribute>(true);
                    var existingNames = new JetHashSet<string>(
                      header.Attributes.Where(attribute => attribute != node).Select(arg => arg.AttributeName), StringComparer.OrdinalIgnoreCase);
                    symbolTable = symbolTable.Filter(new ExistingNamesFilter(existingNames));
                }
            }

            return symbolTable.Filter(new PredicateFilter(symbol =>
            {
                return Abbreviations.Any(a => symbol.ShortName.StartsWith(a));
            }));
        }

        protected override void TransformItems(HtmlCodeCompletionContext context, GroupedItemsCollector collector)
        {
            RemoveItemsToAbbreviate(collector);
        }

        private static void RemoveItemsToAbbreviate(GroupedItemsCollector collector)
        {
            var toRemove = from item in collector.Items
                let unwrappedItem = GetDeclaredElementLookupItem(item)
                where unwrappedItem != null
                      && unwrappedItem.GetData(IdentityKey) == null
                      && StartsWithAbbreviation(unwrappedItem.PreferredDeclaredElement.Element.ShortName)
                select unwrappedItem;

            foreach (var item in toRemove.ToList())
                collector.Remove(item);
        }

        private static IDeclaredElementLookupItem GetDeclaredElementLookupItem(ILookupItem item)
        {
            var wrapped = item as IWrappedLookupItem;
            if (wrapped != null)
                return wrapped.Item as IDeclaredElementLookupItem;
            return item as IDeclaredElementLookupItem;
        }

        private static bool StartsWithAbbreviation(string item)
        {
            return Abbreviations.Any(item.StartsWith);
        }

        private static bool IsSingleCompletion(ISpecificCodeCompletionContext context)
        {
            return context.BasicContext.Parameters.Multiplier == 1;
        }

        private static bool IsDoubleCompletion(ISpecificCodeCompletionContext context)
        {
            return context.BasicContext.Parameters.Multiplier == 2;
        }

        public override CompletionMode SupportedCompletionMode
        {
            get { return CompletionMode.All; }
        }

        public override EvaluationMode SupportedEvaluationMode
        {
            get { return EvaluationMode.Light | EvaluationMode.OnlyDynamicRules; }
        }

        private static int[] GetMatchingIndicies(IdentifierMatcher matcher, string abbreviation)
        {
            return matcher.MatchingIndicies(abbreviation);
        }
    }
}