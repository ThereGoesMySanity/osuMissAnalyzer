using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace OsuMissAnalyzer.Tests
{
    internal static class ProjectSourcePath
    {
        private const string myRelativePath = nameof(ProjectSourcePath) + ".cs";
        private static string lazyValue;
        public static string Value => lazyValue ??= calculatePath();

        private static string calculatePath()
        {
            string pathName = GetSourceFilePathName();
            Assert.That(pathName, Does.EndWith(myRelativePath));
            return pathName.Substring(0, pathName.Length - myRelativePath.Length);
        }
        public static string GetSourceFilePathName( [CallerFilePath] string callerFilePath = null ) //
            => callerFilePath ?? "";
    }
}