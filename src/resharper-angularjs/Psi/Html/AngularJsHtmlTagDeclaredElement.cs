using System.Collections.Generic;
using System.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Html.Html;
using JetBrains.ReSharper.Psi.Html.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.Html
{
    public class AngularJsHtmlTagDeclaredElement : IHtmlTagDeclaredElement
    {
        public IPsiServices GetPsiServices()
        {
            throw new System.NotImplementedException();
        }

        public IList<IDeclaration> GetDeclarations()
        {
            throw new System.NotImplementedException();
        }

        public IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile)
        {
            throw new System.NotImplementedException();
        }

        public DeclaredElementType GetElementType()
        {
            throw new System.NotImplementedException();
        }

        public XmlNode GetXMLDoc(bool inherit)
        {
            throw new System.NotImplementedException();
        }

        public XmlNode GetXMLDescriptionSummary(bool inherit)
        {
            throw new System.NotImplementedException();
        }

        public bool IsValid()
        {
            throw new System.NotImplementedException();
        }

        public bool IsSynthetic()
        {
            throw new System.NotImplementedException();
        }

        public HybridCollection<IPsiSourceFile> GetSourceFiles()
        {
            throw new System.NotImplementedException();
        }

        public bool HasDeclarationsIn(IPsiSourceFile sourceFile)
        {
            throw new System.NotImplementedException();
        }

        public string ShortName { get; private set; }
        public bool CaseSensistiveName { get; private set; }
        public PsiLanguageType PresentationLanguage { get; private set; }
        public bool Obsolete { get; private set; }
        public bool NonStandard { get; private set; }
        public IEnumerable<AttributeInfo> GetAllowedAttributes(IPsiSourceFile sourceFile, bool strict = false)
        {
            throw new System.NotImplementedException();
        }

        public IType GetType(IHtmlTag treeTag)
        {
            throw new System.NotImplementedException();
        }

        public TagClosingRequirement ClosingRequirement { get; private set; }
        public IEnumerable<AttributeInfo> OwnAttributes { get; private set; }
        public IEnumerable<AttributeInfo> InheritedAttributes { get; private set; }
        public bool OnlyOnce { get; private set; }
    }
}