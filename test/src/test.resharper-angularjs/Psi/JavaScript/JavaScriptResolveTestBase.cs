#region license
// Copyright 2015 JetBrains s.r.o.
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
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.ReSharper.TestFramework;
using JetBrains.ReSharper.TestFramework.Components.Psi;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.JavaScript
{
    [TestFileExtension(JavaScriptProjectFileType.JS_EXTENSION)]
    public abstract class JavaScriptResolveTestBase : ReferenceTestBase
    {
        protected override string RelativeTestDataPath
        {
            get { return @"psi\JavaScript\Resolve"; }
        }

        protected override bool MergeDuplicateCandidates
        {
            get { return true; }
        }

        protected override bool AcceptReference(IReference reference)
        {
            return true;
        }

        protected override void DoTest()
        {
            Solution.GetPsiServices().GetComponent<FileDependenciesSynchronizerTest>().ManualFlushChanged();
            base.DoTest();
        }

        protected void DoNamedTest2(params string[] otherFiles)
        {
            var testMethodName2 = TestMethodName2;
            NUnit.Framework.Assert.IsNotNull(testMethodName2, "TestMethodName2 == null");
            DoTest(testMethodName2 + Extension, otherFiles);
        }

        protected override string Format(IDeclaredElement declaredElement, ISubstitution substitution, PsiLanguageType languageType,
            DeclaredElementPresenterStyle presenter, IReference reference)
        {
            if (declaredElement == null)
                return "null";

            // Output the element like it is in the QuickDoc - element name + type
            return DeclaredElementPresenter.Format(JavaScriptLanguage.Instance, XmlDocPresenterUtil.MemberPresentationStyle,
                declaredElement, EmptySubstitution.INSTANCE);
        }
    }
}