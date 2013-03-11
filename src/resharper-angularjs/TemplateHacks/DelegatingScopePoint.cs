using System;
using System.Collections.Generic;
using System.Xml;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS.TemplateHacks
{
    internal class DelegatingScopePoint : ITemplateScopePoint
    {
        private readonly ITemplateScopePoint innerScopePoint;

        public DelegatingScopePoint(ITemplateScopePoint innerScopePoint, IDocument document, int caretOffset)
        {
            this.innerScopePoint = innerScopePoint;

            Prefix = CalcPrefix(document, caretOffset);
        }

        public bool IsSubsetOf(ITemplateScopePoint other)
        {
            return innerScopePoint.IsSubsetOf(other);
        }

        public string CalcPrefix(IDocument document, int caretOffset)
        {
            return document == null ? string.Empty : LiveTemplatesManager.GetPrefix(document, caretOffset, JsAllowedPrefixes.Chars);
        }

        public XmlElement WriteToXml(XmlElement element)
        {
            return innerScopePoint.WriteToXml(element);
        }

        public Guid GetDefaultUID()
        {
            return innerScopePoint.GetDefaultUID();
        }

        public string GetTagName()
        {
            return innerScopePoint.GetTagName();
        }

        public IEnumerable<Pair<string, string>> EnumerateCustomProperties()
        {
            return innerScopePoint.EnumerateCustomProperties();
        }

        public string Prefix { get; private set; }
        public string PresentableShortName { get { return innerScopePoint.PresentableShortName; } }
        public PsiLanguageType RelatedLanguage { get { return innerScopePoint.RelatedLanguage; } }

        public Guid UID
        {
            get { return innerScopePoint.UID; }
            set { innerScopePoint.UID = value; }
        }

        public override string ToString()
        {
            return innerScopePoint + " (with JS prefixes)";
        }
    }
}