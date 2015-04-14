#region license
// Copyright 2014 JetBrains s.r.o.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using JetBrains.ProjectModel;
using JetBrains.ReSharper.FeaturesTestFramework.Completion;
using JetBrains.ReSharper.HtmlTests.CodeCompletion;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.CodeCompletion
{
    [Category("Code Completion")]
    [TestFileExtension(HtmlProjectFileType.HTML_EXTENSION)]
    public partial class AngularJsAttributesCompletionListTest : WebCodeCompletionTestBase
    {
        protected override string RelativeTestDataPath { get { return @"CodeCompletion\List"; } }

        [Test] public void TestShowAbbreviationsWithNoPrefix() { DoNamedTest2(); }
        [Test] public void TestShowAbbreviationsWithMatchingPrefix() { DoNamedTest2(); }
        [Test] public void TestShowItemsWithExactAbbreviationMatch() { DoNamedTest2(); }
        [Test] public void TestShowItemsWithPatternAbbreviationMatch() { DoNamedTest2(); }
        [Test] public void TestShowItemsWithPatternNotIncludingAbbreviation() { DoNamedTest2(); }
        [Test] public void TestShowItemsWithPatternIncludingAbbreviation() { DoNamedTest2(); }
        [Test] public void TestShowItemsWithCaretInMiddleOfCompletionPrefix() { DoNamedTest2(); }
        [Test] public void TestDoesNotIncludeAttributesAlreadyUsed() { DoNamedTest2(); }
    }
}