using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi
{
    [LanguageDefinition(Name)]
    public class AngularJsLanguage : KnownLanguage
    {
        public new const string Name = "AngularJS";

        [CanBeNull, UsedImplicitly]
        public static readonly AngularJsLanguage Instance;

        private AngularJsLanguage() : base(Name, "AngularJS")
        {
        }

        protected AngularJsLanguage([NotNull] string name)
            : base(name)
        {
        }

        protected AngularJsLanguage([NotNull] string name, [NotNull] string presentableName)
            : base(name, presentableName)
        {
        }
    }
}