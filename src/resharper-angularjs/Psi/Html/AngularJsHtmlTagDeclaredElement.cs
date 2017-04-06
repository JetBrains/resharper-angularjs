using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Html;
using JetBrains.ReSharper.Psi.Html.Html;
using JetBrains.ReSharper.Psi.Html.Impl.Html;
using JetBrains.ReSharper.Psi.Html.Impl.TagPrefixes;
using JetBrains.ReSharper.Psi.Html.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.Html
{
    public class AngularJsHtmlTagDeclaredElement : IHtmlTagDeclaredElement, IAngularJsDeclaredElement
    {
        private readonly IPsiServices psiServices;
        private readonly HtmlDeclaredElementsCache declaredElementsCache;

        public AngularJsHtmlTagDeclaredElement(IPsiServices psiServices, HtmlDeclaredElementsCache declaredElementsCache,
            string shortName, IEnumerable<AttributeInfo> ownAttributes, IEnumerable<AttributeInfo> inheritedAttributes)
        {
            ShortName = shortName;
            this.psiServices = psiServices;
            this.declaredElementsCache = declaredElementsCache;
            OwnAttributes = ownAttributes;
            InheritedAttributes = inheritedAttributes;
        }

        public IPsiServices GetPsiServices()
        {
            return psiServices;
        }

        public IList<IDeclaration> GetDeclarations()
        {
            // TODO: Return proper declarations
            // Can this work? Declaration might be a comment node!?
            return EmptyList<IDeclaration>.InstanceList;
        }

        public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
        {
            // TODO: Return proper declarations
            // Can this work? Declaration might be a comment node!?
            return EmptyList<IDeclaration>.InstanceList;
        }

        public DeclaredElementType GetElementType()
        {
            return HtmlDeclaredElementType.HTML_TAG;
        }

        public XmlNode GetXMLDoc(bool inherit)
        {
            return null;
        }

        public XmlNode GetXMLDescriptionSummary(bool inherit)
        {
            return null;
        }

        public bool IsValid()
        {
            return true;
        }

        public bool IsSynthetic()
        {
            return false;
        }

        public HybridCollection<IPsiSourceFile> GetSourceFiles()
        {
            // TODO: Should be able to return source file
            return HybridCollection<IPsiSourceFile>.Empty;
        }

        public bool HasDeclarationsIn(IPsiSourceFile sourceFile)
        {
            // TODO: Should be able to return source file
            return false;
        }

        public string ShortName { get; private set; }
        public bool CaseSensitiveName { get { return false; } }
        public PsiLanguageType PresentationLanguage { get { return HtmlLanguage.Instance; } }
        public bool Obsolete { get { return false; } }
        public bool NonStandard { get { return false; } }

        public IEnumerable<AttributeInfo> GetAllowedAttributes(IPsiSourceFile sourceFile, bool strict = false)
        {
            return CollectionUtil.EnumerateAll(OwnAttributes, InheritedAttributes,
              declaredElementsCache.GetAdditionalAttributesForTag(sourceFile, this, strict));
        }

        public IType GetType(IHtmlTag treeTag)
        {
            // This is used by asp.net, to map tags to controls, I think
            return TypeFactory.CreateUnknownType(treeTag);
        }

        public TagClosingRequirement ClosingRequirement { get { return TagClosingRequirement.REGULAR_TAG_CLOSING_REQUIRED; } }

        // OwnAttributes are tag specific. InheritedAttributes are shared between tags (e.g. I18N, events, etc.)
        // The only real difference here is sorting when showing a description - own are shown before inherited
        public IEnumerable<AttributeInfo> OwnAttributes { get; private set; }
        public IEnumerable<AttributeInfo> InheritedAttributes { get; private set; }

        public bool OnlyOnce { get { return false; } }
        public bool CanBeRenamed { get { return false; } }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            var attribute = obj as AngularJsHtmlAttributeDeclaredElement;
            if (attribute == null) return false;

            // TODO: Other fields?
            return attribute.ShortName == ShortName;
        }

        public override int GetHashCode()
        {
            // TODO: Other fields?
            return ShortName.GetHashCode();
        }
    }
}