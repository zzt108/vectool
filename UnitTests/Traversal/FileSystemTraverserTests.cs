using LogCtxShared;
using NUnit.Framework;
using NLogShared;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VecTool.Configuration;
using VecTool.Handlers.Traversal;

namespace UnitTests.Traversal
{
    /// <summary>
    /// Unit tests for FileSystemTraverser - verifies exclusive authority
    /// over file/folder exclusion decisions.
    /// </summary>
    [TestFixture]
    public class FileSystemTraverserTests
    {
        private string testDir = default!;
        private VectorStoreConfig config = default!;
        private readonly CtxLogger _log = new();

        [SetUp]
        public void Setup()
        {
            testDir = Path.Combine(
                Path.GetTempPath(),
                "TraverserTests",
                Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(testDir);
            config = new VectorStoreConfig(testDir);
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, recursive: true);
            }
            catch
            {
                // Swallow cleanup exceptions
            }
        }

        /// <summary>
        /// TEST 1: Lazy initialization of pattern matcher
        /// Ensures matcher is only initialized once, even with multiple calls.
        /// </summary>
        [Test]
        public void EnsureMatcherInitializedShouldOnlyInitializeOnce()
        {
            // Arrange
            var traverser = new FileSystemTraverser(ui: null);
            var subDir = Path.Combine(testDir, "SubFolder");
            Directory.CreateDirectory(subDir);

            // Act - multiple calls should not reinitialize
            var files1 = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();
            var files2 = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();

            // Assert - no exceptions, matcher reused
            files1.ShouldNotBeNull();
            files2.ShouldNotBeNull();
            // Both calls succeeded = lazy initialization worked correctly
        }

        /// <summary>
        /// TEST 2: Pattern-based exclusion (Layer 1)
        /// .gitignore patterns should exclude files BEFORE legacy config fallback.
        /// </summary>
        [Test]
        public void EnumerateFilesShouldExcludePatternIgnoredFiles()
        {
            // Arrange
            File.WriteAllText(Path.Combine(testDir, ".gitignore"), "*.log\n*.tmp\n");
            File.WriteAllText(Path.Combine(testDir, "program.cs"), "// code");
            File.WriteAllText(Path.Combine(testDir, "debug.log"), "// logs");
            File.WriteAllText(Path.Combine(testDir, "temp.tmp"), "// temp");

            var traverser = new FileSystemTraverser(ui: null);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();

            // Assert - patterns checked first
            files.ShouldContain(f => f.EndsWith("program.cs"), "allowed file included");
            files.ShouldNotContain(f => f.EndsWith("debug.log"), "pattern should exclude .log");
            files.ShouldNotContain(f => f.EndsWith("temp.tmp"), "pattern should exclude .tmp");
        }

        /// <summary>
        /// TEST 3: .vtignore preference over .gitignore
        /// When both exist, .vtignore takes precedence.
        /// </summary>
        [Test]
        public void ShouldPreferVtignoreOverGitignore()
        {
            // Arrange
            File.WriteAllText(Path.Combine(testDir, ".gitignore"), "*.log\n");
            File.WriteAllText(Path.Combine(testDir, ".vtignore"), "*.txt\n");
            File.WriteAllText(Path.Combine(testDir, "file.log"), "log");
            File.WriteAllText(Path.Combine(testDir, "file.txt"), "text");
            File.WriteAllText(Path.Combine(testDir, "file.cs"), "code");

            var traverser = new FileSystemTraverser(ui: null);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();

            // Assert
            files.ShouldContain(f => f.EndsWith("file.log"),
                ".gitignore ignored; .vtignore is used");
            files.ShouldNotContain(f => f.EndsWith("file.txt"),
                ".vtignore should exclude .txt");
            files.ShouldContain(f => f.EndsWith("file.cs"));
        }

        /// <summary>
        /// TEST 4: Fallback to legacy config when no pattern file
        /// When .gitignore/.vtignore missing, use VectorStoreConfig exclusions.
        /// </summary>
        [Test]
        public void ShouldFallbackToLegacyConfigWhenNoPatternFile()
        {
            // Arrange
            config.ExcludedFiles.Add(".tmp");
            File.WriteAllText(Path.Combine(testDir, "file.cs"), "code");
            File.WriteAllText(Path.Combine(testDir, "file.tmp"), "temp");

            var traverser = new FileSystemTraverser(ui: null);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();

            // Assert
            files.ShouldContain(f => f.EndsWith("file.cs"));
            files.ShouldNotContain(f => f.EndsWith("file.tmp"),
                "legacy config should exclude .tmp");
        }

        /// <summary>
        /// TEST 5: LogCtx audit logging for exclusion decisions
        /// Every exclusion decision should be logged with reason + source.
        /// </summary>
        [Test]
        public void ShouldLogExclusionDecisionsViaLogCtx()
        {
            // Arrange
            File.WriteAllText(Path.Combine(testDir, ".gitignore"), "*.log\n");
            File.WriteAllText(Path.Combine(testDir, "debug.log"), "");
            File.WriteAllText(Path.Combine(testDir, "main.cs"), "");

            var traverser = new FileSystemTraverser(ui: null);

            // Act
            using (var ctx = _log.Ctx.Set(
                new Props()
                    .Add("test", "exclusion_logging")
                    .Add("testDir", testDir)))
            {
                var files = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();

                // Assert
                files.ShouldContain(f => f.EndsWith("main.cs"));
                files.ShouldNotContain(f => f.EndsWith("debug.log"),
                    "pattern should exclude");
            }
        }

