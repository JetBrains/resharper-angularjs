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

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.Caches;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Html.Html;
using JetBrains.ReSharper.Psi.Html.Impl.Html;
using JetBrains.ReSharper.Psi.Impl.Resolve;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.Html
{
    [PsiComponent]
    public class AngularJsHtmlElementsProvider : IHtmlDeclaredElementsProvider
    {
        private readonly AngularJsCache cache;
        private readonly ISolution solution;
        private readonly object lockObject = new object();
        private readonly IHtmlAttributeValueType cdataAttributeValueType;

        public AngularJsHtmlElementsProvider(AngularJsCache cache, ISolution solution)
        {
            this.cache = cache;
            this.solution = solution;

            // TODO: Is this the right value for angular attributes?
            cdataAttributeValueType = new HtmlAttributeValueType("CDATA");
        }

        public ISymbolTable GetCommonAttributesSymbolTable()
        {
            return GetAllAttributesSymbolTable();
        }

        public IEnumerable<AttributeInfo> GetAttributeInfos(IPsiSourceFile sourceFile, IHtmlTagDeclaredElement tag, bool strict)
        {
            var psiServices = solution.GetComponent<IPsiServices>();
            var attributes = from d in cache.Directives
                where d.IsAttribute
                from n in new[] {d.Name, "x-" + d.Name, "data-" + d.Name}   // TODO: Not for attributes that don't start ng-
                select
                    new AttributeInfo(
                        new AngularJsHtmlAttributeDeclaredElement(psiServices, n, cdataAttributeValueType, null),
                        DefaultAttributeValueType.IMPLIED, null);
            return attributes.ToList();
        }

        public IHtmlTagDeclaredElement GetTag(string name)
        {
            lock (lockObject)
            {
                // TODO: Get all element directives from the cache
                return null;
            }
        }

        public ISymbolTable GetAllTagsSymbolTable()
        {
            // TODO: Return symbol table containing tags
            return EmptySymbolTable.INSTANCE;
        }

        public IEnumerable<IHtmlAttributeDeclaredElement> GetAttributes(string name)
        {
            var prefixedName = name;
            if (name.StartsWith("x-"))
                name = name.Substring(2);
            else if (name.StartsWith("data-"))
                name = name.Substring(5);

            var psiServices = solution.GetComponent<IPsiServices>();
            var attributes = from d in cache.Directives
                             where d.IsAttribute && string.Equals(d.Name, name, StringComparison.InvariantCultureIgnoreCase)
                             select new AngularJsHtmlAttributeDeclaredElement(psiServices, prefixedName, cdataAttributeValueType, null);
            return attributes.ToList();
        }

        public ISymbolTable GetAllAttributesSymbolTable()
        {
            // TODO: Cached?
            var psiServices = solution.GetComponent<IPsiServices>();
            var attributes = from d in cache.Directives
                where d.IsAttribute
                from n in new[] {d.Name, "x-" + d.Name, "data-" + d.Name}
                // TODO: Not for attributes that don't start ng-
                select new AngularJsHtmlAttributeDeclaredElement(psiServices, n, cdataAttributeValueType, null);
            return new DeclaredElementsSymbolTable<IHtmlAttributeDeclaredElement>(psiServices, attributes);
        }

        public IHtmlAttributeValueType GetAttributeValueType(string typeName)
        {
            // We don't define any value types
            return null;
        }

        public IHtmlEventDeclaredElement GetEvent(string name)
        {
            return null;
        }

        public ISymbolTable GetEventsSymbolTable()
        {
            return EmptySymbolTable.INSTANCE;
        }

        public ISymbolTable GetLegacyEventsSymbolTable()
        {
            return EmptySymbolTable.INSTANCE;
        }

        public bool IsApplicable(IPsiSourceFile file)
        {
            // TODO: Check to see if angular.js included?
            // Maybe not - if it's not included, the cache will be empty
            return true;
        }
    }
}