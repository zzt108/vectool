using NUnit.Framework;
using Shouldly;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace VecTool.Versioning.Tests
{
    [TestFixture]
    public sealed class VersioningShapeTests
    {
        [Test]
        public void FileVersion_Should_Use_Major_PlanId_PlanPhaseAndTime()
        {
            Assert.Inconclusive("Projects should get MajorVersion and PlanId properties from Directory.Buil.props");
            var exe = FindExecutable("VecTool.UI.exe");
            var fvi = FileVersionInfo.GetVersionInfo(exe);
            var v = new Version(fvi.FileVersion);

            v.Major.ShouldBeGreaterThan(0);
            v.Minor.ShouldBeGreaterThanOrEqualTo(0);
            v.Build.ShouldBeInRange(1001, 9366);   // 1*1000+001 .. 9*1000+366
            v.Revision.ShouldBeInRange(0, 2359);
        }

        [Test]
        public void AssemblyVersion_Should_Be_Stable_Major_Line()
        {
            var asm = Assembly.Load("VecTool.UI");
            var ver = asm.GetName().Version!;
            ver.Major.ShouldBeGreaterThan(0);
            ver.Minor.ShouldBe(0);
            ver.Build.ShouldBe(0);
            ver.Revision.ShouldBe(0);
        }

        [Test]
        public void InformationalVersion_Should_Contain_PlanPhase_And_Timestamp()
        {
            var asm = Assembly.Load("VecTool.UI");
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
            info.ShouldContain("p");
            info.ShouldContain("+");
        }

        private static string FindExecutable(string fileName)
        {
            var root = TestContext.CurrentContext.TestDirectory;
            var path = Path.GetFullPath(Path.Combine(root, "..", "..", "..", "..", "src", "VecTool.UI", "bin", "LogDebug", "net8.0-windows", fileName));
            File.Exists(path).ShouldBeTrue($"Missing: {path}");
            return path;
        }
    }
}
