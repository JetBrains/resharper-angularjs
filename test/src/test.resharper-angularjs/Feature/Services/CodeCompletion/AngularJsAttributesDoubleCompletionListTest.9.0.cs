using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.CodeCompletion
{
    [Category("Code Completion")]
    public partial class AngularJsAttributesDoubleCompletionListTest
    {
        protected override CodeCompletionTestType TestType
        {
            get { return CodeCompletionTestType.List; }
        }
    }
}