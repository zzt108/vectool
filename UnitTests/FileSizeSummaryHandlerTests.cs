using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using VecTool.Configuration;
using VecTool.Handlers; // <--- EZ VOLT A KUTYA ELÁSVA! JAVÍTVA!
using VecTool.RecentFiles;

namespace UnitTests
{
    [TestFixture]
    public class FileSizeSummaryHandlerTests
    {
        private string _testDir;
        private VectorStoreConfig _config;
        private FileSizeSummaryHandler _handler;
        private MockRecentFilesManager _mockRecentFilesManager;

        [SetUp]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);

            // Create dummy files
            CreateTestFile(Path.Combine(_testDir, "file1.cs"), 200);
            CreateTestFile(Path.Combine(_testDir, "file2.cs"), 400);
            CreateTestFile(Path.Combine(_testDir, "file1.txt"), 100);
            CreateTestFile(Path.Combine(_testDir, "file2.txt"), 300);
            CreateTestFile(Path.Combine(_testDir, "excluded.log"), 50);

            _config = new VectorStoreConfig();
            _mockRecentFilesManager = new MockRecentFilesManager();
            _handler = new FileSizeSummaryHandler(null, _mockRecentFilesManager);
        }

        [TearDown]
        public void Teardown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        [Test]
        public void GenerateFileSizeSummary_ShouldCreateReportFile()
        {
            // Arrange
            var outputPath = Path.Combine(_testDir, "summary.md");
            var folders = new List<string> { _testDir };

            // Act
            _handler.GenerateFileSizeSummary(folders, outputPath, _config);

            // Assert
            File.Exists(outputPath).ShouldBeTrue();
            var content = File.ReadAllText(outputPath);
            content.ShouldNotBeNullOrWhiteSpace();
            content.ShouldContain("# File Size Summary");
        }

        [Test]
        public void GenerateFileSizeSummary_ShouldExcludeFilesFromConfig()
        {
            // Arrange
            var outputPath = Path.Combine(_testDir, "summary_excluded.md");
            var folders = new List<string> { _testDir };
            _config.ExcludedFiles.Add("*.log");

            // Act
            _handler.GenerateFileSizeSummary(folders, outputPath, _config);

            // Assert
            var content = File.ReadAllText(outputPath);
            content.ShouldNotContain(".log");
        }

        // ... a többi teszt változatlan ...

        [Test]
        public void GenerateFileSizeSummary_ShouldCorrectlySumFileSizesAndCounts()
        {
            // Arrange
            var outputPath = Path.Combine(_testDir, "summary_sums.md");
            var folders = new List<string> { _testDir };

            // Act
            _handler.GenerateFileSizeSummary(folders, outputPath, _config);

            // Assert
            var content = File.ReadAllText(outputPath);
            content.ShouldContain("| .cs | 2 |");
            content.ShouldContain("600.00 B");
            content.ShouldContain("| .txt | 2 |");
            content.ShouldContain("400.00 B");
        }

        [Test]
        public void GenerateFileSizeSummary_ShouldRegisterWithRecentFilesManager()
        {
            // Arrange
            var outputPath = Path.Combine(_testDir, "summary_recent.md");
            var folders = new List<string> { _testDir };

            // Act
            _handler.GenerateFileSizeSummary(folders, outputPath, _config);

            // Assert
            _mockRecentFilesManager.RegisteredFiles.Count.ShouldBe(1);
            var registeredFile = _mockRecentFilesManager.RegisteredFiles[0];
            registeredFile.FilePath.ShouldBe(outputPath);
            registeredFile.FileType.ShouldBe(RecentFileType.Md);
            registeredFile.SourceFolders.ShouldBe(folders);
            registeredFile.FileSizeBytes.ShouldBeGreaterThan(0);
        }

        private void CreateTestFile(string path, int sizeInBytes)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.SetLength(sizeInBytes);
            }
        }

        private class MockRecentFilesManager : IRecentFilesManager
        {
            public List<(string FilePath, RecentFileType FileType, IReadOnlyList<string> SourceFolders, long FileSizeBytes)> RegisteredFiles { get; } = new();

            public void RegisterGeneratedFile(string filePath, RecentFileType fileType, IReadOnlyList<string> sourceFolders, long fileSizeBytes = 0, DateTime? generatedAtUtc = null)
            {
                RegisteredFiles.Add((filePath, fileType, sourceFolders, fileSizeBytes));
            }

            public IReadOnlyList<RecentFileInfo> GetRecentFiles() => throw new NotImplementedException();
            public void CleanupExpiredFiles(DateTime? nowUtc = null) => throw new NotImplementedException();
            public void Load() => throw new NotImplementedException();
            public void Save() => throw new NotImplementedException();

            int IRecentFilesManager.CleanupExpiredFiles(DateTime? nowUtc)
            {
                throw new NotImplementedException();
            }
        }
    }
}
