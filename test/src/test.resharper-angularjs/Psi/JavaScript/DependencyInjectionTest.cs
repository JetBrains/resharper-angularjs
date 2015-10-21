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

using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Cpp.Util;
using JetBrains.ReSharper.Psi.JavaScript.Impl.Resolve;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.AngularJS.Psi.JavaScript
{
    [WebApplication]
    public class DependencyInjectionTest : JavaScriptResolveTestBase
    {
        protected override string RelativeTestDataPath
        {
            get { return base.RelativeTestDataPath + @"\DependencyInjection"; }
        }

        private JetHashSet<string> acceptableReferenceNames;

        private void SetInterestingReferences(params string[] names)
        {
            acceptableReferenceNames = new JetHashSet<string>(names);
        }

        protected override bool AcceptReference(IReference reference)
        {
            var referenceExpressionReference = reference as ReferenceExpressionReference;
            if (referenceExpressionReference != null)
            {
                var owner = referenceExpressionReference.Owner.Qualifier as IReferenceExpression;
                if (owner != null)
                {
                    var acceptReference = AcceptReference(owner.Reference);
                    if (acceptReference)
                        return true;
                }
            }
            return acceptableReferenceNames.IsEmpty() || acceptableReferenceNames.ContainsAny(reference.GetAllNames());
        }

        private void DoNamedTest2(params string[] names)
        {
            SetInterestingReferences(names);
            base.DoNamedTest2();
        }

        [Test] public void TestInjectNumberConstant() { DoNamedTest2("myConstant");}
        [Test] public void TestInjectStringConstant() { DoNamedTest2("myConstant");}
        [Test] public void TestInjectFunctionConstant() { DoNamedTest2("myConstantFunc");}
        [Test] public void TestInjectStringValue() { DoNamedTest2("myValue");}
        [Test] public void TestInjectFunctionValue() { DoNamedTest2("myValueFunc");}
        [Test] public void TestInjectFactory() { DoNamedTest2("myFactory");}
        [Test] public void TestInjectService() { DoNamedTest2("myService");}
        [Test] public void TestInjectProviderValue() { DoNamedTest2("greeter");}

        // TODO: Decorator...

        [Test] public void TestInjectByStringLiteral() { DoNamedTest2("v", "f", "s"); }

        [Test] public void TestInjectIntoController() { DoNamedTest2("myFactory"); }
        [Test] public void TestInjectIntoDirective() { DoNamedTest2("myFactory"); }
        [Test] public void TestInjectIntoFilter() { DoNamedTest2("greet"); }
        [Test] public void TestInjectIntoAnimation() { DoNamedTest2("myOpacity"); }

        [Test] public void TestInjectIntoConfig() { DoNamedTest2("myConstant"); }
        [Test] public void TestInjectIntoRun() { DoNamedTest2("myConstant"); }

        [Test] public void TestInjectIntoFactory() { DoNamedTest2("defaultGreeting"); }
        [Test] public void TestInjectIntoService() { DoNamedTest2("defaultGreeting"); }
        [Test] public void TestInjectIntoProvider() { DoNamedTest2("defaultSalutation"); }
    }
}