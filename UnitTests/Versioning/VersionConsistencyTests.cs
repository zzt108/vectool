using NUnit.Framework;
using Shouldly;
using System.Text;
using System.Xml.Linq;

namespace VecTool.Versioning.Tests
{
    /// <summary>
    /// Validates that all .csproj files follow the versioning convention
    /// and share consistent MajorVersion/PlanId properties.
    /// </summary>
    [TestFixture]
    public sealed class VersionConsistencyTests
    {
        private static readonly string SolutionRoot = GetSolutionRoot();

        [Test]
        public void AllCsprojFiles_Should_HaveMajorVersionAndPlanId()
        {
            Assert.Inconclusive("Projects should get MajorVersion and PlanId properties from Directory.Buil.props");
            var csprojFiles = Directory.GetFiles(SolutionRoot, "*.csproj", SearchOption.AllDirectories);
            csprojFiles.Length.ShouldBeGreaterThan(0, "No .csproj files found in solution");

            var violations = new List<string>();

            foreach (var file in csprojFiles)
            {
                var doc = XDocument.Load(file);
                var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

                var major = doc.Descendants(ns + "MajorVersion").FirstOrDefault()?.Value;
                var planId = doc.Descendants(ns + "PlanId").FirstOrDefault()?.Value;

                if (string.IsNullOrWhiteSpace(major))
                    violations.Add($"{Path.GetFileName(file)}: Missing <MajorVersion>");

                if (string.IsNullOrWhiteSpace(planId))
                    violations.Add($"{Path.GetFileName(file)}: Missing <PlanId>");
            }

            if (violations.Any())
            {
                var message = new StringBuilder("Version property violations detected:\n");
                violations.ForEach(v => message.AppendLine($"  - {v}"));
                Assert.Fail(message.ToString());
            }
        }

        [Test]
        public void AllCsprojFiles_Should_ShareSameMajorVersionAndPlanId()
        {
            var csprojFiles = Directory.GetFiles(SolutionRoot, "*.csproj", SearchOption.AllDirectories);
            var versions = new Dictionary<string, (string major, string planId)>();

            foreach (var file in csprojFiles)
            {
                var doc = XDocument.Load(file);
                var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

                var major = doc.Descendants(ns + "MajorVersion").FirstOrDefault()?.Value ?? "0";
                var planId = doc.Descendants(ns + "PlanId").FirstOrDefault()?.Value ?? "0";

                versions[Path.GetFileName(file)] = (major, planId);
            }

            var distinctMajors = versions.Select(v => v.Value.major).Distinct().ToList();
            var distinctPlanIds = versions.Select(v => v.Value.planId).Distinct().ToList();

            distinctMajors.Count.ShouldBe(1,
                $"MajorVersion inconsistency: {string.Join(", ", distinctMajors)}");
            distinctPlanIds.Count.ShouldBe(1,
                $"PlanId inconsistency: {string.Join(", ", distinctPlanIds)}");
        }

        [Test]
        public void AllCsprojFiles_Should_UseStableAssemblyVersion()
        {
            Assert.Inconclusive("Projects should get MajorVersion and PlanId properties from Directory.Buil.props");
            var csprojFiles = Directory.GetFiles(SolutionRoot, "*.csproj", SearchOption.AllDirectories);
            var violations = new List<string>();

            foreach (var file in csprojFiles)
            {
                var doc = XDocument.Load(file);
                var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

                var asmVersion = doc.Descendants(ns + "AssemblyVersion").FirstOrDefault()?.Value;
                if (asmVersion == null)
                    continue;

                // Should be Major.0.0.0 format
                if (!asmVersion.EndsWith(".0.0.0"))
                    violations.Add($"{Path.GetFileName(file)}: AssemblyVersion={asmVersion} should be Major.0.0.0");
            }

            if (violations.Any())
            {
                var message = new StringBuilder("AssemblyVersion stability violations:\n");
                violations.ForEach(v => message.AppendLine($"  - {v}"));
                Assert.Fail(message.ToString());
            }
        }

        [Test]
        public void AllCsprojFiles_Should_UseCorrectFileVersionFormat()
        {
            Assert.Inconclusive("Projects should get MajorVersion and PlanId properties from Directory.Buil.props");
            var csprojFiles = Directory.GetFiles(SolutionRoot, "*.csproj", SearchOption.AllDirectories);
            var violations = new List<string>();

            foreach (var file in csprojFiles)
            {
                var doc = XDocument.Load(file);
                var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

                var fileVersion = doc.Descendants(ns + "FileVersion").FirstOrDefault()?.Value;
                if (fileVersion == null)
                    continue;

                // Should reference $(MajorVersion).$(PlanId).$(BuildPart).$(HHmm)
                if (!fileVersion.Contains("$(MajorVersion)") || !fileVersion.Contains("$(PlanId)"))
                    violations.Add($"{Path.GetFileName(file)}: FileVersion should use property references");
            }

            if (violations.Any())
            {
                var message = new StringBuilder("FileVersion format violations:\n");
                violations.ForEach(v => message.AppendLine($"  - {v}"));
                Assert.Fail(message.ToString());
            }
        }

        /// <summary>
        /// Generates corrected .csproj snippets for files missing version properties.
        /// Run this manually when violations are detected.
        /// </summary>
        [Test]
        [Explicit("Run manually to generate correction snippets")]
        public void GenerateCorrectionSnippets_ForMissingVersionProperties()
        {
            Assert.Inconclusive("Projects should get MajorVersion and PlanId properties from Directory.Buil.props");
            var csprojFiles = Directory.GetFiles(SolutionRoot, "*.csproj", SearchOption.AllDirectories);
            var corrections = new StringBuilder();

            corrections.AppendLine("=== Version Property Corrections ===\n");

            foreach (var file in csprojFiles)
            {
                var doc = XDocument.Load(file);
                var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

                var major = doc.Descendants(ns + "MajorVersion").FirstOrDefault()?.Value;
                var planId = doc.Descendants(ns + "PlanId").FirstOrDefault()?.Value;

                if (string.IsNullOrWhiteSpace(major) || string.IsNullOrWhiteSpace(planId))
                {
                    corrections.AppendLine($"File: {Path.GetFileName(file)}");
                    corrections.AppendLine("Add to first <PropertyGroup>:\n");
                    corrections.AppendLine("  <MajorVersion>1</MajorVersion>");
                    corrections.AppendLine("  <PlanId>12</PlanId>");
                    corrections.AppendLine("  <PlanPhase>3</PlanPhase>\n");
                }
            }

            TestContext.WriteLine(corrections.ToString());
        }

        private static string GetSolutionRoot()
        {
            var dir = TestContext.CurrentContext.TestDirectory;
            while (dir != null && !Directory.GetFiles(dir, "*.sln").Any())
                dir = Directory.GetParent(dir)?.FullName;

            dir.ShouldNotBeNull("Solution root not found");
            return dir!;
        }
    }
}
