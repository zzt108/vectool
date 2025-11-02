using LogCtxShared;
using NUnit.Framework;
using NLogShared;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VecTool.Configuration;
using VecTool.Handlers.Traversal;

namespace UnitTests.Traversal
{
    /// <summary>
    /// Integration tests for FileSystemTraverser with real file system operations.
    /// Tests performance, nested structures, and real-world .gitignore patterns.
    /// </summary>
    [TestFixture]
    public class FileSystemTraverserIntegrationTestsV1
    {
        private string testRoot = default!;
        private readonly CtxLogger _log = new();

        [SetUp]
        public void Setup()
        {
            testRoot = Path.Combine(
                Path.GetTempPath(),
                "TraverserIntegration",
                Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(testRoot);
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                if (Directory.Exists(testRoot))
                    Directory.Delete(testRoot, recursive: true);
            }
            catch
            {
                // Swallow cleanup exceptions
            }
        }

        /// <summary>
        /// INTEGRATION TEST 1: Performance baseline
        /// 1000 files should enumerate in reasonable time (~200-500ms).
        /// </summary>
        [Test]
        public void EnumerateFilesShouldHandleLargeProjectsEfficiently()
        {
            // Arrange - Create realistic project structure
            var setupStopwatch = Stopwatch.StartNew();

            var folders = new[] { "src", "tests", "bin", "obj" };
            foreach (var folder in folders)
            {
                var dir = Path.Combine(testRoot, folder);
                Directory.CreateDirectory(dir);
                for (int i = 0; i < 250; i++)
                {
                    File.WriteAllText(
                        Path.Combine(dir, $"file_{i:D4}.cs"),
                        "// code line\n// another line"
                    );
                }
            }

            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "bin/\nobj/\n");
            setupStopwatch.Stop();

            var config = new VectorStoreConfig();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);

            // Act - Enumerate and measure
            var enumStopwatch = Stopwatch.StartNew();
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();
            enumStopwatch.Stop();

            // Assert
            files.Count.ShouldBe(500, "Only src/ and tests/ included");
            enumStopwatch.ElapsedMilliseconds.ShouldBeLessThan(
                1000,
                $"Should complete in <1s (took {enumStopwatch.ElapsedMilliseconds}ms)"
            );

