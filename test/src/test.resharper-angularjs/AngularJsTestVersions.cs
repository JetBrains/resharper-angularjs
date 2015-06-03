using System;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.AngularJS
{
    public class AngularJsTestVersions
    {
        public static readonly Version[] Versions =
        {
            new Version(1, 2, 28),
            new Version(1, 3, 15),
            new Version(1, 4, 0)
        };

        public static string GetAngularJsVersion(FileSystemPath root, Version version)
        {
            return root.Combine("angular." + version.ToString(3) + ".js").FullPath;
        }

        public static string GetTestMethodName(string testMethodName, Version version)
        {
            return testMethodName + GetProductVersion(version);
        }

        public static string GetProductVersion(Version version)
        {
            return string.Format("{0}{1}", version.Major, version.Minor);
        }
    }
}