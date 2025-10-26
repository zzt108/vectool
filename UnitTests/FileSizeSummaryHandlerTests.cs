// ✅ FULL FILE VERSION
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using VecTool.Configuration;
using VecTool.Handlers;
using VecTool.RecentFiles;

namespace UnitTests
{
    [TestFixture]
    public class FileSizeSummaryHandlerTests
    {
        private string testDir = default!;
        private VectorStoreConfig config = default!;
        private FileSizeSummaryHandler handler = default!;
        private MockRecentFilesManager mockRecentFilesManager = default!;

        [SetUp]
        public void Setup()
        {
            testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);

            // Minimal config for tests; properties adjusted in individual tests if needed
            config = new VectorStoreConfig();

            mockRecentFilesManager = new MockRecentFilesManager();
            // UserInterface is not required for these tests; pass null safely
            handler = new FileSizeSummaryHandler(null, mockRecentFilesManager);
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, true);
            }
            catch
            {
                // Swallow cleanup exceptions to keep tests robust on CI
            }
        }

        [Test]
        public void GenerateFileSizeSummaryShouldCreateReportFile()
        {
            // Arrange
            var outputPath = Path.Combine(testDir, "summary.md");
            var folders = new List<string> { testDir };
            CreateTestFile(Path.Combine(testDir, "file1.cs"), 200);
            CreateTestFile(Path.Combine(testDir, "file2.txt"), 300);

            // Act
            handler.GenerateFileSizeSummary(folders, outputPath, config);

            // Assert
            File.Exists(outputPath).ShouldBeTrue();
        }

        [Test]
        public void GenerateFileSizeSummaryShouldExcludeFilesFromConfig()
        {
            // Arrange
            var outputPath = Path.Combine(testDir, "summary_excluded.md");
            var folders = new List<string> { testDir };
            CreateTestFile(Path.Combine(testDir, "keep.cs"), 100);
            CreateTestFile(Path.Combine(testDir, "excluded.log"), 50);
            // Exclude .log
            config.ExcludedFiles.Add(".log");

            // Act
            handler.GenerateFileSizeSummary(folders, outputPath, config);

            // Assert
            var content = File.ReadAllText(outputPath);
            content.ShouldNotBeNullOrWhiteSpace();
            content.ShouldNotContain(".log");
        }

        [Test]
        public void GenerateFileSizeSummaryShouldCorrectlySumFileSizesAndCounts()
        {
            // Arrange
            var outputPath = Path.Combine(testDir, "summary_sums.md");
            var folders = new List<string> { testDir };
            CreateTestFile(Path.Combine(testDir, "a1.cs"), 200);
            CreateTestFile(Path.Combine(testDir, "a2.cs"), 400);
            CreateTestFile(Path.Combine(testDir, "b1.txt"), 100);
            CreateTestFile(Path.Combine(testDir, "b2.txt"), 300);

            // Act
            handler.GenerateFileSizeSummary(folders, outputPath, config);

            // Assert
            var content = File.ReadAllText(outputPath);
            content.ShouldContain("File Size Summary - Exported Files");   
            content.ShouldContain("Analyzed Folders");


            // Find the .cs row (robust to spacing)
            var line = content
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .FirstOrDefault(l => l.TrimStart().Contains(".cs", StringComparison.OrdinalIgnoreCase));

            line.ShouldNotBeNull();

            // Count must be 2 (avoid strict column/whitespace coupling)
            line.ShouldContain("2");

            // Culture/format tolerant totals and averages:
            // Accept either bytes or KB with comma or dot decimal separators.
            var totalPattern = @"(600(?:[.,]00)?\s?B|0[.,]59\s?KB)";
            var avgPattern = @"(300(?:[.,]00)?\s?B|0[.,]29\s?KB)";

            line.ShouldMatch(totalPattern);
            line.ShouldMatch(avgPattern);
            
            line = content
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .FirstOrDefault(l => l.TrimStart().Contains(".txt", StringComparison.OrdinalIgnoreCase));

            line.ShouldNotBeNull();

            // Count must be 2 (avoid strict column/whitespace coupling)
            line.ShouldContain("2");
            totalPattern = @"(400(?:[.,]00)?\s?B|0[.,]39\s?KB)";
            avgPattern = @"(200(?:[.,]00)?\s?B|0[.,]19\s?KB)";

            line.ShouldMatch(totalPattern);
            line.ShouldMatch(avgPattern);
        }

        [Test]
        public void GenerateFileSizeSummaryShouldRegisterWithRecentFilesManager()
        {
            // Arrange
            var outputPath = Path.Combine(testDir, "summary_recent.md");
            var folders = new List<string> { testDir };
            CreateTestFile(Path.Combine(testDir, "x.cs"), 10);

            // Act
            handler.GenerateFileSizeSummary(folders, outputPath, config);

            // Assert
            mockRecentFilesManager.RegisteredFiles.Count.ShouldBe(1);
            var record = mockRecentFilesManager.RegisteredFiles[0];
            record.FilePath.ShouldBe(outputPath);
            record.FileType.ShouldBe(RecentFileType.TestResults_Md); // The handler uses a summary/report type; adjust if the handler uses another enum
            record.SourceFolders.ShouldBe(folders);
            record.FileSizeBytes.ShouldBeGreaterThan(0);
        }

        // Helpers

        private static void CreateTestFile(string path, int sizeInBytes)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            fs.SetLength(sizeInBytes);
        }

        // Test double

        private class MockRecentFilesManager : IRecentFilesManager
        {
            public List<(string FilePath, RecentFileType FileType, IReadOnlyList<string> SourceFolders, long FileSizeBytes)> RegisteredFiles { get; } = new();

            public IReadOnlyList<RecentFileInfo> GetRecentFiles()
            {
                throw new NotImplementedException();
            }

            public void RegisterGeneratedFile(string filePath, RecentFileType fileType, IReadOnlyList<string> sourceFolders, long fileSizeBytes = 0, DateTime? generatedAtUtc = null)
            {
                RegisteredFiles.Add((filePath, fileType, sourceFolders, fileSizeBytes));
            }

            public int CleanupExpiredFiles(DateTime? nowUtc = null)
            {
                throw new NotImplementedException();
            }

            public void Save()
            {
                throw new NotImplementedException();
            }

            public void Load()
            {
                throw new NotImplementedException();
            }

            public void RemoveFile(string path)
            {
                // No-op for these tests; not required by the current assertions
            }
        }
    }
}
