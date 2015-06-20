using JetBrains.ReSharper.Plugins.AngularJS.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.Html
{
    [DeclaredElementIconProvider]
    public class AngularJsDeclaredElementIconProvider : IDeclaredElementIconProvider
    {
        public IconId GetImageId(IDeclaredElement declaredElement, PsiLanguageType languageType, out bool canApplyExtensions)
        {
            canApplyExtensions = false;
            if (declaredElement is IAngularJsDeclaredElement)
                return LogoThemedIcons.Angularjs.Id;
            return null;
        }
    }
}