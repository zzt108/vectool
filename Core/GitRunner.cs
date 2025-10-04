using NLogShared;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VecTool.Core
{
    /// <summary>
    /// Executes Git commands and returns the output.
    /// </summary>
    public sealed class GitRunner
    {
        private static readonly CtxLogger _log = new();
        private readonly string _workingDirectory;

        public GitRunner(string workingDirectory)
        {
            _workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
        }

        public async Task<string> RunGitCommandAsync(string command, int timeoutSeconds = 60)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Git command is required.", nameof(command));

            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = command,
                WorkingDirectory = _workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = false };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            _log.Info($"Running git command: git {command} in {_workingDirectory}"); // structured logging module wraps NLog [attached_file:1][attached_file:2]

            if (!process.Start())
                throw new InvalidOperationException("Failed to start git process.");

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
#if NET8_0_OR_GREATER
                await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
#else
                // Fallback if needed; this project is .NET 8, so this is mostly a placeholder.
                while (!process.HasExited)
                {
                    if (cts.IsCancellationRequested)
                        break;
                    await Task.Delay(50, cts.Token).ConfigureAwait(false);
                }
#endif
            }
            catch (OperationCanceledException)
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                _log.Warn($"Git command timed out after {timeoutSeconds}s."); // structured warning [attached_file:1][attached_file:2]
                throw new TimeoutException("Git command timed out.");
            }

            // Ensure all async reads are completed.
            process.WaitForExit();

            var exitCode = process.ExitCode;
            var stdOut = outputBuilder.ToString().Trim();
            var stdErr = errorBuilder.ToString().Trim();

            if (exitCode == 0)
            {
                _log.Info("Git command finished successfully."); // info [attached_file:1][attached_file:2]
                return stdOut;
            }

            _log.Warn($"Git command failed with exit code {exitCode}. Error: {stdErr}"); // warn [attached_file:1][attached_file:2]
            throw new InvalidOperationException($"Git command failed: {stdErr}");
        }

        public async Task<string> GetStatusAsync()
        {
            try
            {
                return await RunGitCommandAsync("status --porcelain");
            }
            catch (Exception ex) when (ex.Message.Contains("dubious ownership"))
            {
                var safeDirectoryCommand = $"git config --global --add safe.directory {_workingDirectory}";
                _log.Warn($"Dubious ownership detected in {_workingDirectory}. Solution: {safeDirectoryCommand}");
                return $"Error: Dubious ownership in {_workingDirectory}. Fix with: {safeDirectoryCommand}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> GetDiffAsync()
        {
            var unstaged = await RunGitCommandAsync("diff");
            var staged = await RunGitCommandAsync("diff --cached");

            if (string.IsNullOrWhiteSpace(unstaged) && string.IsNullOrWhiteSpace(staged))
            {
                return "No diff changes.";
            }

            var result = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(unstaged))
            {
                result.AppendLine("## Unstaged Changes");
                result.AppendLine(unstaged);
            }
            if (!string.IsNullOrWhiteSpace(staged))
            {
                result.AppendLine("## Staged Changes");
                result.AppendLine(staged);
            }
            return result.ToString().Trim();
        }

        public async Task<string> GetSubmodulesAsync()
        {
            return await RunGitCommandAsync("submodule status");
        }

        /// <summary>
        /// Gets the current Git branch name from the repository.
        /// </summary>
        /// <returns>Current branch name, or "unknown" if not in a Git repository.</returns>
        public async Task<string> GetCurrentBranchAsync()
        {
            try
            {
                var result = await RunGitCommandAsync("branch --show-current", timeoutSeconds: 5);
                return string.IsNullOrWhiteSpace(result) ? "unknown" : result.Trim();
            }
            catch
            {
                return "unknown";
            }
        }

        public static bool IsGitRepository(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                return false;
            }

            string gitPath = Path.Combine(folderPath, ".git");
            return Directory.Exists(gitPath) || File.Exists(gitPath);
        }
    }
}
