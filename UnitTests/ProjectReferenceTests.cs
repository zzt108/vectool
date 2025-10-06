using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace VecTool.UnitTests
{
    [TestFixture]
    public sealed class ProjectReferenceTests
    {
        private static readonly string RepoRoot = GetRepoRoot();

        [TestCase("OaiUI\\oaiUI.csproj", new[]
        {
            @"..\Constants\Constants.csproj",
            @"..\Core\Core.csproj",
            @"..\Log\Log.csproj",
            @"..\Handlers\Handlers.csproj",
            @"..\RecentFiles\RecentFiles.csproj",
            @"..\Configuration\Configuration.csproj",
            @"..\oaiVectorStore\oaiVectorStore.csproj",
        })]
        [TestCase("oaiVectorStore\\oaiVectorStore.csproj", new[]
        {
            @"..\Constants\Constants.csproj",
            @"..\Core\Core.csproj",
            @"..\Log\Log.csproj",
            @"..\Handlers\Handlers.csproj",
            @"..\RecentFiles\RecentFiles.csproj",
            @"..\Configuration\Configuration.csproj",
        })]

        // Todo: Utilize mocking in tests more efficiently
        // TODO: Constants project should be utilized in unit tests
        [TestCase("UnitTests\\UnitTests.csproj", new[]
        {
            @"..\Constants\Constants.csproj",
            @"..\Handlers\Handlers.csproj",
            @"..\RecentFiles\RecentFiles.csproj",
            @"..\Configuration\Configuration.csproj",
            @"..\OaiUI\oaiUI.csproj",
            @"..\oaiVectorStore\oaiVectorStore.csproj",
        })]
        public void Csproj_ShouldContainExpectedProjectReferences(string csprojRelPath, string[] expectedIncludes)
        {
            var csprojPath = Path.Combine(RepoRoot, csprojRelPath);
            File.Exists(csprojPath).ShouldBeTrue($"Missing csproj: {csprojPath}"); // Prevent NU1105 by ensuring correct file presence
            var doc = XDocument.Load(csprojPath);
            var includes = doc.Descendants("ProjectReference")
                              .Select(e => (string)e.Attribute("Include"))
                              .ToArray();

            foreach (var expected in expectedIncludes)
            {
                includes.ShouldContain(expected, $"{csprojRelPath} missing ProjectReference '{expected}'");
                var referencedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(csprojPath)!, expected));
                File.Exists(referencedPath).ShouldBeTrue($"Missing referenced project file: {referencedPath}");
            }
        }

        private static string GetRepoRoot()
        {
            // Adjust if tests run from a different working directory
            var probe = Directory.GetCurrentDirectory();
            while (!File.Exists(Path.Combine(probe, "VecTool.sln")) && probe.Length > 3)
            {
                probe = Directory.GetParent(probe)!.FullName;
            }
            return probe;
        }
    }
}
