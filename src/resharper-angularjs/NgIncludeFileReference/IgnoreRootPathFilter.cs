using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl;
using JetBrains.ReSharper.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.AngularJS.NgIncludeFileReference
{
    public class IgnoreRootPathFilter : SimpleSymbolFilter
    {
        public override bool Accepts(IDeclaredElement declaredElement, ISubstitution substitution)
        {
            return declaredElement.ShortName != PathDeclaredElement.ROOT_NAME;
        }

        public override ResolveErrorType ErrorType
        {
            get { return FileResolveErrorType.PATH_IGNORED; }
        }
    }
}