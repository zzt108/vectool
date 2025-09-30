using DocXHandler;
using DocXHandler.RecentFiles;
using Shouldly;

namespace UnitTests
{
    [TestFixture]
    public class FileSizeSummaryHandlerTests
    {
        private string _testDir;
        private string _outputPath;
        private FileSizeSummaryHandler _handler;
        private VectorStoreConfig _config;

        [SetUp]
        public void Setup()
        {
            // Create a temporary directory for testing
            _testDir = Path.Combine(Path.GetTempPath(), "FileSizeSummaryTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_testDir);

            // Create test files
            CreateTestFile(Path.Combine(_testDir, "test1.cs"), 100);
            CreateTestFile(Path.Combine(_testDir, "test2.cs"), 200);
            CreateTestFile(Path.Combine(_testDir, "test.txt"), 150);

            // Create subdirectory with files
            string subDir = Path.Combine(_testDir, "subdir");
            Directory.CreateDirectory(subDir);
            CreateTestFile(Path.Combine(subDir, "test3.cs"), 300);
            CreateTestFile(Path.Combine(subDir, "test2.txt"), 250);

            _outputPath = Path.Combine(_testDir, "size_summary.md");
            _handler = new FileSizeSummaryHandler(null, null);
            _config = new VectorStoreConfig();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test directory
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        [Test]
        public void GenerateFileSizeSummary_ShouldCreateMarkdownFile()
        {
            // Arrange
            var folders = new List<string> { _testDir };

            // Act
            _handler.GenerateFileSizeSummary(folders, _outputPath, _config);

            // Assert
            File.Exists(_outputPath).ShouldBeTrue();
        }

        [Test]
        public void GenerateFileSizeSummary_ShouldIncludeAllFileTypes()
        {
            Assert.Inconclusive();

            // Arrange
            var folders = new List<string> { _testDir };

            // Act
            _handler.GenerateFileSizeSummary(folders, _outputPath, _config);

            // Assert
            string content = File.ReadAllText(_outputPath);
            content.ShouldContain(".cs");
            content.ShouldContain(".txt");

            // Should include file counts
            content.ShouldContain("3"); // 3 .cs files
            content.ShouldContain("2"); // 2 .txt files

            // Should include sizes
            content.ShouldContain("600"); // Total size of .cs files
            content.ShouldContain("400"); // Total size of .txt files
        }

        [Test]
        public void GenerateFileSizeSummaryShouldRegisterWithRecentFilesManager()
        {
            // Arrange
            var mockRecentFilesManager = new MockRecentFilesManager();
            var handlerWithManager = new FileSizeSummaryHandler(null, mockRecentFilesManager);
            var folders = new List<string> { _testDir };

            // Act
            handlerWithManager.GenerateFileSizeSummary(folders, _outputPath, _config);

            // Assert
            File.Exists(_outputPath).ShouldBeTrue();
            mockRecentFilesManager.RegisteredFiles.Count.ShouldBe(1);

            var registered = mockRecentFilesManager.RegisteredFiles[0];
            registered.FilePath.ShouldBe(_outputPath);
            registered.FileType.ShouldBe(RecentFileType.Md);
            registered.SourceFolders.ShouldBe(folders);
            registered.FileSizeBytes.ShouldBeGreaterThan(0);
        }

        [Test]
        public void GenerateFileSizeSummaryShouldNotRegisterIfWriteFails()
        {
            // Arrange
            var mockRecentFilesManager = new MockRecentFilesManager();
            var handlerWithManager = new FileSizeSummaryHandler(null, mockRecentFilesManager);
            var invalidOutputPath = Path.Combine(_testDir, "invalid\\path\\\\\\file.md");

            // Act & Assert
            Should.Throw<Exception>(() =>
                handlerWithManager.GenerateFileSizeSummary(
                    new List<string> { _testDir },
                    invalidOutputPath,
                    _config
                )
            );

            // Verify no registration happened
            mockRecentFilesManager.RegisteredFiles.Count.ShouldBe(0);
        }

        // Helper mock class
        private class MockRecentFilesManager : IRecentFilesManager
        {
            public List<(string FilePath, RecentFileType FileType, IReadOnlyList<string> SourceFolders, long FileSizeBytes)>
                RegisteredFiles{ get; } = new();

            public IReadOnlyList<RecentFileInfo> GetRecentFiles() =>
                Array.Empty<RecentFileInfo>();

            public void RegisterGeneratedFile(
                string filePath,
                RecentFileType fileType,
                IReadOnlyList<string> sourceFolders,
                long fileSizeBytes = 0,
                DateTime? generatedAtUtc = null)
            {
                RegisteredFiles.Add((filePath, fileType, sourceFolders, fileSizeBytes));
            }

            public int CleanupExpiredFiles(DateTime? nowUtc = null) => 0;
        }
        private void CreateTestFile(string path, int sizeInBytes)
        {
            using (var stream = File.Create(path))
            {
                var data = new byte[sizeInBytes];
                new Random().NextBytes(data);
                stream.Write(data, 0, data.Length);
            }
        }
    }
}
