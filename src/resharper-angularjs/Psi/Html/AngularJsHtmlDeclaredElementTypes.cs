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

using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Html.Html;
using JetBrains.ReSharper.Psi.Html.Impl.Html;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.Html
{
    public interface IAngularJsHtmlDeclaredElementTypes : IHtmlDeclaredElementTypesBase
    {
        IHtmlAttributeValueType AngularJsUrlType { get; }
    }

    [PsiComponent]
    internal class AngularJsHtmlDeclaredElementTypes : HtmlDeclaredElementTypesBase, IAngularJsHtmlDeclaredElementTypes
    {
        public AngularJsHtmlDeclaredElementTypes(IHtmlDeclaredElementsCache elementsCache)
            : base(elementsCache)
        {
        }

        public IHtmlAttributeValueType AngularJsUrlType
        {
            get { return GetAttributeValueType("%AngularJsUrl"); }
        }

        private IHtmlAttributeValueType GetAttributeValueType(string name)
        {
            return ElementsCache.GetAttributeValueType(name, null);
        }
    }
}