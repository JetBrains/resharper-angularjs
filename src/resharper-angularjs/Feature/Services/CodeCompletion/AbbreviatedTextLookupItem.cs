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

using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.BaseRules;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Psi.ExpectedTypes;
using JetBrains.TextControl;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.CodeCompletion
{
    public class AbbreviatedTextLookupItem : TextLookupItem
    {
        private readonly CodeCompletionContext context;

        public AbbreviatedTextLookupItem(string text, CodeCompletionContext context)
            : base(text, true)
        {
            this.context = context;

            // When dealing with dynamic items providers, static items are simply merged into the list
            // on each keystroke. If the dynamic item provider adds dynamic items, the merging happens
            // like this:
            // 1. Get a list of existing dynamic items
            // 2. Remove any items that fail ILookupItem.Match
            // 3. Retrieve the item's BaseDynamicRule.PrefixKey value. If it exists and is longer
            //    than the current prefix, remove it
            //    (I'm not sure on the reason for this, perhaps to handle deleting chars in the prefix?)
            // 4. Loop over the new items
            //    a. If the item is ITextualLookupItem and has a BaseDynamicRule.PrefixKey value, look
            //       for the name in the (filtered) list of previous dynamic results. If it exists,
            //       don't add a new item
            //    b. Otherwise, add the item
            //
            // So, static items are always added and never removed. Dynamic items are only merged if
            // they are ITextualLookupItem and have a BaseDynamicRule.PrefixKey value. Dynamic items
            // are removed if they are ITextualLookupItem and their PrefixKey value is longer than
            // the current prefix, presumably when the prefix has chars deleted
            //
            // Which means, to allow us to remove dynamic items on each keystroke, even when adding
            // chars to the prefix, we need a dynamic item that implements ITextualLookupItem and
            // that has a really long PrefixKey value. This is a hack.
            PutData(BaseDynamicRule.PrefixKey, "really_long_string_that_shouldnt_match_so_that_the_item_is_removed");
        }

        protected override RichText GetDisplayName()
        {
            return LookupUtil.FormatLookupString(Text + "…", TextColor);
        }

        protected override void OnAfterComplete(ITextControl textControl, ref TextRange nameRange,
            ref TextRange decorationRange, TailType tailType, ref Suffix suffix,
            ref IRangeMarker caretPositionRangeMarker)
        {
            // TODO: completion with a space can break this
            base.OnAfterComplete(textControl, ref nameRange, ref decorationRange,
                tailType, ref suffix, ref caretPositionRangeMarker);

            if (context != null)
            {
                context.CompletionManager.Locks.QueueReadLock("Code completion inside markup extension",
                    () => context.CompletionManager.ExecuteManualCompletion(
                        CodeCompletionType.AutomaticCompletion, textControl, context.Solution, EmptyAction.Instance,
                        context.CompletionManager.GetPrimaryEvaluationMode(CodeCompletionType.AutomaticCompletion),
                        AutocompletionBehaviour.DoNotAutocomplete));
            }
        }
    }
}