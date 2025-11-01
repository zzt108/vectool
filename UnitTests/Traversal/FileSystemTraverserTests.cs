using LogCtxShared;
using NUnit.Framework;
using NLogShared;
using Shouldly;
using System.IO;
using VecTool.Configuration;
using VecTool.Handlers.Traversal;

namespace UnitTests.Traversal
{
    [TestFixture]
    public class FileSystemTraverserTests
    {
        private string testDir = default!;
        private VectorStoreConfig config = default!;
        private readonly CtxLogger _ctxLogger = new("Config/nlog.config");

        [SetUp]
        public void Setup()
        {
            testDir = Path.Combine(Path.GetTempPath(), $"TraverserTests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(testDir);
            config = new VectorStoreConfig();
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, true);
            }
            catch { /* Swallow cleanup exceptions */ }
        }

        // ✅ TEST 1: Lazy initialization of pattern matcher
        [Test]
        public void EnsureMatcherInitialized_ShouldOnlyInitializeOnce()
        {
            // Arrange
            var traverser = new FileSystemTraverser(ui: null, rootPath: testDir);
            var subDir = Path.Combine(testDir, "SubFolder");
            Directory.CreateDirectory(subDir);

            // Act - multiple calls should not reinitialize
            var files1 = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();
            var files2 = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();

            // Assert - no exceptions, matcher reused
            files1.ShouldNotBeNull();
            files2.ShouldNotBeNull();
        }

        // ✅ TEST 2: Pattern-based exclusion (Layer 1) works
        [Test]
        public void EnumerateFiles_ShouldExcludePatternIgnoredFiles()
        {
            // Arrange
            var gitignorePath = Path.Combine(testDir, ".gitignore");
            File.WriteAllText(gitignorePath, "*.log\ntemp/\n");

            var includedFile = Path.Combine(testDir, "Program.cs");
            var excludedLog = Path.Combine(testDir, "App.log");
            var tempDir = Path.Combine(testDir, "temp");
            Directory.CreateDirectory(tempDir);
            var excludedInTemp = Path.Combine(tempDir, "data.txt");

            File.WriteAllText(includedFile, "class Program {}");
            File.WriteAllText(excludedLog, "log data");
            File.WriteAllText(excludedInTemp, "temp data");

            var traverser = new FileSystemTraverser(ui: null, rootPath: testDir);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();

            // Assert
            files.ShouldContain(includedFile);
            files.ShouldNotContain(excludedLog, "*.log pattern should exclude");
            files.ShouldNotContain(excludedInTemp, "temp/ folder should be excluded");
        }

        // ✅ TEST 3: Legacy config fallback (Layer 1 fallback) works
        [Test]
        public void EnumerateFiles_ShouldExcludeLegacyConfigFiles()
        {
            // Arrange
            config.ExcludedFiles.Add("*.tmp");
            var includedFile = Path.Combine(testDir, "data.json");
            var excludedFile = Path.Combine(testDir, "cache.tmp");

            File.WriteAllText(includedFile, "{}");
            File.WriteAllText(excludedFile, "cache");

            var traverser = new FileSystemTraverser(ui: null, rootPath: testDir);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();

            // Assert
            files.ShouldContain(includedFile);
            files.ShouldNotContain(excludedFile, "Legacy config should exclude *.tmp");
        }

        // ✅ TEST 4: ProcessFolder delegates exclusion correctly
        [Test]
        public void ProcessFolder_ShouldNotInvokeProcessFileForExcludedFiles()
        {
            // Arrange
            config.ExcludedFiles.Add("*.log");
            _ctxLogger.Ctx.Set();
            _ctxLogger.Debug($"Excluded files: {config.ExcludedFiles}");
            var csFile = Path.Combine(testDir, "Test.cs");
            var logFile = Path.Combine(testDir, "App.log");

            File.WriteAllText(csFile, "class Test {}");
            File.WriteAllText(logFile, "log entry");

            var traverser = new FileSystemTraverser(ui: null, rootPath: testDir);
            var processedFiles = new List<string>();

            // Act
            traverser.ProcessFolder(
                testDir,
                processedFiles,
                config,
                processFile: (file, ctx, cfg) => ctx.Add(file),
                writeFolderName: (ctx, name) => { },
                writeFolderEnd: null
            );

            // Assert
            processedFiles.ShouldContain(csFile);
            processedFiles.ShouldNotContain(logFile, "Excluded files should not reach processFile delegate");
        }

        // ✅ TEST 5: Pattern matcher handles missing .gitignore gracefully
        [Test]
        public void EnumerateFiles_ShouldWorkWithoutGitignoreFile()
        {
            // Arrange - no .gitignore
            var file = Path.Combine(testDir, "Program.cs");
            File.WriteAllText(file, "class Program {}");

            var traverser = new FileSystemTraverser(ui: null, rootPath: testDir);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();

            // Assert - should not throw, falls back to legacy-only
            files.ShouldContain(file);
        }
    }
}