using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Application;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Context;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.LiveTemplates;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Scope;
using JetBrains.ReSharper.LiveTemplates.JavaScript.LiveTemplates;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS.TemplateHacks
{
    // ReSharper doesn't correctly handle templates with a non-default prefix, i.e.
    // it only correctly handles templates that start with characters, digits or '_'.
    // We have templates that start with '$'
    // This class allows ReSharper to expand templates with the tab key. The default
    // JavaScript scope provider creates scopes that represent the current context in
    // a JS file, but doesn't provide any custom prefixes, so defaults to '_'. Here
    // we create the same scopes, but wrap them, and create a prefix that includes
    // the '$' character, so we can better match against the template shortcut
    [ShellComponent]
    public class NonDefaultPrefixJavaScriptScopeProvider : IScopeProvider
    {
        private readonly JavaScriptScopeProvider javaScriptScopeProvider;

        public NonDefaultPrefixJavaScriptScopeProvider(JavaScriptScopeProvider javaScriptScopeProvider)
        {
            this.javaScriptScopeProvider = javaScriptScopeProvider;
        }

        public IEnumerable<ITemplateScopePoint> ProvideScopePoints(TemplateAcceptanceContext context)
        {
            var prefix = LiveTemplatesManager.GetPrefix(context.Document, context.CaretOffset, JsAllowedPrefixes.Chars);
            return from scopePoint in javaScriptScopeProvider.ProvideScopePoints(context)
                   select (ITemplateScopePoint) new ScopePoint(scopePoint, prefix);
        }

        public ITemplateScopePoint ReadFromXml(XmlElement scopeElement)
        {
            return null;
        }

        public ITemplateScopePoint CreateScope(Guid scopeGuid, string typeName,
                                               IEnumerable<Pair<string, string>> customProperties)
        {
            return null;
        }

        private class ScopePoint : ITemplateScopePoint
        {
            private readonly ITemplateScopePoint innerScopePoint;

            public ScopePoint(ITemplateScopePoint innerScopePoint, string prefix)
            {
                this.innerScopePoint = innerScopePoint;

                Prefix = prefix;
            }

            public bool IsSubsetOf(ITemplateScopePoint other)
            {
                return innerScopePoint.IsSubsetOf(other);
            }

            public string CalcPrefix(IDocument document, int caretOffset)
            {
                return LiveTemplatesManager.GetPrefix(document, caretOffset, JsAllowedPrefixes.Chars);
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
}