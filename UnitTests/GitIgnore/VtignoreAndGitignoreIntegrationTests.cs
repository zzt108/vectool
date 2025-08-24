using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using DocXHandler; // For EnumerateFilesRespectingGitIgnore and VectorStoreConfig

namespace UnitTests.GitIgnore
{
    [TestFixture]
    public class VtignoreAndGitignoreIntegrationTests
    {
        private string _testRoot;
        private VectorStoreConfig _config;

        [SetUp]
        public void Setup()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), $"IgnoreTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testRoot);

            // Create a .gitignore at root
            File.WriteAllLines(Path.Combine(_testRoot, ".gitignore"), new[]
            {
                "*.tmp",
                "build/"
            });

            // Create a .vtignore in a subfolder
            var sub = Path.Combine(_testRoot, "src");
            Directory.CreateDirectory(sub);
            File.WriteAllLines(Path.Combine(sub, ".vtignore"), new[]
            {
                "!keep.tmp",  // un-ignore a specific file
                "logs/"
            });

            _config = new VectorStoreConfig(new[] { _testRoot }.ToList());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testRoot))
                Directory.Delete(_testRoot, recursive: true);
        }

        private void CreateFile(string relativePath)
        {
            var full = Path.Combine(_testRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, "content");
        }

        [Test]
        public void Should_Exclude_TmpFiles_By_Gitignore()
        {
            CreateFile("file.tmp");
            CreateFile("file.txt");

            var files = _testRoot
                .EnumerateFilesRespectingGitIgnore(_config)
                .Select(Path.GetFileName)
                .ToList();

            files.ShouldNotContain("file.tmp");
            files.ShouldContain("file.txt");
        }

        [Test]
        public void Should_Exclude_BuildFolder_By_Gitignore()
        {
            CreateFile("build/output.dll");
            CreateFile("src/code.cs");

            var files = _testRoot
                .EnumerateFilesRespectingGitIgnore(_config)
                .Select(Path.GetFileName)
                .ToList();

            files.ShouldNotContain("output.dll");
            files.ShouldContain("code.cs");
        }

        [Test]
        public void Should_Exclude_LogsFolder_By_Vtignore()
        {
            CreateFile("src/logs/log.txt");
            CreateFile("src/app.cs");

            var files = _testRoot
                .EnumerateFilesRespectingGitIgnore(_config)
                .Where(path => path.StartsWith(Path.Combine(_testRoot, "src")))
                .Select(Path.GetFileName)
                .ToList();

            files.ShouldNotContain("log.txt");
            files.ShouldContain("app.cs");
        }

        [Test]
        public void Should_Unignore_Specific_File_By_Negation_In_Vtignore()
        {
            CreateFile("src/keep.tmp");
            CreateFile("src/ignore.tmp");

            var files = _testRoot
                .EnumerateFilesRespectingGitIgnore2(_config)
                .Where(path => path.StartsWith(Path.Combine(_testRoot, "src")))
                .Select(f => FileHandlerBase.RelativePath(_testRoot,f))
                .ToList();

            files.ShouldContain("src/keep.tmp");
            files.ShouldNotContain("src/ignore.tmp");
        }
    }
}