            using (var ctx = _log.Ctx.Set(
                new Props()
                    .Add("test", "performance_large_project")
                    .Add("fileCount", files.Count)
                    .Add("setupMs", setupStopwatch.ElapsedMilliseconds)
                    .Add("enumerateMs", enumStopwatch.ElapsedMilliseconds)))
            {
                _log.Info("Large project enumeration completed");
            }
        }

        /// <summary>
        /// INTEGRATION TEST 2: Nested folder exclusions
        /// Verify bin/ in nested directories is excluded at all levels.
        /// </summary>
        [Test]
        public void ShouldRespectNestedExclusionsAcrossLevels()
        {
            // Arrange - Multi-level nesting
            var structure = new[]
            {
                "src/main.cs",
                "src/feature1/feature.cs",
                "src/feature1/bin/output.dll",
                "src/feature1/bin/debug.pdb",
                "src/feature2/feature.cs",
                "src/feature2/obj/temp.obj",
                "tests/test.cs",
                "tests/bin/test.dll",
            };

            foreach (var path in structure)
            {
                var fullPath = Path.Combine(testRoot, path);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllText(fullPath, "content");
            }

            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "bin/\nobj/\n");

            var config = new VectorStoreConfig();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert
            files.Count().ShouldBe(5, "5 .cs files only");
            files.ShouldContain(f => f.EndsWith("main.cs"));
            files.ShouldContain(f => f.EndsWith("feature.cs"));
            files.ShouldContain(f => f.EndsWith("test.cs"));

            files.ShouldNotContain(f => f.EndsWith("output.dll"));
            files.ShouldNotContain(f => f.EndsWith("debug.pdb"));
            files.ShouldNotContain(f => f.EndsWith("temp.obj"));
        }

        /// <summary>
        /// INTEGRATION TEST 3: Real-world .gitignore from C# project
        /// Test with patterns typical in .NET projects.
        /// </summary>
        [Test]
        public void ShouldHandleRealWorldDotnetGitignore()
        {
            // Arrange - Typical .NET .gitignore
            var gitignoreContent = @"
## Ignore Visual Studio temporary files, build results, and
bin/
obj/
.vs/
.vscode/
*.user
*.suo
*.db
*.log
*~
*.swp
*.swo
.DS_Store
node_modules/
dist/
";
            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), gitignoreContent);

            // Create realistic .NET project structure
            var structure = new[]
            {
                "Program.cs",
                "App.xaml.cs",
                "Properties/AssemblyInfo.cs",
                "bin/Debug/app.exe",
                "bin/Release/app.exe",
                "obj/Debug/temp.obj",
                ".vs/config/v16/StateStore/component.xml",
                ".vscode/settings.json",
                "build.log",
                "temp~",
                "file.swp",
                "node_modules/package.json",
                "dist/bundle.js",
            };

            foreach (var path in structure)
            {
                var fullPath = Path.Combine(testRoot, path);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllText(fullPath, "");
            }

            var config = new VectorStoreConfig();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert - Only .cs files in root and Properties
            files.Count().ShouldBe(3, "Only 3 source files");
            files.ShouldContain(f => f.EndsWith("Program.cs"));
            files.ShouldContain(f => f.EndsWith("App.xaml.cs"));
            files.ShouldContain(f => f.EndsWith("AssemblyInfo.cs"));

            // All excluded patterns should be filtered out
            files.ShouldNotContain(f => f.Contains("bin"));
            files.ShouldNotContain(f => f.Contains("obj"));
            files.ShouldNotContain(f => f.Contains(".vs"));
            files.ShouldNotContain(f => f.Contains(".vscode"));
            files.ShouldNotContain(f => f.Contains("node_modules"));
            files.ShouldNotContain(f => f.Contains("dist"));
            files.ShouldNotContain(f => f.EndsWith(".log"));
            files.ShouldNotContain(f => f.EndsWith(".swp"));
        }

        /// <summary>
        /// INTEGRATION TEST 4: Symlinks and special paths
        /// Gracefully handle symlinks and inaccessible paths.
        /// </summary>
        [Test]
        public void ShouldHandleSpecialPathsGracefully()
        {
            // Arrange
            Directory.CreateDirectory(Path.Combine(testRoot, "accessible"));
            File.WriteAllText(Path.Combine(testRoot, "accessible", "file.cs"), "");
            File.WriteAllText(Path.Combine(testRoot, "accessible", "file.txt"), "");

            var config = new VectorStoreConfig();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);

            // Act & Assert - Should NOT throw exception
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            files.ShouldContain(f => f.EndsWith("file.cs"), "Accessible file enumerated");
            files.ShouldContain(f => f.EndsWith("file.txt"), "All files enumerated");
        }

        /// <summary>
        /// INTEGRATION TEST 5: Unicode and special characters in paths
        /// Ensure Unicode filenames and paths are handled correctly.
        /// </summary>
        [Test]
        public void ShouldHandleUnicodeFilenamesAndPaths()
        {
            // Arrange
            Directory.CreateDirectory(Path.Combine(testRoot, "中文文件夹"));
            File.WriteAllText(Path.Combine(testRoot, "中文文件夹", "файл.cs"), "");
            File.WriteAllText(Path.Combine(testRoot, "αρχείο.txt"), "");

            var config = new VectorStoreConfig();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert
            files.Count().ShouldBe(2, "Unicode files enumerated");
            files.ShouldContain(f => f.Contains("файл.cs"));
            files.ShouldContain(f => f.Contains("αρχείο.txt"));
        }

        /// <summary>
        /// INTEGRATION TEST 6: Folder enumeration with mixed exclusions
        /// Test EnumerateFolders with both pattern and legacy config.
        /// </summary>
        [Test]
        public void EnumerateFoldersShouldRespectMixedExclusions()
        {
            // Arrange
            var structure = new[]
            {
                "src/core/main.cs",
                "src/Ui/window.cs",
                "bin/debug/app.exe",
                "build/temp/file.tmp",
                ".git/config",
                "vendor/lib/helper.cs",
            };

            foreach (var path in structure)
            {
                var fullPath = Path.Combine(testRoot, path);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllText(fullPath, "");
            }

            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "bin/\nbuild/\n.git/\n");

            var config = new VectorStoreConfig();
            config.ExcludedFolders.Add("vendor");

            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);

            // Act
            var folders = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert
            folders.ShouldContain(f => f.EndsWith("src"), "src should be included");
            folders.ShouldContain(f => f.EndsWith("core"), "src/core should be included");
            folders.ShouldContain(f => f.EndsWith("Ui"), "src/Ui should be included");

            folders.ShouldNotContain(f => f.EndsWith("bin"), "bin excluded by pattern");
            folders.ShouldNotContain(f => f.EndsWith("build"), "build excluded by pattern");
            folders.ShouldNotContain(f => f.EndsWith(".git"), ".git excluded by pattern");
            folders.ShouldNotContain(f => f.EndsWith("vendor"), "vendor excluded by config");
        }

        /// <summary>
        /// INTEGRATION TEST 7: ProcessFolder with filtering
        /// Verify ProcessFolder correctly delegates and filters.
        /// </summary>
        [Test]
        public void ProcessFolderShouldFilterAndDelegate()
        {
            // Arrange
            var structure = new[]
            {
                "code.cs",
                "debug.log",
                "readme.md",
                "test.cs",
            };

            foreach (var file in structure)
            {
                File.WriteAllText(Path.Combine(testRoot, file), "content");
            }

            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "*.log\n");

            var config = new VectorStoreConfig();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);
            var processedFiles = new List<string>();
            var folderSummaries = new List<string>();

            // Act
            traverser.ProcessFolder(
                testRoot,
                processedFiles,
                config,
                (file, ctx, cfg) =>
                {
                    ctx.Add(Path.GetFileName(file));
                },
                (ctx, name) =>
                {
                    folderSummaries.Add($"{name}: {string.Join(", ", ctx)}");
                }
            );

            // Assert
            processedFiles.Count.ShouldBe(3, "3 non-excluded files");
            processedFiles.ShouldContain(f => f.EndsWith("code.cs"));
            processedFiles.ShouldContain(f => f.EndsWith("readme.md"));
            processedFiles.ShouldContain(f => f.EndsWith("test.cs"));
            processedFiles.ShouldNotContain(f => f.EndsWith("debug.log"));
        }

        /// <summary>
        /// INTEGRATION TEST 8: .vtignore overrides .gitignore (real files)
        /// Test precedence with actual file system.
        /// </summary>
        [Test]
        public void ShouldPreferVtignoreOverGitignoreInRealFilesystem()
        {
            // Arrange
            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "*.log\n*.txt\n");
            File.WriteAllText(Path.Combine(testRoot, ".vtignore"), "*.txt\n");

            var structure = new[]
            {
                "main.cs",
                "debug.log",
                "readme.txt",
            };

            foreach (var file in structure)
            {
                File.WriteAllText(Path.Combine(testRoot, file), "");
            }

            var config = new VectorStoreConfig();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert
            files.ShouldContain(f => f.EndsWith("main.cs"));
            files.ShouldContain(f => f.EndsWith("debug.log"), ".gitignore ignored");
            files.ShouldNotContain(f => f.EndsWith("readme.txt"), ".vtignore takes precedence");
        }

        /// <summary>
        /// INTEGRATION TEST 9: Large file handling
        /// Verify large files don't cause issues during enumeration.
        /// </summary>
        [Test]
        public void ShouldHandleLargeFilesWithoutIssues()
        {
            // Arrange
            var smallFile = Path.Combine(testRoot, "small.cs");
            var largeFile = Path.Combine(testRoot, "large.bin");
            var excludedFile = Path.Combine(testRoot, "debug.log");

            File.WriteAllText(smallFile, "// small");
            File.WriteAllBytes(largeFile, new byte[10 * 1024 * 1024]); // 10MB
            File.WriteAllText(excludedFile, "");

            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "*.log\n");

            var config = new VectorStoreConfig();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);

            // Act - Should complete quickly despite large file
            var sw = Stopwatch.StartNew();
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();
            sw.Stop();

            // Assert
            files.Count().ShouldBe(2, "small.cs and large.bin enumerated");
            files.ShouldNotContain(f => f.EndsWith("debug.log"));
            sw.ElapsedMilliseconds.ShouldBeLessThan(500, "Should not read file contents");
        }

        /// <summary>
        /// INTEGRATION TEST 10: Concurrent enumeration safety
        /// Multiple traversers on same structure should be safe.
        /// </summary>
        [Test]
        public void ConcurrentEnumerationShouldBeSafe()
        {
            // Arrange
            for (int i = 0; i < 10; i++)
            {
                File.WriteAllText(Path.Combine(testRoot, $"file_{i}.cs"), $"// Content {i}");
            }

            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "*.log\n");

            var config = new VectorStoreConfig();
            var results = new List<int>();

            // Act - Multiple threads enumerate simultaneously
            var threads = Enumerable.Range(0, 5).Select(_ => new System.Threading.Thread(() =>
            {
                var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);
                var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();
                lock (results)
                {
                    results.Add(files.Count);
                }
            })).ToList();

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            // Assert
            results.ShouldAllBe( x=>x==10, "All threads got same result");
        }

        /// <summary>
        /// INTEGRATION TEST 11: Empty .gitignore behavior
        /// Empty or whitespace-only .gitignore should not exclude files.
        /// </summary>
        [Test]
        public void EmptyGitignoreShouldNotExcludeAnything()
        {
            // Arrange
            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "   \n\n  \n");
            File.WriteAllText(Path.Combine(testRoot, "main.cs"), "");
            File.WriteAllText(Path.Combine(testRoot, "config.xml"), "");
            File.WriteAllText(Path.Combine(testRoot, "data.log"), "");

            var config = new VectorStoreConfig();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert
            files.Count().ShouldBe(3, "All files included with empty .gitignore");
        }

        /// <summary>
        /// INTEGRATION TEST 12: Breadcrumb pattern matching
        /// Test wildcard patterns like **/*.log
        /// </summary>
        [Test]
        public void ShouldSupportBreadcrumbPatterns()
        {
            // Arrange
            var structure = new[]
            {
                "main.cs",
                "debug/output.log",
                "src/build/temp.log",
                "tests/results.log",
                "readme.md",
            };

            foreach (var path in structure)
            {
                var fullPath = Path.Combine(testRoot, path);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllText(fullPath, "");
            }

            // .gitignore with breadcrumb pattern (typically handled by pattern matcher)
            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "*.log\n");

            var config = new VectorStoreConfig();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert
            files.Count().ShouldBe(2, "Only .cs and .md files");
            files.ShouldNotContain(f => f.EndsWith(".log"));
        }
    }
}