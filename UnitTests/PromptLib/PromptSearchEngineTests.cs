using NUnit.Framework;
using Shouldly;
using VecTool.Core.Models;
using VecTool.Handlers;

namespace VecTool.UnitTests.PromptLib
{
    [TestFixture]
    public class PromptSearchEngineTests
    {
        private string testRepoPath = null!;
        private PromptsConfig testConfig = null!;
        private PromptSearchEngine engine = null!;

        [SetUp]
        public void Setup()
        {
            // Create temporary test directory
            testRepoPath = Path.Combine(Path.GetTempPath(), $"prompt-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(testRepoPath);

            // Create test config
            testConfig = new PromptsConfig(
                testRepoPath,
                ".md,.txt",
                "llm-config.json",
                "favorites.json");

            // Initialize engine
            engine = new PromptSearchEngine(testConfig);
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(testRepoPath))
            {
                Directory.Delete(testRepoPath, recursive: true);
            }
        }

        [Test]
        public void RebuildIndexShouldParseValidFiles()
        {
            // Arrange
            var workDir = Path.Combine(testRepoPath, "work", "vectool", "spaces");
            Directory.CreateDirectory(workDir);

            File.WriteAllText(
                Path.Combine(workDir, "PROMPT-1.0-analyzer.md"),
                "# AI Code Analyzer\nThis is test content.");

            File.WriteAllText(
                Path.Combine(workDir, "GUIDE-1.5-convention.md"),
                "# Coding Convention Guide\nFollow these rules.");

            // Act
            engine.RebuildIndex();

            // Assert
            engine.GetIndexedFileCount().ShouldBe(2);
        }

        [Test]
        public void SearchShouldFindFilesByFilename()
        {
            // Arrange
            CreateTestFile("PROMPT-1.0-analyzer.md", "Test content");
            CreateTestFile("GUIDE-1.0-setup.md", "Setup guide");
            engine.RebuildIndex();

            // Act
            var results = engine.Search("analyzer");

            // Assert
            results.Count.ShouldBe(1);
            results[0].Metadata.Name.ShouldBe("analyzer");
        }

        [Test]
        public void SearchShouldFindFilesByContent()
        {
            // Arrange
            CreateTestFile("PROMPT-1.0-test.md", "This file contains VecTool information.");
            CreateTestFile("GUIDE-1.0-other.md", "Unrelated content.");
            engine.RebuildIndex();

            // Act
            var results = engine.Search("VecTool");

            // Assert
            results.Count.ShouldBe(1);
            results[0].Metadata.Name.ShouldBe("test");
        }

        [Test]
        public void SearchShouldReturnAllFilesWhenQueryIsEmpty()
        {
            // Arrange
            CreateTestFile("PROMPT-1.0-test1.md", "Content 1");
            CreateTestFile("PROMPT-1.0-test2.md", "Content 2");
            engine.RebuildIndex();

            // Act
            var results = engine.Search("");

            // Assert
            results.Count.ShouldBe(2);
        }

        [Test]
        public void GetByHierarchyShouldFilterByArea()
        {
            // Arrange
            var workDir = Path.Combine(testRepoPath, "work", "vectool", "spaces");
            var privateDir = Path.Combine(testRepoPath, "private", "notes", "misc");
            Directory.CreateDirectory(workDir);
            Directory.CreateDirectory(privateDir);

            File.WriteAllText(Path.Combine(workDir, "PROMPT-1.0-work-file.md"), "Work content");
            File.WriteAllText(Path.Combine(privateDir, "PROMPT-1.0-private-file.md"), "Private content");
            engine.RebuildIndex();

            // Act
            var results = engine.GetByHierarchy("work", null, null);

            // Assert
            results.Count.ShouldBe(1);
            results[0].Metadata.Area.ShouldBe("work");
        }

        [Test]
        public void GetByHierarchyShouldFilterByProjectAndCategory()
        {
            // Arrange
            var vecDir = Path.Combine(testRepoPath, "work", "vectool", "spaces");
            var linxDir = Path.Combine(testRepoPath, "work", "linx", "guides");
            Directory.CreateDirectory(vecDir);
            Directory.CreateDirectory(linxDir);

            File.WriteAllText(Path.Combine(vecDir, "PROMPT-1.0-vec.md"), "VecTool space");
            File.WriteAllText(Path.Combine(linxDir, "GUIDE-1.0-linx.md"), "Linx guide");
            engine.RebuildIndex();

            // Act
            var results = engine.GetByHierarchy(null, "vectool", "spaces");

            // Assert
            results.Count.ShouldBe(1);
            results[0].Metadata.Project.ShouldBe("vectool");
            results[0].Metadata.Category.ShouldBe("spaces");
        }

        [Test]
        public void RebuildIndexShouldHandleInvalidFiles()
        {
            // Arrange
            CreateTestFile("invalid-file.docx", "No valid pattern");
            CreateTestFile("PROMPT-1.0-valid.md", "Valid content");
            engine.RebuildIndex();

            // Act & Assert
            engine.GetIndexedFileCount().ShouldBe(1); // Only valid file indexed
        }

        [Test]
        public void RebuildIndexShouldSkipNonMatchingExtensions()
        {
            // Arrange
            CreateTestFile("PROMPT-1.0-test.md", "Markdown file");
            CreateTestFile("PROMPT-1.0-test.exe", "Executable"); // Not in config extensions
            engine.RebuildIndex();

            // Act & Assert
            engine.GetIndexedFileCount().ShouldBe(1);
        }

        [Test]
        public void SearchShouldBeCaseInsensitive()
        {
            // Arrange
            CreateTestFile("PROMPT-1.0-Analyzer.md", "Content");
            engine.RebuildIndex();

            // Act
            var resultsLower = engine.Search("analyzer");
            var resultsUpper = engine.Search("ANALYZER");

            // Assert
            resultsLower.Count.ShouldBe(1);
            resultsUpper.Count.ShouldBe(1);
        }

        [Test]
        public void SearchShouldRespectContentLimitOf2000Chars()
        {
            // Arrange
            var longContent = new string('x', 2500) + " FINDME";
            CreateTestFile("PROMPT-1.0-long.md", longContent);
            engine.RebuildIndex();

            // Act - search term beyond 2000 chars should not be found
            var results = engine.Search("FINDME");

            // Assert
            results.Count.ShouldBe(0);
        }

        private void CreateTestFile(string fileName, string content)
        {
            var filePath = Path.Combine(testRepoPath, fileName);
            File.WriteAllText(filePath, content);
        }
    }
}
