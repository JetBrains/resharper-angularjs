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

using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Html.Html;
using JetBrains.ReSharper.Psi.Html.Impl.References;
using JetBrains.ReSharper.Psi.Html.Tree;
using JetBrains.ReSharper.Psi.Html.Utils;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.AngularJS.NgIncludeFileReference
{
    [ReferenceProviderFactory]
    public class AngularJsUrlAttributeValueReferenceProvider : HtmlAttributeValueReferenceFactoryBase<IAngularJsHtmlDeclaredElementTypes, HtmlProjectFileType>
    {
        public AngularJsUrlAttributeValueReferenceProvider(IAngularJsHtmlDeclaredElementTypes declaredElementTypes)
            : base(declaredElementTypes)
        {
        }

        public override bool IsApplicable(HtmlAttributeValueEntry entry)
        {
            var attribute = entry.AttributeResolution as IHtmlAttributeDeclaredElement;
            if (attribute != null)
                return Equals(attribute.ValueType, DeclaredElementTypes.AngularJsUrlType);

            return false;
        }

        protected override IReference[] CreateReferences(HtmlAttributeValueEntry entry)
        {
            return HtmlPathReferenceUtil.CreatePathAndIdReferences(entry.HtmlAttributeValue, entry.ValueToken, null,
              (element, qualifier, token, range) => new AngularJsFolderLateBoundReference<IHtmlTreeNode, IHtmlToken>(element, qualifier, token, range),
              (element, qualifier, token, range) => new AngularJsFileLateBoundReference<IHtmlTreeNode, IHtmlToken>(element, qualifier, token, range),
              (element, qualifier, token, range) => new HtmlReferenceToIdInsidePath(element, qualifier, token, range)
              );
        }
    }
}