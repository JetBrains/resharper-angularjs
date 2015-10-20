using System.Text.RegularExpressions;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.Caches
{
    public static class DirectiveUtil
    {
        private static readonly Regex NormaliseNameRegex = new Regex(@"(\B[A-Z])", RegexOptions.Compiled);

        public static string GetNormalisedName(string name)
        {
            return NormaliseNameRegex.Replace(name, "-$1").ToLowerInvariant();
        }
    }
}