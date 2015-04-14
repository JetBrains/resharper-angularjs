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
    public class AngularJsIncludeFileReferenceProvider : AngularJsReferenceFactoryBase
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

        protected override bool HasReference(ITreeNode element, IReferenceNameContainer names)
        {
            var stringLiteral = element as IJavaScriptLiteralExpression;
            if (stringLiteral == null)
                return false;

            var file = element.GetContainingFile();
            if (file == null)
                return false;

            // TODO: We can't use this, due to losing data when we reparse for code completion
            // When we start code completion, the tree node is reparsed with new text inserted,
            // so that references have something to attach to. Reparsing works with IChameleon
            // blocks that allow for resync-ing in-place. Our AngularJs nodes don't have any
            // chameleon blocks (for JS, it's the Block class - anything with braces) so we end
            // up re-parsing the file. This creates a new IFile, (in a sandbox that allows access
            // to the original file's reference provider) but it doesn't copy the user data. We
            // could theoretically look for a containing sandbox, get the context node and try
            // and get the user data there, but that just makes it feel like this is the wrong
            // solution. I think maybe this should be a reference provider for HTML, not AngularJs.
            // It would have the context of the attribute name, but should really work with the
            // injected AngularJs language, if only to see that it's a string literal
            //var originalAttributeType = file.UserData.GetData(AngularJsFileData.OriginalAttributeType);
            //if (originalAttributeType != elementTypes.AngularJsUrlType.Name)
            //    return false;

            return true;
        }
    }
}