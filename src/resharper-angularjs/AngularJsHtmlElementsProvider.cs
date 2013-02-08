using System.Reflection;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Html.Impl.Html;

namespace JetBrains.ReSharper.Plugins.AngularJS
{
    [PsiComponent]
    public class AngularJsHtmlElementsProvider : HtmlDeclaredElementsProvider
    {
        public AngularJsHtmlElementsProvider(Lifetime lifetime, ISolution solution)
            : base(lifetime, solution, "JetBrains.ReSharper.Plugins.AngularJS.Resources.HtmlElements.xml", Assembly.GetExecutingAssembly(), true)
        {
        }

        public override bool IsApplicable(IPsiSourceFile file)
        {
            // TODO: Only when angularjs is referenced in the project?
            return true;
        }
    }
}