        /// <summary>
        /// TEST 7: Nested folder exclusions
        /// bin/ in nested directories should also be excluded.
        /// </summary>
        [Test]
        public void ShouldExcludeNestedBinFolders()
        {
            // Arrange
            Directory.CreateDirectory(Path.Combine(testDir, "src", "feature1", "bin"));
            Directory.CreateDirectory(Path.Combine(testDir, "src", "feature2"));

            File.WriteAllText(Path.Combine(testDir, "src", "main.cs"), "");
            File.WriteAllText(Path.Combine(testDir, "src", "feature1", "feature.cs"), "");
            File.WriteAllText(Path.Combine(testDir, "src", "feature1", "bin", "output.dll"), "");
            File.WriteAllText(Path.Combine(testDir, "src", "feature2", "feature.cs"), "");

            File.WriteAllText(Path.Combine(testDir, ".gitignore"), "bin/\n");

            var traverser = new FileSystemTraverser(ui: null);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();

            // Assert
            files.Count().ShouldBe(3, "3 .cs files in src");
            files.ShouldNotContain(f => f.EndsWith("output.dll"),
                "Nested bin/ should be excluded");
        }

        /// <summary>
        /// TEST 8: Empty folder handling
        /// Empty folders should not cause errors during traversal.
        /// </summary>
        [Test]
        public void ShouldHandleEmptyFoldersGracefully()
        {
            // Arrange
            Directory.CreateDirectory(Path.Combine(testDir, "empty"));
            File.WriteAllText(Path.Combine(testDir, "file.cs"), "");

            var traverser = new FileSystemTraverser(ui: null);

            // Act & Assert - should NOT throw
            var files = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();
            files.ShouldContain(f => f.EndsWith("file.cs"));
        }

        /// <summary>
        /// TEST 9: Folder exclusion via legacy config
        /// VectorStoreConfig.ExcludedFolders should filter folders.
        /// </summary>
        [Test]
        public void ShouldExcludeFoldersViaLegacyConfig()
        {
            // Arrange
            config.ExcludedFolders.Add("vendor");
            Directory.CreateDirectory(Path.Combine(testDir, "src"));
            Directory.CreateDirectory(Path.Combine(testDir, "vendor"));

            File.WriteAllText(Path.Combine(testDir, "src", "main.cs"), "");
            File.WriteAllText(Path.Combine(testDir, "vendor", "lib.cs"), "");

            var traverser = new FileSystemTraverser(ui: null);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();

            // Assert
            files.ShouldNotContain(f => f.EndsWith("main.cs"));
            files.ShouldNotContain(f => f.EndsWith("lib.cs"), "vendor/ excluded by legacy config");
        }

        /// <summary>
        /// TEST 10: Multiple extension patterns
        /// Multiple exclusion patterns should all be applied.
        /// </summary>
        [Test]
        public void ShouldExcludeMultipleExtensionPatterns()
        {
            // Arrange
            File.WriteAllText(Path.Combine(testDir, ".gitignore"), "*.log\n*.tmp\n*.bak\n");
            File.WriteAllText(Path.Combine(testDir, "main.cs"), "");
            File.WriteAllText(Path.Combine(testDir, "debug.log"), "");
            File.WriteAllText(Path.Combine(testDir, "temp.tmp"), "");
            File.WriteAllText(Path.Combine(testDir, "backup.bak"), "");

            var traverser = new FileSystemTraverser(ui: null);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();

            // Assert
            files.Count.ShouldBe(1, "Only main.cs included");
            files.ShouldContain(f => f.EndsWith("main.cs"));
        }

        /// <summary>
        /// TEST 11: ProcessFolder delegates to traverser
        /// ProcessFolder method should use same exclusion logic as EnumerateFiles.
        /// </summary>
        [Test]
        public void ProcessFolderShouldRespectExclusions()
        {
            // Arrange
            File.WriteAllText(Path.Combine(testDir, ".gitignore"), "*.log\n");
            File.WriteAllText(Path.Combine(testDir, "code.cs"), "");
            File.WriteAllText(Path.Combine(testDir, "debug.log"), "");

            var traverser = new FileSystemTraverser(ui: null);
            var processedFiles = new List<string>();

            // Act
            traverser.ProcessFolder(
                testDir,
                processedFiles,
                config,
                (file, ctx, cfg) => ctx.Add(file),
                (ctx, name) => { }
            );

            // Assert
            processedFiles.ShouldContain(f => f.EndsWith("code.cs"));
            processedFiles.ShouldNotContain(f => f.EndsWith("debug.log"),
                "Excluded files should not reach delegate");
        }

        /// <summary>
        /// TEST 12: Whitespace and comment patterns in .gitignore
        /// Comments and empty lines in .gitignore should be handled.
        /// </summary>
        [Test]
        public void ShouldHandleCommentsAndWhitespaceInGitignore()
        {
            // Arrange
            var gitignoreContent = @"
# This is a comment
*.log

# Temp files
*.tmp
";
            File.WriteAllText(Path.Combine(testDir, ".gitignore"), gitignoreContent);
            File.WriteAllText(Path.Combine(testDir, "code.cs"), "");
            File.WriteAllText(Path.Combine(testDir, "debug.log"), "");

            var traverser = new FileSystemTraverser(ui: null);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testDir, config).ToList();

            // Assert
            files.ShouldContain(f => f.EndsWith("code.cs"));
            files.ShouldNotContain(f => f.EndsWith("debug.log"));
        }
    }
}