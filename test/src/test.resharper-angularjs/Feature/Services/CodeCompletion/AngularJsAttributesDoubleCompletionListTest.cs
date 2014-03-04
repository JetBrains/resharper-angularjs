using JetBrains.ProjectModel;
using JetBrains.ReSharper.HtmlTests.CodeCompletion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.CodeCompletion
{
    [Category("Code Completion")]
    [TestFileExtension(HtmlProjectFileType.HTML_EXTENSION)]
    public class AngularJsAttributesDoubleCompletionListTest : WebCodeCompletionTestBase
    {
        protected override bool ExecuteAction { get { return false; } }
        protected override string RelativeTestDataPath { get { return @"CodeCompletion\Double\List"; } }

        [Test] public void TestShowAllItemsOnDoubleCompletionWithNoPrefix() { DoNamedTest2(); }
        [Test] public void TestShowMatchingItemsOnDoubleCompletionWithPrefix() { DoNamedTest2(); }
    }
}