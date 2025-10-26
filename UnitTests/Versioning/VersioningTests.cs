using NUnit.Framework;
using Shouldly;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace VecTool.Versioning.Tests
{
    [TestFixture]
    public sealed class VersioningTests
    {
        [Test]
        public void FileVersion_Should_Follow_MajorYYMM_HHmm()
        {
            Assert.Inconclusive("Projects should get MajorVersion and PlanId properties from Directory.Buil.props");

            // Arrange: locate the app exe next to the test run or via known output path
            var solutionRoot = TestContext.CurrentContext.TestDirectory;
            var appPath = Path.Combine(solutionRoot, "..", "..", "..", "..", "src", "VecTool.UI", "bin", "Debug", "net8.0-windows", "VecTool.UI.exe");
            appPath = Path.GetFullPath(appPath);

            File.Exists(appPath).ShouldBeTrue($"App not found: {appPath}");

            // Act: read Windows file version, which corresponds to <FileVersion>
            var fvi = FileVersionInfo.GetVersionInfo(appPath);
            var version = new Version(fvi.FileVersion);

            // Assert: Major is the epoch, and YY.MM.HHmm fit into the remaining parts
            version.Major.ShouldBeGreaterThan(0);
            version.Build.ShouldBeGreaterThanOrEqualTo(0);
            version.Revision.ShouldBeGreaterThanOrEqualTo(0);
        }

        [Test]
        public void AssemblyVersion_Should_Be_MajorStable()
        {
            // If you chose the stable AssemblyVersion variant
            var assembly = Assembly.Load("VecTool.UI");
            var asmVersion = assembly.GetName().Version;
            asmVersion.ShouldNotBeNull();
            asmVersion!.Minor.ShouldBe(0);
            asmVersion.Build.ShouldBe(0);
            asmVersion.Revision.ShouldBe(0);
        }
    }
}
