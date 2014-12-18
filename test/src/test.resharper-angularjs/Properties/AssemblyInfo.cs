using System.Collections.Generic;
using System.Reflection;
using JetBrains.Application;
using JetBrains.ReSharper.Plugins.AngularJS.Psi.AngularJs;
using JetBrains.Threading;
using NUnit.Framework;


// TODO: Testing needs fixing for 9.0...

[SetUpFixture]
public class TestEnvironmentAssembly : ReSharperTestEnvironmentAssembly
{
    /// <summary>
    /// Gets the assemblies to load into test environment.
    /// Should include all assemblies which contain components.
    /// </summary>
    private static IEnumerable<Assembly> GetAssembliesToLoad()
    {
        // Test assembly
        yield return Assembly.GetExecutingAssembly();
        yield return typeof (AngularJsLanguage).Assembly;
    }

    public override void SetUp()
    {
        base.SetUp();
        ReentrancyGuard.Current.Execute(
            "LoadAssemblies",
            () => Shell.Instance.GetComponent<AssemblyManager>().LoadAssemblies(
                GetType().Name, GetAssembliesToLoad()));
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

