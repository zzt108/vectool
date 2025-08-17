using GitIgnore.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitIgnore.Services;

namespace GitIgnore.Examples
{
    /// <summary>
    /// Demonstrates how to use the hierarchical .gitignore functionality
    /// </summary>
    public static class GitIgnoreUsageExamples
    {
        /// <summary>
        /// Example 1: Basic usage with HierarchicalGitIgnoreManager
        /// </summary>
        public static void BasicGitIgnoreExample(string projectRoot)
        {
            Console.WriteLine("=== Basic GitIgnore Example ===");

            using var gitIgnoreManager = new HierarchicalGitIgnoreManager(projectRoot);

            // Get statistics about loaded .gitignore files
            var stats = gitIgnoreManager.GetStatistics();
            Console.WriteLine($"Loaded {stats.GitIgnoreFileCount} .gitignore files");
            Console.WriteLine($"Total patterns: {stats.TotalPatterns}");
            Console.WriteLine($"Negation patterns: {stats.NegationPatterns}");

            // Check if specific files should be ignored
            var testFiles = new[]
            {
                Path.Combine(projectRoot, "app.log"),
                Path.Combine(projectRoot, "src", "main.cs"),
                Path.Combine(projectRoot, "bin", "debug", "app.exe"),
                Path.Combine(projectRoot, "README.md")
            };

            foreach (var file in testFiles)
            {
                var isIgnored = gitIgnoreManager.ShouldIgnore(file, File.Exists(file) ? false : Directory.Exists(file));
                Console.WriteLine($"{Path.GetFileName(file)}: {(isIgnored ? "IGNORED" : "NOT IGNORED")}");
            }
        }

        /// <summary>
        /// Example 2: Using GitIgnoreAwareFileProcessor for batch operations
        /// </summary>
        public static void FileProcessingExample(string projectRoot)
        {
            Console.WriteLine("\n=== File Processing Example ===");

            var options = GitIgnoreOptionsBuilder.Create()
                .WithCache(true)
                .WithAutoRefresh(true)
                .ContinueOnErrors(true);

            using var processor = new GitIgnoreAwareFileProcessor(projectRoot, options);

            // Set up error handling
            processor.ErrorOccurred += (sender, error) =>
            {
                Console.WriteLine($"Error processing {error.FilePath}: {error.Exception?.Message}");
            };

            // Process all non-ignored files
            processor.ProcessDirectory(
                projectRoot,
                fileInfo =>
                {
                    Console.WriteLine($"Processing: {fileInfo.Name} ({fileInfo.Length} bytes)");
                    // Your file processing logic here
                },
                directoryInfo =>
                {
                    Console.WriteLine($"Entering directory: {directoryInfo.Name}");
                }
            );

            // Get processing statistics
            var processingStats = processor.GetStatistics();
            Console.WriteLine($"Processed {processingStats.ProcessedFiles} files and {processingStats.ProcessedDirectories} directories");
        }

        /// <summary>
        /// Example 3: Using extension methods for simple operations
        /// </summary>
        public static void ExtensionMethodsExample(string projectRoot)
        {
            Console.WriteLine("\n=== Extension Methods Example ===");

            // Get all C# files that aren't ignored
            var csharpFiles = projectRoot.EnumerateFilesRespectingGitIgnore("*.cs").ToList();
            Console.WriteLine($"Found {csharpFiles.Count} non-ignored C# files");

            // Filter existing file collection
            var allFiles = Directory.EnumerateFiles(projectRoot, "*.*", SearchOption.AllDirectories);
            var nonIgnoredFiles = allFiles.FilterByGitIgnore(projectRoot).ToList();
            Console.WriteLine($"Filtered {allFiles.Count()} files down to {nonIgnoredFiles.Count} non-ignored files");

            // Check individual files
            var testFile = Path.Combine(projectRoot, "test.log");
            if (testFile.IsIgnoredByGit(projectRoot))
            {
                Console.WriteLine($"{Path.GetFileName(testFile)} is ignored by .gitignore");
            }

            // Get GitIgnore statistics
            var stats = projectRoot.GetGitIgnoreStats();
            Console.WriteLine($"GitIgnore stats: {stats}");
        }

        /// <summary>
        /// Example 4: Processing with custom filtering and actions
        /// </summary>
        public static void CustomProcessingExample(string projectRoot)
        {
            Console.WriteLine("\n=== Custom Processing Example ===");

            // Process only source code files, respecting .gitignore
            projectRoot.ProcessFilesRespectingGitIgnore(
                fileInfo =>
                {
                    if (IsSourceCodeFile(fileInfo.Extension))
                    {
                        AnalyzeSourceFile(fileInfo);
                    }
                },
                GitIgnoreOptionsBuilder.Create()
                    .IncludeHiddenFiles(false)
                    .ContinueOnErrors(true)
            );
        }

