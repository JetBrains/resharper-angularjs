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
using System.Linq;
using System.Text;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Html;
using JetBrains.ReSharper.Psi.Html.Tree;
using JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi
{
    [SolutionComponent]
    public class InjectedPsiProvider : IndependentInjectedPsiProvider
    {
        public override bool IsApplicable(PsiLanguageType originalLanguage)
        {
            return originalLanguage.Is<HtmlLanguage>();
        }

        public override bool IsApplicableToNode(ITreeNode node, IInjectedFileContext context)
        {
            var htmlAttributeValue = node as IHtmlAttributeValue;
            if (htmlAttributeValue == null || htmlAttributeValue.LeadingQuote == null ||
                htmlAttributeValue.TrailingQuote == null)
            {
                return false;
            }

            var attribute = htmlAttributeValue.GetContainingNode<ITagAttribute>();
            if (attribute == null || !attribute.AttributeName.StartsWith("ng-", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        public override IInjectedNodeContext CreateInjectedNodeContext(IInjectedFileContext fileContext, ITreeNode originalNode)
        {
            var htmlAttributeValue = originalNode as IHtmlAttributeValue;
            if (htmlAttributeValue == null)
            {
                Logger.Fail("Original node is not IHtmlAttributeValue");
                return null;
            }

            // Ignore the attribute quotes
            var stringBuilder = new StringBuilder(htmlAttributeValue.GetTextLength() - 2);

            // TODO: What's this?
            foreach (var valueElement in htmlAttributeValue.ValueElements)
                valueElement.GetText(stringBuilder);

            var buffer = new StringBuilderBuffer(stringBuilder);
            var languageService = AngularJsLanguage.Instance.LanguageService();
            var originalStartOffset = htmlAttributeValue.LeadingQuote == null ? 0 : htmlAttributeValue.LeadingQuote.GetTextLength();
            var originalEndOffset = originalStartOffset + buffer.Length;

            return CreateInjectedFileAndContext(fileContext, originalNode, buffer, languageService, originalStartOffset, originalEndOffset);
        }

        public override void Regenerate(IndependentInjectedNodeContext nodeContext)
        {
            var htmlAttributeValue = nodeContext.OriginalContextNode as IHtmlAttributeValue;
            if (htmlAttributeValue == null)
            {
                Logger.Fail("Original node is not IHtmlAttributeValue");
                return;
            }

            if (htmlAttributeValue.LeadingQuote == null)
            {
                Logger.Fail("No leading quote");
                return;
            }

            var buffer = nodeContext.GeneratedNode.GetTextAsBuffer();

            var token =
              htmlAttributeValue.LeadingQuote.TokenTypes.ATTRIBUTE_VALUE.Create(buffer, TreeOffset.Zero,
                new TreeOffset(buffer.Length)) as IHtmlToken;

            var list = htmlAttributeValue.ValueElements.ToArray();
            if (list.Any())
                LowLevelModificationUtil.DeleteChildRange(list.First(), list.Last());

            LowLevelModificationUtil.AddChildAfter(htmlAttributeValue.LeadingQuote, token);
        }

        protected override bool CanBeGeneratedNode(ITreeNode node)
        {
            return node is IJavaScriptFile;
        }

        protected override bool CanBeOriginalNode(ITreeNode node)
        {
            return node is IHtmlAttributeValue;
        }

        public override PsiLanguageType GeneratedLanguage
        {
            get { return (PsiLanguageType) AngularJsLanguage.Instance ?? UnknownLanguage.Instance; }
        }
    }
}