using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Html;
using JetBrains.ReSharper.Psi.Html.Html;
using JetBrains.ReSharper.Psi.Html.Impl.Html;
using JetBrains.ReSharper.Psi.Html.Impl.TagPrefixes;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Web;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.Html
{
    public class AngularJsHtmlAttributeDeclaredElement : IHtmlAttributeDeclaredElement, IAngularJsDeclaredElement
    {
        private readonly IPsiServices psiServices;

        public AngularJsHtmlAttributeDeclaredElement(IPsiServices psiServices, string shortName,
            IHtmlAttributeValueType type, IHtmlTagDeclaredElement tag)
        {
            ShortName = shortName;
            ValueType = type;
            Tag = tag;
            this.psiServices = psiServices;
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
            return HtmlDeclaredElementType.HTML_ATTRIBUTE;
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
        public AspNetVersion? SupportedVersion { get { return null; } }
        public bool Obsolete { get { return false; } }
        public bool NonStandard { get { return false; } }
        public AttributeValueRequirement ValueRequirement { get { return AttributeValueRequirement.Required; } }
        public IHtmlAttributeValueType ValueType { get; private set; }
        public IHtmlTagDeclaredElement Tag { get; private set; }

        // Canonical DOM name, for e.g. HTML in JSX, which needs to be case sensitive onClick vs onclick
        public string DomName { get { return null; } }

        public bool CanBeRenamed { get; private set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            var attribute = obj as AngularJsHtmlAttributeDeclaredElement;
            if (attribute == null) return false;

            return attribute.ShortName == ShortName;
        }

        public override int GetHashCode()
        {
            return ShortName.GetHashCode();
        }
    }
}