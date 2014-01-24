using JetBrains.DataFlow;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.Parsing;
using JetBrains.ReSharper.Psi.Parsing;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.Parsing
{
    public class AngularJsParser : JavaScriptQuickParser
    {
        public AngularJsParser(ILexer lexer)
            : base(lexer)
        {
        }

        protected override PsiLanguageType Language
        {
            get { return AngularJsLanguage.Instance; }
        }

        protected override JavaScriptTreeBuilder CreateTreeBuilder(Lifetime lifetime)
        {
            return new AngularJsTreeBuilder(myLexer, lifetime);
        }
    }
}