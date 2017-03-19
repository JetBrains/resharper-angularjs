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
using System.Linq;
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
            return acceptableReferenceNames.IsEmpty() || reference.GetAllNames().Any(x => acceptableReferenceNames.Contains(x));
        }

        private void DoNamedTestForReferences(params string[] names)
        {
            SetInterestingReferences(names);
            DoNamedTest2();
        }

        [Test] public void TestInjectNumberConstant() { DoNamedTestForReferences("myConstant");}
        [Test] public void TestInjectStringConstant() { DoNamedTestForReferences("myConstant");}
        [Test] public void TestInjectFunctionConstant() { DoNamedTestForReferences("myConstantFunc");}
        [Test] public void TestInjectStringValue() { DoNamedTestForReferences("myValue");}
        [Test] public void TestInjectFunctionValue() { DoNamedTestForReferences("myValueFunc");}
        [Test] public void TestInjectFactory() { DoNamedTestForReferences("myFactory");}
        [Test] public void TestInjectService() { DoNamedTestForReferences("myService");}
        [Test] public void TestInjectProviderValue() { DoNamedTestForReferences("greeter");}

        [Test] public void TestInjectProvider() { DoNamedTestForReferences("greeter", "greeterProvider"); }

        // TODO: Decorator...

        [Test] public void TestInjectByStringLiteral() { DoNamedTestForReferences("v", "f", "s"); }

        [Test] public void TestInjectIntoController() { DoNamedTestForReferences("myFactory"); }
        [Test] public void TestInjectIntoDirective() { DoNamedTestForReferences("myFactory"); }
        [Test] public void TestInjectIntoFilter() { DoNamedTestForReferences("greet"); }
        [Test] public void TestInjectIntoAnimation() { DoNamedTestForReferences("myOpacity"); }

        [Test] public void TestInjectIntoConfig() { DoNamedTestForReferences("myConstant"); }
        [Test] public void TestInjectIntoRun() { DoNamedTestForReferences("myConstant"); }

        [Test] public void TestInjectIntoFactory() { DoNamedTestForReferences("defaultGreeting"); }
        [Test] public void TestInjectIntoService() { DoNamedTestForReferences("defaultGreeting"); }
        [Test] public void TestInjectIntoProvider() { DoNamedTestForReferences("defaultSalutation"); }

        [Test]
        public void TestInjectBuiltinServices()
        {
            SetInterestingReferences(
                "$cacheFactory", "$cacheFactoryProvider",
                "$anchorScroll", "$anchorScrollProvider",
                "$animate", "$animateProvider",
                "$templateCache");
            var path = BaseTestDataPath.Combine("angular.1.4.0.js");
            DoNamedTest2(path.FullPath);//@"..\..\..\..\angular.1.4.0.js");
        }

        [Test]
        public void TestInjectBuiltinServicesByStringLiteral()
        {
            SetInterestingReferences("l", "lp");
            DoNamedTest2(@"..\..\..\..\angular.1.4.0.js");
        }
    }
}