        /// <summary>
        /// Example 5: Handling hierarchical patterns with negations
        /// </summary>
        public static void HierarchicalPatternsExample(string projectRoot)
        {
            Console.WriteLine("\n=== Hierarchical Patterns Example ===");

            using var manager = new HierarchicalGitIgnoreManager(projectRoot);

            // Demonstrate how child .gitignore files can override parent patterns
            var testPaths = new Dictionary<string, string>
            {
                ["Root log file"] = Path.Combine(projectRoot, "app.log"),
                ["Config important log"] = Path.Combine(projectRoot, "config", "important.log"),
                ["Test log file"] = Path.Combine(projectRoot, "tests", "unit.log"),
                ["Build artifact"] = Path.Combine(projectRoot, "bin", "release", "app.exe"),
                ["Source file"] = Path.Combine(projectRoot, "src", "Program.cs")
            };

            foreach (var kvp in testPaths)
            {
                var isIgnored = manager.ShouldIgnore(kvp.Value, Directory.Exists(kvp.Value));
                Console.WriteLine($"{kvp.Key}: {(isIgnored ? "IGNORED" : "NOT IGNORED")}");
            }

            // Show which .gitignore files are affecting decisions
            Console.WriteLine("\nLoaded .gitignore files:");
            var stats = manager.GetStatistics();
            Console.WriteLine($"Found {stats.GitIgnoreFileCount} .gitignore files with {stats.TotalPatterns} total patterns");
        }

        /// <summary>
        /// Example 6: Performance optimization with caching
        /// </summary>
        public static void PerformanceOptimizationExample(string projectRoot)
        {
            Console.WriteLine("\n=== Performance Optimization Example ===");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // First run - builds cache
            using (var manager = new HierarchicalGitIgnoreManager(projectRoot, enableCache: true))
            {
                var files = Directory.GetFiles(projectRoot, "*.*", SearchOption.AllDirectories);
                var ignoredCount = files.Count(f => manager.ShouldIgnore(f, false));
                Console.WriteLine($"First run: Processed {files.Length} files, {ignoredCount} ignored");
            }

            var firstRunTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            // Second run - uses cache
            using (var manager = new HierarchicalGitIgnoreManager(projectRoot, enableCache: true))
            {
                var files = Directory.GetFiles(projectRoot, "*.*", SearchOption.AllDirectories);
                var ignoredCount = files.Count(f => manager.ShouldIgnore(f, false));
                Console.WriteLine($"Second run: Processed {files.Length} files, {ignoredCount} ignored");
            }

            var secondRunTime = stopwatch.ElapsedMilliseconds;

            Console.WriteLine($"Performance: First run: {firstRunTime}ms, Second run: {secondRunTime}ms");
            Console.WriteLine($"Cache speedup: {(double)firstRunTime / secondRunTime:F2}x");
        }

        // Helper methods
        private static bool IsSourceCodeFile(string extension)
        {
            var sourceExtensions = new[] { ".cs", ".vb", ".cpp", ".h", ".hpp", ".js", ".ts", ".py", ".java" };
            return sourceExtensions.Contains(extension.ToLowerInvariant());
        }

        private static void AnalyzeSourceFile(FileInfo fileInfo)
        {
            try
            {
                var lines = File.ReadAllLines(fileInfo.FullName);
                Console.WriteLine($"Analyzed {fileInfo.Name}: {lines.Length} lines, {fileInfo.Length} bytes");
                
                // Add your source code analysis logic here
                // For example: count methods, classes, comments, etc.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error analyzing {fileInfo.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Main method to run all examples
        /// </summary>
        public static void RunAllExamples(string projectRoot)
        {
            if (!Directory.Exists(projectRoot))
            {
                Console.WriteLine($"Project root directory does not exist: {projectRoot}");
                return;
            }

            Console.WriteLine($"Running GitIgnore examples for project: {projectRoot}");
            Console.WriteLine(new string('=', 60));

            try
            {
                BasicGitIgnoreExample(projectRoot);
                FileProcessingExample(projectRoot);
                ExtensionMethodsExample(projectRoot);
                CustomProcessingExample(projectRoot);
                HierarchicalPatternsExample(projectRoot);
                PerformanceOptimizationExample(projectRoot);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running examples: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine(new string('=', 60));
            Console.WriteLine("Examples completed.");
        }
    }
}