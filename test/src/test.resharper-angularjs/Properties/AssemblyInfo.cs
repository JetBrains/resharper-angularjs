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


// TODO: Testing needs fixing for 9.0...

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

