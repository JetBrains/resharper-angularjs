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
using JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs.Tree;
using JetBrains.ReSharper.Plugins.AngularJS.Psi.Html;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs.References
{
    [ReferenceProviderFactory]
    public partial class AngularJsIncludeFileReferenceProvider : AngularJsReferenceFactoryBase
    {
        private readonly IAngularJsHtmlDeclaredElementTypes elementTypes;

        public AngularJsIncludeFileReferenceProvider(IAngularJsHtmlDeclaredElementTypes elementTypes)
        {
            this.elementTypes = elementTypes;
        }

        protected override IReference[] GetReferences(ITreeNode element, IReference[] oldReferences)
        {
            if (!HasReference(element, null))
                return EmptyArray<IReference>.Instance;

            var stringLiteral = (IJavaScriptLiteralExpression)element;
            var references = PathReferenceUtil.CreatePathReferences(stringLiteral, stringLiteral, null,
                GetFolderPathReference, GetFileReference, node => node.GetStringValue(), node => node.GetUnquotedTreeTextRange('"', '\'').StartOffset.Offset);
            return references;
        }

        private static IPathReference GetFileReference(IJavaScriptLiteralExpression literal, IQualifier qualifier, IJavaScriptLiteralExpression token, TreeTextRange range)
        {
            return new AngularJsFileLateBoundReference<IJavaScriptLiteralExpression, IJavaScriptLiteralExpression>(literal, qualifier, token, range);
        }

        private static IPathReference GetFolderPathReference(IJavaScriptLiteralExpression literal, IQualifier qualifier,
            IJavaScriptLiteralExpression token, TreeTextRange range)
        {
            return new AngularJsFolderLateBoundReference<IJavaScriptLiteralExpression, IJavaScriptLiteralExpression>(literal, qualifier, token, range);
        }
    }
}