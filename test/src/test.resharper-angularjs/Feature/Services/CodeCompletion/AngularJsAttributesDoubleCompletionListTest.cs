using JetBrains.ProjectModel;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.CodeCompletion
{
    [Category("Code Completion")]
    [TestFileExtension(HtmlProjectFileType.HTML_EXTENSION)]
    public partial class AngularJsAttributesDoubleCompletionListTest : WebCodeCompletionTestBase
    {
        protected override string RelativeTestDataPath { get { return @"CodeCompletion\Double\List"; } }

        protected override CodeCompletionTestType TestType
        {
            get { return CodeCompletionTestType.List; }
        }

        [Test]
        public void TestShowAllItemsOnDoubleCompletionWithNoPrefix() { DoNamedTest2(); }
        [Test] public void TestShowMatchingItemsOnDoubleCompletionWithPrefix() { DoNamedTest2(); }
    }
}