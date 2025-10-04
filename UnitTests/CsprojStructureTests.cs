// File: UnitTests/CsprojStructureTests.cs
// Always provide complete, working code blocks
// Include proper syntax highlighting
// Specify necessary using statements
using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace UnitTests
{
    [TestFixture]
    public sealed class CsprojStructureTests
    {
        private static string RepoRoot => GetRepoRoot();

        [TestCase("UnitTests\\UnitTests.csproj", new[]
        {
            @"..\OaiUI\Vectool.UI.csproj",
            @"..\Handlers\Handlers.csproj",
            @"..\RecentFiles\RecentFiles.csproj",
            @"..\Configuration\Configuration.csproj",
            @"..\Constants\Constants.csproj",
            @"..\Core\Core.csproj",
            @"..\Utils\Utils.csproj",
            @"..\Log\Log.csproj",
        })]
        public void Csproj_ShouldContainExpectedProjectReferences(string csprojRelPath, string[] expectedIncludes)
        {
            var csprojPath = Path.Combine(RepoRoot, csprojRelPath);
            File.Exists(csprojPath).ShouldBeTrue($"Missing csproj: {csprojPath}");
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
            var probe = Directory.GetCurrentDirectory();
            while (!File.Exists(Path.Combine(probe, "VecTool.sln")) && probe.Length > 3)
            {
                probe = Directory.GetParent(probe)!.FullName;
            }
            return probe;
        }
    }
}
