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
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.AngularJS.Feature.Services.CodeCompletion
{
    [Category("Code Completion")]
    [TestFileExtension(HtmlProjectFileType.HTML_EXTENSION)]
    public class AngularJsAttributesDoubleCompletionListTest : WebCodeCompletionTestBase
    {
        private const string AngularJs = @"..\..\..\angular.js";

        protected override string RelativeTestDataPath { get { return @"CodeCompletion\Double\List"; } }

        protected override CodeCompletionTestType TestType
        {
            get { return CodeCompletionTestType.List; }
        }

        [Test] public void TestShowAllItemsOnDoubleCompletionWithNoPrefix() { DoNamedTest2(AngularJs); }
        [Test] public void TestShowMatchingItemsOnDoubleCompletionWithPrefix() { DoNamedTest2(AngularJs); }
    }
}