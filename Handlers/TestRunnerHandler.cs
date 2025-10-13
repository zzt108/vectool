// ✅ FULL FILE VERSION
// Path: Handlers/TestRunnerHandler.cs
// Migrated from NLogSharedCtxLogger to NLog with message-template logging per guide.

using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VecTool.Core;
using VecTool.Core.Abstractions;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    /// <summary>
    /// Handler for running NUnit tests and capturing their output.
    /// Refactored with dependency injection for testability (Phase 4.1.3.b2.2).
    /// </summary>
    public sealed class TestRunnerHandler : FileHandlerBase
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly IGitRunner gitRunner;
        private readonly IProcessRunner processRunner;

        /// <summary>
        /// Initializes a new instance with explicit dependencies for testability.
        /// </summary>
        /// <param name="gitRunner">Git operations abstraction.</param>
        /// <param name="processRunner">Process execution abstraction.</param>
        /// <param name="ui">Optional UI interface for progress updates.</param>
        /// <param name="recentFilesManager">Optional recent files manager.</param>
        public TestRunnerHandler(
            IGitRunner gitRunner,
            IProcessRunner processRunner,
            IUserInterface? ui,
            IRecentFilesManager? recentFilesManager)
            : base(ui, recentFilesManager)
        {
            this.gitRunner = gitRunner ?? throw new ArgumentNullException(nameof(gitRunner));
            this.processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        }

        /// <summary>
        /// Runs NUnit tests for the specified solution and writes results to the output path.
        /// </summary>
        /// <param name="solutionPath">Path to the solution file containing test projects.</param>
        /// <param name="storeName">Name of the vector store being tested.</param>
        /// <param name="folderPaths">Collection of folder paths involved in the test run.</param>
        /// <param name="cancellationToken">Cancellation token to abort the test run.</param>
        /// <returns>The output file path, or null if the solution doesn't exist or tests fail.</returns>
        public async Task<string?> RunTestsAsync(
            string solutionPath,
            string storeName,
            IReadOnlyList<string> folderPaths,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(solutionPath))
                throw new ArgumentException("Solution path is required.", nameof(solutionPath));
            if (string.IsNullOrWhiteSpace(storeName))
                throw new ArgumentException("Store name is required.", nameof(storeName));
            if (folderPaths == null)
                throw new ArgumentNullException(nameof(folderPaths));

            // Return null if solution doesn't exist (testable behavior)
            if (!File.Exists(solutionPath))
            {
                log.Warn("Solution file not found: {SolutionPath}", solutionPath);
                return null;
            }

            // Determine git branch for metadata
            string? branch = null;
            try
            {
                var repoRoot = RepoLocator.FindRepoRoot(Path.GetDirectoryName(solutionPath)!);
                if (repoRoot != null)
                {
                    branch = await gitRunner.GetCurrentBranchAsync(repoRoot, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                log.Debug(ex, "Could not determine git branch for test run");
            }

            // Generate output path based on store name and timestamp
            var outputDir = Path.Combine(Path.GetDirectoryName(solutionPath)!, "TestResults");
            Directory.CreateDirectory(outputDir);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var outputPath = Path.Combine(outputDir, $"TestRun-{storeName}-{timestamp}.md");

            try
            {
                ui?.UpdateStatus($"Running tests for {Path.GetFileName(solutionPath)}...");
                log.Info("Starting test run for solution {SolutionPath}, store {StoreName}", solutionPath, storeName);

                var solutionDir = Path.GetDirectoryName(solutionPath)
                    ?? throw new InvalidOperationException("Could not determine solution directory.");

                // Use IProcessRunner abstraction for testability
                var result = await processRunner.RunAsync(
                    "dotnet",
                    $"test \"{solutionPath}\" --no-build --logger \"console;verbosity=detailed\"",
                    solutionDir,
                    cancellationToken).ConfigureAwait(false);

                // Return null if tests failed (exit code != 0)
                if (result.ExitCode != 0)
                {
                    log.Warn("Test run completed with exit code {ExitCode} for {SolutionPath}", result.ExitCode, solutionPath);
                    ui?.UpdateStatus($"Tests failed with exit code {result.ExitCode}");
                    return null;
                }

                // Write output to file
                var fullOutput = new StringBuilder();
                fullOutput.AppendLine("# Test Run Results");
                fullOutput.AppendLine();
                fullOutput.AppendLine($"**Solution:** {solutionPath}");
                fullOutput.AppendLine($"**Store:** {storeName}");
                if (branch != null)
                    fullOutput.AppendLine($"**Branch:** {branch}");
                fullOutput.AppendLine($"**Exit Code:** {result.ExitCode}");
                fullOutput.AppendLine($"**Duration:** {result.Duration.TotalSeconds:F2}s");
                fullOutput.AppendLine($"**Timestamp:** {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
                fullOutput.AppendLine();
                fullOutput.AppendLine("## Test Output");
                fullOutput.AppendLine();
                fullOutput.AppendLine(result.StandardOutput);

                if (!string.IsNullOrWhiteSpace(result.StandardError))
                {
                    fullOutput.AppendLine();
                    fullOutput.AppendLine("## Errors");
                    fullOutput.AppendLine();
                    fullOutput.AppendLine(result.StandardError);
                }

                await File.WriteAllTextAsync(outputPath, fullOutput.ToString(), cancellationToken).ConfigureAwait(false);

                // Register with recent files manager
                if (recentFilesManager != null)
                {
                    var fileInfo = new FileInfo(outputPath);
                    recentFilesManager.RegisterGeneratedFile(
                        outputPath,
                        RecentFileType.TestResults,
                        folderPaths.Concat(new[] { solutionPath }).ToArray(),
                        fileInfo.Exists ? fileInfo.Length : 0);
                }

                log.Info("Test run completed successfully for {SolutionPath}", solutionPath);
                ui?.UpdateStatus($"Tests completed. Results saved to {outputPath}");
                return outputPath;
            }
            catch (OperationCanceledException)
            {
                log.Info("Test run cancelled for {SolutionPath}", solutionPath);
                ui?.UpdateStatus("Test run cancelled");
                return null;
            }
            catch (Exception ex)
            {
                var evt = new LogEventInfo(LogLevel.Error, log.Name, "RunTests failed");
                evt.Exception = ex;
                evt.Properties["Solution"] = solutionPath;
                evt.Properties["StoreName"] = storeName;
                log.Log(evt);

                ui?.UpdateStatus($"Test run failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Synchronous wrapper for RunTestsAsync (legacy compatibility).
        /// </summary>
        public string? RunTests(string solutionPath, string storeName, IReadOnlyList<string> folderPaths)
        {
            return RunTestsAsync(solutionPath, storeName, folderPaths, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
