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
using JetBrains.DataFlow;
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
        private readonly Dictionary<string, IHtmlTagDeclaredElement> tagsByName = new Dictionary<string, IHtmlTagDeclaredElement>(); 
        private readonly Dictionary<string, IHtmlAttributeDeclaredElement> attributesByName = new Dictionary<string, IHtmlAttributeDeclaredElement>();
        private ISymbolTable commonAttributesSymbolTable;
        private ISymbolTable allAttributesSymbolTable;

        public AngularJsHtmlElementsProvider(Lifetime lifetime, AngularJsCache cache, ISolution solution)
        {
            this.cache = cache;
            this.solution = solution;

            // TODO: Finer grained caching?
            // This will clear the cache of elements whenever the AngularJs cache changes, which will be
            // every time a .js file is updated
            cache.CacheUpdated.Advise(lifetime, ClearCache);

            // TODO: Is this the right value for angular attributes?
            cdataAttributeValueType = new HtmlAttributeValueType("CDATA");
        }

        public ISymbolTable GetCommonAttributesSymbolTable()
        {
            lock (lockObject)
            {
                if (commonAttributesSymbolTable == null)
                {
                    var psiServices = solution.GetComponent<IPsiServices>();
                    var attributes = from d in cache.Directives
                        where d.IsAttribute && d.IsForAnyTag()
                        from n in GetPrefixedNames(d.Name)
                        select GetOrCreateAttribute(n);
                    commonAttributesSymbolTable = new DeclaredElementsSymbolTable<IHtmlAttributeDeclaredElement>(psiServices, attributes);
                }
                return commonAttributesSymbolTable;
            }
        }

        public IEnumerable<AttributeInfo> GetAttributeInfos(IPsiSourceFile sourceFile, IHtmlTagDeclaredElement tag, bool strict)
        {
            lock (lockObject)
            {
                var attributes = from d in cache.Directives
                    where d.IsAttribute && d.IsForTag(tag.ShortName)
                    from n in GetPrefixedNames(d.Name)   // TODO: Not for attributes that don't start ng-
                    select
                        new AttributeInfo(GetOrCreateAttribute(n), DefaultAttributeValueType.IMPLIED, null);
                return attributes.ToList();
            }
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

            lock (lockObject)
            {
                var psiServices = solution.GetComponent<IPsiServices>();
                var attributes = from d in cache.Directives
                    where d.IsAttribute && string.Equals(d.Name, name, StringComparison.InvariantCultureIgnoreCase)
                    select new AngularJsHtmlAttributeDeclaredElement(psiServices, prefixedName, cdataAttributeValueType, null);
                return attributes.ToList();
            }
        }

        public ISymbolTable GetAllAttributesSymbolTable()
        {
            lock (lockObject)
            {
                if (allAttributesSymbolTable == null)
                {
                    var psiServices = solution.GetComponent<IPsiServices>();
                    var attributes = from d in cache.Directives
                        where d.IsAttribute
                        from n in GetPrefixedNames(d.Name)
                        // TODO: Not for attributes that don't start ng-
                        select GetOrCreateAttribute(n);
                    allAttributesSymbolTable = new DeclaredElementsSymbolTable<IHtmlAttributeDeclaredElement>(psiServices, attributes);
                }
                return allAttributesSymbolTable;
            }
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

        private string[] GetPrefixedNames(string root)
        {
            return new[] {root, "data-" + root, "x-" + root};
        }

        private IHtmlTagDeclaredElement GetOrCreateTag(string name)
        {
            IHtmlTagDeclaredElement tag;
            if (!tagsByName.TryGetValue(name, out tag))
            {
                tag = new AngularJsHtmlTagDeclaredElement();
                tagsByName.Add(name, tag);
            }
            return tag;
        }

        private IHtmlAttributeDeclaredElement GetOrCreateAttribute(string name)
        {
            IHtmlAttributeDeclaredElement attribute;
            if (!attributesByName.TryGetValue(name, out attribute))
            {
                var psiServices = solution.GetComponent<IPsiServices>();
                // TODO: Add tag
                attribute = new AngularJsHtmlAttributeDeclaredElement(psiServices, name, cdataAttributeValueType, null);
                attributesByName.Add(name, attribute);
            }
            return attribute;
        }

        private void ClearCache()
        {
            lock (lockObject)
            {
                tagsByName.Clear();
                attributesByName.Clear();
                commonAttributesSymbolTable = null;
                allAttributesSymbolTable = null;
            }
        }
    }
}