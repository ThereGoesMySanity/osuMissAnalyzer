using System;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace OsuMissAnalyzer.Tests
{
    internal static class ProjectSourcePath
    {
        private const string myRelativePath = nameof(ProjectSourcePath) + ".cs";
        private static string? lazyValue;
        public static string Value => lazyValue ??= calculatePath();

        private static string calculatePath()
        {
            string pathName = GetSourceFilePathName();
            Assert.IsTrue(pathName.EndsWith(myRelativePath, StringComparison.Ordinal));
            return pathName.Substring(0, pathName.Length - myRelativePath.Length);
        }
        public static string GetSourceFilePathName( [CallerFilePath] string? callerFilePath = null ) //
            => callerFilePath ?? "";
    }
}