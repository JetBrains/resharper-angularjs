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
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.Html
{
    [PsiComponent]
    public class AngularJsHtmlElementsProvider : IHtmlDeclaredElementsProvider
    {
        private readonly AngularJsCache cache;
        private readonly ISolution solution;
        private readonly HtmlStandardDeclaredElementsProvider standardProvider;
        private readonly object lockObject = new object();
        private readonly IHtmlAttributeValueType cdataAttributeValueType;
        private readonly Dictionary<string, IHtmlTagDeclaredElement> tags = new Dictionary<string, IHtmlTagDeclaredElement>(); 
        private readonly Dictionary<string, IHtmlAttributeDeclaredElement> attributes = new Dictionary<string, IHtmlAttributeDeclaredElement>();
        private IList<AttributeInfo> commonAttributeInfos = new List<AttributeInfo>();
        private ISymbolTable commonAttributesSymbolTable;
        private ISymbolTable allAttributesSymbolTable;
        private ISymbolTable allTagsSymbolTable;
        private JetHashSet<string> allTags;
        private JetHashSet<string> allAttributes;

        public AngularJsHtmlElementsProvider(Lifetime lifetime, AngularJsCache cache, ISolution solution, HtmlStandardDeclaredElementsProvider standardProvider)
        {
            this.cache = cache;
            this.solution = solution;
            this.standardProvider = standardProvider;

            // TODO: Finer grained caching?
            // This will clear the cache of elements whenever the AngularJs cache changes, which will be
            // every time a .js file is updated
            cache.CacheUpdated.Advise(lifetime, ClearCache);

            // TODO: Is this the right value for angular attributes?
            cdataAttributeValueType = new HtmlAttributeValueType("CDATA");
        }

        // Gets a symbol table that contains all common attributes, that is, all attributes that apply to any tag.
        // Used in attribute code completion and resolution - if the tag is unknown, show common attributes,
        // rather than specific attributes
        // The standard HtmlDeclaredElementsProvider treats attributes as specific (tag.OwnAttributes), shared
        // between multiple tags (tag.InheritedAttributes - I18N, core, asp, events, etc.) and common (additional
        // - all groups of inherited/shared/reused attributes). The only significant difference betwen OwnAttributes
        // and InheritedAttributes is the sorting when showing descriptions (important attributes, important common
        // attributes, own attributes, inherited attributes - important comes from the XML file with the descriptions).
        // This method returns all common and inherited attributes
        public ISymbolTable GetCommonAttributesSymbolTable()
        {
            lock (lockObject)
            {
                if (commonAttributesSymbolTable == null)
                {
                    var psiServices = solution.GetComponent<IPsiServices>();
                    var directives = cache.Directives;
                    var attributeDeclaredElements = from ai in GetCommonAttributeInfosLocked(directives)
                        select ai.AttributeDeclaredElement;
                    commonAttributesSymbolTable = new DeclaredElementsSymbolTable<IHtmlAttributeDeclaredElement>(psiServices, attributeDeclaredElements);
                }
                return commonAttributesSymbolTable;
            }
        }

        // Gets (additional) attributes that should apply to the tag. Should not return attributes that are
        // already known by the tag. The standard standard HtmlDeclaredElementsProvider returns only common
        // attributes.
        // Used by HtmlDeclaredElementsCache.GetAdditionalAttributesForTag so a provider can add additional
        // attributes to a known tag (e.g. angular attributes to a standard HTML tag)
        // Also used when generating descriptions for tags
        public IEnumerable<AttributeInfo> GetAttributeInfos(IPsiSourceFile sourceFile, IHtmlTagDeclaredElement tag, int offset, bool strict)
        {
            lock (lockObject)
            {
                // We have nothing to add to an angular tag - it already knows all angular tags
                if (tag is IAngularJsDeclaredElement)
                    return EmptyArray<AttributeInfo>.Instance;

                var directives = cache.Directives.ToList();

                // Note that this includes common attributes
                var attributeInfos = from d in directives
                    where d.IsAttribute && d.IsForTag(tag.ShortName)
                    from n in GetPrefixedNames(d.Name)
                    select
                        new AttributeInfo(GetOrCreateAttributeLocked(n, tag), DefaultAttributeValueType.IMPLIED, null);

                // Get any elements that have the same name as the tag, and add the parameters as attributes
                // Don't include any attributes that are aleady defined by other providers (this causes the
                // HTML description cache component to crash)
                var parameterAttributeInfos = from d in directives
                    where d.IsElement && d.Name == tag.ShortName
                    from p in d.Parameters
                    where !IsKnownCommonAttribute(p.Name)
                    from n in GetPrefixedNames(p.Name)
                    select
                        new AttributeInfo(GetOrCreateAttributeLocked(n, tag),
                            p.IsOptional ? DefaultAttributeValueType.IMPLIED : DefaultAttributeValueType.REQUIRED, null);

                // Parameter attributes take precedence over attribute directives. Only include one.
                return parameterAttributeInfos.Concat(attributeInfos)
                    .Distinct(ai => ai.AttributeDeclaredElement.ShortName).ToList();
            }
        }

        // Returns the tag with the given name. Only one provider should own a particular tag,
        // so providers other than the standard HtmlDeclaredElementsProvider should only return
        // new, non-standard HTML tags
        // Used all over the place to get a declared element for a named tag
        public IHtmlTagDeclaredElement GetTag(string name, string parentTag)
        {
            return GetTag(name);
        }

        public IHtmlTagDeclaredElement GetTag(string name)
        {
            lock (lockObject)
            {
                var directives = cache.Directives.ToList();
                var tagDeclaredElements = from d in directives
                    where d.IsElement && string.Equals(d.Name, name, StringComparison.InvariantCultureIgnoreCase) && !IsOverridingTag(d.Name)
                    select GetOrCreateTagLocked(d.Name, directives);
                return tagDeclaredElements.FirstOrDefault();
            }
        }

        // Returns a symbol table of all tags known to the provider. Only one provider should own
        // a particular tag, so providers other than the standard HtmlDeclaredElementProvider should
        // return non-standard HTML tags.
        // Used all over the place, e.g. resolution + code completion
        public ISymbolTable GetAllTagsSymbolTable()
        {
            lock (lockObject)
            {
                if (allTagsSymbolTable == null)
                {
                    var psiServices = solution.GetComponent<IPsiServices>();

                    var directives = cache.Directives.ToList();
                    var tagDeclaredElements = from d in directives
                        where d.IsElement && !IsOverridingTag(d.Name)
                        from n in GetPrefixedNames(d.Name)
                        select GetOrCreateTagLocked(n, directives);
                    allTagsSymbolTable = new DeclaredElementsSymbolTable<IHtmlTagDeclaredElement>(psiServices, tagDeclaredElements);
                }
                return allTagsSymbolTable;
            }
        }

        // Returns all attributes that match the given name, regardless of which tag they apply to.
        // Used to get particular attributes for various features, e.g. "for", "runat", etc.
        // Note that the attribute's Tag property should be correctly populated
        // TODO: Needs a test
        public IEnumerable<IHtmlAttributeDeclaredElement> GetAttributes(string name)
        {
            var originalName = name;
            name = StripPrefix(originalName);

            lock (lockObject)
            {
                var attributeDeclaredElements = from d in cache.Directives
                    where d.IsAttribute && string.Equals(d.Name, name, StringComparison.InvariantCultureIgnoreCase)
                    from t in d.Tags
                    select GetOrCreateAttributeLocked(originalName, t);
                return attributeDeclaredElements.ToList();
            }
        }

        // Returns a symbol table that contains all known attributes
        // Used to initialize the description cache, and to get the attributes for resolving a
        // tag that has a null or empty name (? don't follow the logic here)
        public ISymbolTable GetAllAttributesSymbolTable()
        {
            lock (lockObject)
            {
                if (allAttributesSymbolTable == null)
                {
                    var psiServices = solution.GetComponent<IPsiServices>();

                    var directives = cache.Directives.ToList();

                    var attributeDeclaredElements = from d in directives
                        where d.IsAttribute
                        from n in GetPrefixedNames(d.Name)
                        from t in d.Tags
                        select GetOrCreateAttributeLocked(n, t);

                    var parameterAttributeDeclaredElements = from d in directives
                        where d.IsElement
                        from p in d.Parameters
                        where !IsKnownCommonAttribute(p.Name)
                        from n in GetPrefixedNames(p.Name)
                        select GetOrCreateAttributeLocked(n, d.Name);

                    var attrs = attributeDeclaredElements.Concat(parameterAttributeDeclaredElements);
                    allAttributesSymbolTable = new DeclaredElementsSymbolTable<IHtmlAttributeDeclaredElement>(psiServices, attrs);
                }
                return allAttributesSymbolTable;
            }
        }

        // Gets the value type that corresponds to the passed in name. E.g the standard
        // HtmlDeclaredElementsProvider understands several types, named such as %Shape,
        // %CAlign, %Bool, etc., and variously populated as enum values, e.g. "True|False".
        // Known types are exposed in HtmlDeclaredElementTypesBase
        public IHtmlAttributeValueType GetAttributeValueType(string typeName)
        {
            // We don't define any value types. Perhaps we should, so that we can mark attributes
            // as containing angular expressions, etc. which might be useful for inspections
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
            // Note that file can be null
            return true;
        }

        private bool IsOverridingTag(string name)
        {
            // This is horrible. We can't override existing tags and attributes, but we calling the
            // other providers dynamically causes serious perf issues. We'll just make do with
            // caching the standard HTML attributes and hope for the best. We also can't do this
            // in the constructor, or we get a nasty circular instantiation issue. I'll file a YT
            // ticket to handle this better in a future version.
            if (allTags == null)
            {
                lock (lockObject)
                {
                    if (allTags == null)
                    {
                        allTags = standardProvider.GetAllTagsSymbolTable().Names().ToHashSet(IdentityFunc<string>.Instance, StringComparer.InvariantCultureIgnoreCase);
                    }
                }
            }
            return allTags.Contains(name);
        }

        private bool IsKnownCommonAttribute(string name)
        {
            // This is horrible. We can't override existing tags and attributes, but we calling the
            // other providers dynamically causes serious perf issues. We'll just make do with
            // caching the standard HTML attributes and hope for the best. We also can't do this
            // in the constructor, or we get a nasty circular instantiation issue. I'll file a YT
            // ticket to handle this better in a future version.
            if (allAttributes == null)
            {
                lock (lockObject)
                {
                    if (allAttributes == null)
                    {
                        allAttributes = standardProvider.GetAllAttributesSymbolTable().Names().ToHashSet(IdentityFunc<string>.Instance, StringComparer.InvariantCultureIgnoreCase);
                    }
                }
            }
            return allAttributes.Contains(name);
        }

        private static string StripPrefix(string name)
        {
            if (name.StartsWith("x-"))
                name = name.Substring(2);
            else if (name.StartsWith("data-"))
                name = name.Substring(5);
            return name;
        }

        private string[] GetPrefixedNames(string root)
        {
            if (root.StartsWith("ng-"))
                return new[] {root, "data-" + root, "x-" + root};
            return new[] {root};
        }

        private IHtmlTagDeclaredElement GetOrCreateTagLocked(string name, IList<Directive> directives)
        {
            IHtmlTagDeclaredElement tag;
            if (!tags.TryGetValue(name, out tag))
            {
                var psiServices = solution.GetComponent<IPsiServices>();
                var htmlDeclaredElementsCache = solution.GetComponent<HtmlDeclaredElementsCache>();

                var ownAttributes = new List<AttributeInfo>();
                var inheritedAttributes = new List<AttributeInfo>();

                tag = new AngularJsHtmlTagDeclaredElement(psiServices, htmlDeclaredElementsCache, name, ownAttributes, inheritedAttributes);

                // Stupid circular references. Populate the attributes after creating the tag, so
                // that the attribute can reference the tag
                var ownAttributeInfos = GetParameterAttributeInfosLocked(tag, directives)
                    .Concat(GetSpecificAttributeInfosLocked(tag, directives)
                    .Distinct(ai => ai.AttributeDeclaredElement.ShortName));
                ownAttributes.AddRange(ownAttributeInfos);

                // Only include common attributes if they're not already part of the own attributes
                var ownAttributeNames = ownAttributes.ToHashSet(a => a.AttributeDeclaredElement.ShortName);
                var inheritedAttributeInfos = GetCommonAttributeInfosLocked(directives)
                    .Where(ai => !ownAttributeNames.Contains(ai.AttributeDeclaredElement.ShortName));
                inheritedAttributes.AddRange(inheritedAttributeInfos);

                tags.Add(name, tag);
            }
            return tag;
        }

        private IEnumerable<AttributeInfo> GetParameterAttributeInfosLocked(IHtmlTagDeclaredElement tag, IEnumerable<Directive> directives)
        {
            return from d in directives
                where d.IsElement && d.Name.Equals(tag.ShortName, StringComparison.InvariantCultureIgnoreCase)
                from p in d.Parameters
                from n in GetPrefixedNames(p.Name)
                select new AttributeInfo(GetOrCreateAttributeLocked(n, tag), DefaultAttributeValueType.IMPLIED, null);
        }

        private IEnumerable<AttributeInfo> GetSpecificAttributeInfosLocked(IHtmlTagDeclaredElement tag, IEnumerable<Directive> directives)
        {
            return from d in directives
                where d.IsAttribute && d.IsForTagSpecific(tag.ShortName)
                from n in GetPrefixedNames(d.Name)
                select new AttributeInfo(GetOrCreateAttributeLocked(n, tag), DefaultAttributeValueType.IMPLIED, null);
        }

        private IEnumerable<AttributeInfo> GetCommonAttributeInfosLocked(IEnumerable<Directive> directives)
        {
            if (commonAttributeInfos == null)
            {
                commonAttributeInfos = new List<AttributeInfo>(from d in directives
                    where d.IsAttribute && d.IsForAnyTag()
                    from n in GetPrefixedNames(d.Name)
                    select
                        new AttributeInfo(GetOrCreateCommonAttributeLocked(n), DefaultAttributeValueType.IMPLIED, null));
            }
            return commonAttributeInfos;
        }

        private IHtmlAttributeDeclaredElement GetOrCreateCommonAttributeLocked(string name)
        {
            return GetOrCreateAttributeLocked(name, (IHtmlTagDeclaredElement) null);
        }

        private IHtmlAttributeDeclaredElement GetOrCreateAttributeLocked(string attributeName, string tagName)
        {
            var key = GetAttributeLookupKey(attributeName, tagName);
            var attribute = GetAttributeLocked(key);
            if (attribute != null)
                return attribute;

            IHtmlTagDeclaredElement tag = null;
            if (!tagName.Equals(Directive.AnyTagName, StringComparison.InvariantCultureIgnoreCase))
            {
                // This overload is only called when creating attributes as part of GetAttributes or
                // GetAllAttributesSymbolTable. If the caches aren't populated, we need to create an
                // attribute which has a reference to its owning tag. This might be an angular tag,
                // or a standard HTML tag. If it's a standard HTML tag, we can ask the ReSharper to
                // get it for us. If it's an angular tag, it won't already exist (or the attribute
                // will already exist, belonging to the tag), so we need to create it, at which
                // point, it will create the attribute, so we can then look it up in the cache.
                // Since creating an angular tag using GetTag doesn't cause re-entry, we can just
                // let HtmlDeclaredElementsCache do all of the work.
                //
                // Using IHtmlDeclaredElementsProvider to add elements and attributes is surprisingly
                // tricky. Perhaps I should be creating all attributes and tags at once, rather than
                // doing it piecemeal. Would definitely be nice to get more fine grained caching for that.

                var htmlDeclaredElementsCache = solution.GetComponent<HtmlDeclaredElementsCache>();
                tag = htmlDeclaredElementsCache.GetTag(null, tagName);
                attribute = GetAttributeLocked(key);
                if (attribute != null)
                    return attribute;
            }
            return CreateAndCacheAttributeLocked(key, attributeName, tag);
        }

        private IHtmlAttributeDeclaredElement GetOrCreateAttributeLocked(string attributeName, IHtmlTagDeclaredElement tag)
        {
            var key = GetAttributeLookupKey(attributeName, tag == null ? null : tag.ShortName);
            return GetAttributeLocked(key) ?? CreateAndCacheAttributeLocked(key, attributeName, tag);
        }

        private IHtmlAttributeDeclaredElement GetAttributeLocked(string key)
        {
            IHtmlAttributeDeclaredElement attribute;
            attributes.TryGetValue(key, out attribute);
            return attribute;
        }

        private IHtmlAttributeDeclaredElement CreateAndCacheAttributeLocked(string lookupKey, string attributeName,
            IHtmlTagDeclaredElement tag)
        {
            var psiServices = solution.GetComponent<IPsiServices>();
            var attribute = new AngularJsHtmlAttributeDeclaredElement(psiServices, attributeName, cdataAttributeValueType, tag);
            attributes.Add(lookupKey, attribute);
            return attribute;
        }

        private string GetAttributeLookupKey(string attributeName, string tagName)
        {
            return string.Format("{0}::{1}", tagName ?? "<ANY>", attributeName);
        }

        private void ClearCache()
        {
            lock (lockObject)
            {
                tags.Clear();
                attributes.Clear();
                commonAttributeInfos = null;
                commonAttributesSymbolTable = null;
                allAttributesSymbolTable = null;
                allTagsSymbolTable = null;
            }
        }
    }
}