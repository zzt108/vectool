// File: Handlers/TestRunnerHandler.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLogShared;
using VecTool.Core;
using VecTool.Core.Abstractions;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    /// <summary>
    /// Runs 'dotnet test', captures outputs, writes a result file, and registers it in Recent Files.
    /// </summary>
    public sealed class TestRunnerHandler : FileHandlerBase
    {
        private readonly IGitRunner gitRunner;
        private readonly IProcessRunner processRunner;

        // Canonical DI-friendly constructor
        public TestRunnerHandler(IGitRunner gitRunner, IProcessRunner processRunner, IUserInterface? ui, IRecentFilesManager? recentFilesManager)
            : base(ui, recentFilesManager)
        {
            this.gitRunner = gitRunner ?? throw new ArgumentNullException(nameof(gitRunner));
            this.processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        }

        // ✅ Back-compat: legacy 2-arg ctor used by existing tests
        public TestRunnerHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
            : this(
                new GitRunner(AppDomain.CurrentDomain.BaseDirectory),
                new ProcessRunner(),
                ui,
                recentFilesManager)
        {
        }

        // ✅ Back-compat: tests passing UI first, then fake git/proc in this order
        public TestRunnerHandler(IUserInterface? ui, IRecentFilesManager? recent, IGitRunner git, IProcessRunner proc)
            : this(git, proc, ui, recent)
        {
        }

        /// <summary>
        /// Run dotnet test against the solution path and write a structured report to Generated folder.
        /// Returns the path on success; null on failure.
        /// </summary>
        public async Task<string?> RunTestsAsync(
//            string solutionPath,
            string storeId,
            IReadOnlyList<string> selectedFolders,
            CancellationToken cancellationToken = default)
        {

            // var solutionDir = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;

            // Derive preferred working directory from selected repositories; else solution folder
            var preferred = RepoLocator.ResolvePreferredWorkingDirectory(selectedFolders);
            if (string.IsNullOrWhiteSpace(preferred) || !Directory.Exists(preferred))
            {
                ui?.ShowMessage($"{preferred} folder not found.", "Run Tests", MessageType.Error);
                return null;
            }

            var workingDir = preferred;

            // Get branch, fallback to unknown
            var branch = "unknown";
            try
            {
                var b = await gitRunner.GetCurrentBranchAsync(workingDir, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(b)) branch = b;
            }
            catch
            {
                // keep unknown
            }

            // Execute tests
            var args = $"test \"{workingDir}\" --nologo --verbosity minimal";
            ProcessResult result;
            try
            {
                result = await processRunner.RunAsync("dotnet", args, workingDir, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ui?.ShowMessage($"Failed to start test run: {ex.Message}", "Run Tests", MessageType.Error);
                return null;
            }

            if (result.ExitCode != 0)
            {
                ui?.ShowMessage("Tests failed. See report for details.", "Run Tests", MessageType.Warning);
            }

            // Write report under Generated
            var outDir = Path.Combine(workingDir, "..\\Generated");
            Directory.CreateDirectory(outDir);
            var fileName = $"test-results.{storeId}.{branch}.md";
            var outPath = Path.Combine(outDir, fileName);

            var sb = new StringBuilder();
            sb.AppendLine($"# dotnet test results");
            sb.AppendLine($"Solution: {Path.GetFileName(workingDir)}");
            sb.AppendLine($"Branch: {branch}");
            sb.AppendLine($"Store: {storeId}");
            sb.AppendLine($"When (UTC): {DateTime.UtcNow:O}");
            sb.AppendLine($"Duration: {result.Duration}");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(result.StandardOutput))
            {
                sb.AppendLine("## Standard Output");
                sb.AppendLine(result.StandardOutput);
            }
            if (!string.IsNullOrEmpty(result.StandardError))
            {
                sb.AppendLine();
                sb.AppendLine("## Standard Error");
                sb.AppendLine(result.StandardError);
            }

            await File.WriteAllTextAsync(outPath, sb.ToString(), cancellationToken).ConfigureAwait(false);

            // Register in Recent Files
            try
            {
                var length = new FileInfo(outPath).Length;
                recentFilesManager?.RegisterGeneratedFile(
                    filePath: outPath,
                    fileType: RecentFileType.TestResults,
                    sourceFolders: selectedFolders,
                    fileSizeBytes: length,
                    generatedAtUtc: DateTime.UtcNow);
            }
            catch
            {
                // best effort only
            }

            return result.ExitCode == 0 ? outPath : null;
        }
    }
}
