// Path: UnitTests/ProjectReferenceTests.cs

using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace UnitTests
{
    [TestFixture]
    public sealed class ProjectReferenceTests
    {
        // Verifies expected project references in UnitTests.csproj after the October refactor.
        // Intentionally case-sensitive on Vectool.UI.csproj to catch casing drift.
        [Test]
        public void Csproj_ShouldContainExpectedProjectReferences()
        {
            // Arrange
            var csprojRelPath = Path.Combine("UnitTests", "UnitTests.csproj");
            var includes = GetProjectReferenceIncludes(csprojRelPath);

            // Expected references based on the refactored solution structure:
            // - UI renamed to Vectool.UI under OaiUI folder (case-sensitive)
            // - New modular projects: Configuration, Constants, Core, Handlers, RecentFiles, Utils, Log
            var expected = new[]
            {
                @"..\OaiUI\VecTool.UI.csproj",
                @"..\Handlers\Handlers.csproj",
                @"..\RecentFiles\RecentFiles.csproj",
                @"..\Configuration\Configuration.csproj",
                @"..\Constants\Constants.csproj",
                @"..\Core\Core.csproj",
                @"..\Utils\Utils.csproj",
                @"..\Log\Log.csproj",
            };

            // Act + Assert
            foreach (var exp in expected)
            {
                includes.ShouldContain(exp, $"UnitTests\\UnitTests.csproj missing ProjectReference '{exp}'"); // [attached_file:2]
            }

            // Guard against accidental wrong-casing regression like '..\OaiUI\vectool.UI.csproj'
            includes.Any(x => x.Equals(@"..\OaiUI\vectool.UI.csproj", StringComparison.Ordinal)).ShouldBeFalse(
                "Wrong-cased UI project reference found: '..\\OaiUI\\vectool.UI.csproj'"); // [attached_file:2]
        }

        private static IReadOnlyList<string> GetProjectReferenceIncludes(string csprojRelPath)
        {
            var csprojPath = ResolveFromRepoRoot(csprojRelPath);
            File.Exists(csprojPath).ShouldBeTrue($"Could not find csproj at '{csprojPath}'"); // [attached_file:2]

            var doc = XDocument.Load(csprojPath);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            var includes = doc
                .Descendants(ns + "ProjectReference")
                .Select(e => (string?)e.Attribute("Include"))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Cast<string>()
                .ToList();

            return includes; // [attached_file:2]
        }

        private static string ResolveFromRepoRoot(string relativePath)
        {
            // Walk up from test execution base directory until 'VecTool.sln' is found, then combine the relative path
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var sln = Path.Combine(dir.FullName, "VecTool.sln");
                if (File.Exists(sln))
                {
                    return Path.GetFullPath(Path.Combine(dir.FullName, relativePath));
                }
                dir = dir.Parent;
            }

            // Fallback to current base directory resolution if solution not found (keeps test actionable locally)
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", relativePath)); // [attached_file:2]
        }
    }
}
