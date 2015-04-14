using System;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ProjectModel.impl;
using JetBrains.ProjectModel.Test.Components;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS
{
    [ShellComponent]
    public class Net46CompatibleFrameworkLocationHelper : TestSwitchingFrameworkLocationHelper
    {
        protected override IFrameworkDetectionHelper GetDefaultHelper()
        {
            var helper = base.GetDefaultHelper();
            if (helper is SystemFrameworkLocationHelper)
                return new SystemFrameworkLocationHelperFor46();
            return new SystemFrameworkLocationHelper();
        }

        private class SystemFrameworkLocationHelperFor46 : SystemFrameworkLocationHelper, IFrameworkDetectionHelper
        {
            public override FileSystemPath GetNetFrameworkDirectory(Version version)
            {
                if (version.ToString(2) == "4.6")
                {
                    return GetPathToDotNetFrameworkV46();
                }
                return base.GetNetFrameworkDirectory(version);
            }

            public override FileSystemPath GetMsBuildDirectory(Version version)
            {
                if (version >= new Version(4, 6))
                {
                    if (!CheckForFrameworkInstallation("Microsoft\\NET Framework Setup\\NDP\\v4\\Full", "Install"))
                        return null;
                    if (!(FindRegistryValueUnderKey("Microsoft\\NET Framework Setup\\NDP\\v4\\Full", "Version") ?? "").StartsWith("4.6.", StringComparison.Ordinal))
                        return null;
                    return Lifetimes.Using(lifetime =>
                    {
                        var root = RegistryUtil.OpenSoftwareKey(lifetime);
                        if (root == null)
                            return null;
                        var registryKey = root.OpenSubKey(lifetime, "Microsoft\\MSBuild\\ToolsVersions\\14.0");
                        if (registryKey == null)
                            return null;
                        var fileSystemPath1 = FileSystemPath.TryParse(registryKey.GetValue("MSBuildToolsRoot") as string);
                        if (!fileSystemPath1.ExistsDirectory)
                            return null;
                        var fileSystemPath2 = fileSystemPath1.Combine("14.0").Combine("Bin");
                        if (!fileSystemPath2.ExistsDirectory)
                            return null;
                        return fileSystemPath2;
                    }) ?? base.GetMsBuildDirectory(new Version(4, 5));

                }
                return base.GetMsBuildDirectory(version);
            }

            Version IFrameworkDetectionHelper.GetMsBuildVersion(Version platformVersion)
            {
                return platformVersion >= new Version(4, 6) ? new Version(14, 0) : base.GetMsBuildVersion(platformVersion);
            }

            private static FileSystemPath GetPathToDotNetFrameworkV46()
            {
                if (!CheckForFrameworkInstallation("Microsoft\\NET Framework Setup\\NDP\\v4\\Full", "Install")
                    || !(FindRegistryValueUnderKey("Microsoft\\NET Framework Setup\\NDP\\v4\\Full", "Version") ?? "").StartsWith("4.6.", StringComparison.Ordinal))
                {
                    return null;
                }
                return FindDotNetFrameworkPath("v4.0", "mscorlib.dll");
            }

            private static bool CheckForFrameworkInstallation(string registryEntryToCheckInstall, string registryValueToCheckInstall)
            {
                return FindRegistryValueUnderKey(registryEntryToCheckInstall, registryValueToCheckInstall) == "1";
            }

            private static string FindRegistryValueUnderKey(string registryBaseKeyName, string registryKeyName)
            {
                return Lifetimes.Using(lifetime =>
                {
                    var registryKey1 = RegistryUtil.OpenSoftwareKey(lifetime);
                    if (registryKey1 == null)
                        return null;
                    using (var registryKey2 = registryKey1.OpenSubKey(registryBaseKeyName))
                    {
                        if (registryKey2 == null)
                            return null;
                        var obj = registryKey2.GetValue(registryKeyName);
                        return obj == null ? null : obj.ToString();
                    }
                });
            }

            private static FileSystemPath FindDotNetFrameworkPath(string prefix, string requiredFile = null)
            {
                return Lifetimes.Using(lifetime =>
                {
                    var root = RegistryUtil.OpenSoftwareKey(lifetime);
                    if (root == null)
                        return null;
                    var registryKey = root.OpenSubKey(lifetime, "Microsoft\\.NETFramework");
                    if (registryKey == null)
                        return null;
                    var path = FileSystemPath.TryParse(registryKey.GetValue("InstallRoot") as string);
                    if (!path.ExistsDirectory)
                        return null;
                    foreach (var fileSystemPath in path.GetChildDirectories())
                    {
                        if (fileSystemPath.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                            && (string.IsNullOrEmpty(requiredFile)
                            || fileSystemPath.Combine(requiredFile).ExistsFile))
                        {
                            return fileSystemPath;
                        }
                    }
                    return null;
                });
            }
        }
    }
}