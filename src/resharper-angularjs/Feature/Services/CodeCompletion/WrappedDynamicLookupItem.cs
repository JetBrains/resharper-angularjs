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

using System.Collections.Generic;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.Match;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.ReSharper.Feature.Services.ParameterInfo;
using JetBrains.TextControl;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.CodeCompletion
{
    public class WrappedDynamicLookupItem : UserDataHolder, IWrappedLookupItem, IDescriptionProvidingLookupItem,
        IParameterInfoCandidatesProvider
    {
        public WrappedDynamicLookupItem(ILookupItem item)
        {
            Item = item;
        }

        public ILookupItem Item { get; private set; }

        private IParameterInfoCandidatesProvider ParameterInfoProvidingLookupItem
        {
            get { return Item as IParameterInfoCandidatesProvider; }
        }

        // This is the important bit!
        public bool IsDynamic
        {
            get { return true; }
        }

        #region Delegated ILookupItem implementation

        public bool AcceptIfOnlyMatched(LookupItemAcceptanceContext itemAcceptanceContext)
        {
            return Item.AcceptIfOnlyMatched(itemAcceptanceContext);
        }

        public void Accept(ITextControl textControl, TextRange nameRange, LookupItemInsertType lookupItemInsertType, Suffix suffix,
            ISolution solution, bool keepCaretStill)
        {
            Item.Accept(textControl, nameRange, lookupItemInsertType, suffix, solution, keepCaretStill);
        }

        public DocumentRange GetVisualReplaceRange (DocumentRange nameRange)
        {
            return Item.GetVisualReplaceRange(nameRange);
        }

        public bool Shrink()
        {
            return Item.Shrink();
        }

        public void Unshrink()
        {
            Item.Unshrink();
        }

        public RichText DisplayName
        {
            get { return Item.DisplayName; }
        }

        public RichText DisplayTypeName
        {
            get { return Item.DisplayTypeName; }
        }

        public IconId Image
        {
            get { return Item.Image; }
        }

        public bool CanShrink
        {
            get { return Item.CanShrink; }
        }

        public bool IgnoreSoftOnSpace
        {
            get { return Item.IgnoreSoftOnSpace; }
        }

        public int Identity
        {
            get { return Item.Identity; }
        }

        #endregion

        public IEnumerable<ICandidate> CreateCandidates()
        {
            return ParameterInfoProvidingLookupItem == null
                ? EmptyArray<ICandidate>.Instance
                : ParameterInfoProvidingLookupItem.CreateCandidates();
        }

        public bool HasCandidates
        {
            get
            {
                return ParameterInfoProvidingLookupItem != null && ParameterInfoProvidingLookupItem.HasCandidates;
            }
        }

        public RichTextBlock GetDescription()
        {
            // This would be so much easier if ReSharper better supported IWrappedLookupItem
            var descriptionProvidingLookupItem = Item as IDescriptionProvidingLookupItem;
            if (descriptionProvidingLookupItem != null)
                return descriptionProvidingLookupItem.GetDescription();

            return LookupUtil.GetDescriptionForDeclaredElementLookupItem(Item as IDeclaredElementLookupItem);
        }

        public MatchingResult Match(PrefixMatcher prefixMatcher, ITextControl textControl)
        {
            return Item.Match(prefixMatcher, textControl);
        }

        public LookupItemPlacement Placement
        {
            get { return Item.Placement; }
            set { Item.Placement = value; }
        }
    }
}