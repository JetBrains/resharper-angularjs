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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using JetBrains.Application;
using JetBrains.Build.AllAssemblies;
using JetBrains.ReSharper;
using JetBrains.ReSharper.Plugins.AngularJS;
using JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs;
using JetBrains.Threading;
using NUnit.Framework;

[SetUpFixture]
public class TestEnvironmentAssembly : ReSharperTestEnvironmentAssembly
{
    // ReSharper 8.2 doesn't know anything about .net 4.6, and throws exceptions.
    // We can teach it by overriding the default FrameworkLocationHelper class, but
    // it must be overridden before the ShellComponents are composed, and the call
    // to AssemblyManager in SetUp is too late for that. So, we add this assembly
    // into the list of product assemblies that are used by default when composing
    // the Shell container. Hacky, but at least it lets us run the tests.
    public override IApplicationDescriptor CreateApplicationDescriptor()
    {
        return new CustomApplicationDescriptor();
    }

    private class CustomApplicationDescriptor : ReSharperApplicationDescriptor
    {
        private AllAssembliesXml allAssembliesXml;

        public override AllAssembliesXml AllAssembliesXml
        {
            get
            {
                if (allAssembliesXml != null)
                    return allAssembliesXml;

                allAssembliesXml = base.AllAssembliesXml;
                var assemblies = new List<ProductAssemblyXml>(allAssembliesXml.Assemblies)
                    {
                        new ProductAssemblyXml
                        {
                            Configurations = "Common",
                            Name = typeof (Net46CompatibleFrameworkLocationHelper).Assembly.GetName().Name
                        }
                    };
                allAssembliesXml.Assemblies = assemblies.ToArray();
                return allAssembliesXml;
            }
        }
    }

    private static IEnumerable<Assembly> GetAssembliesToLoad()
    {
        yield return typeof(AngularJsLanguage).Assembly;
    }

    public override void SetUp()
    {
        var sw = Stopwatch.StartNew();

        base.SetUp();
        ReentrancyGuard.Current.Execute(
            "LoadAssemblies",
            () => Shell.Instance.GetComponent<AssemblyManager>().LoadAssemblies(
                GetType().Name, GetAssembliesToLoad()));

        Console.WriteLine("Startup took: {0}", sw.Elapsed);
    }

    public override void TearDown()
    {
        ReentrancyGuard.Current.Execute(
            "UnloadAssemblies",
            () => Shell.Instance.GetComponent<AssemblyManager>().UnloadAssemblies(
                GetType().Name, GetAssembliesToLoad()));
        base.TearDown();
    }
}

