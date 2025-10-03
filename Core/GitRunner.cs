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
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = command,
                    WorkingDirectory = _workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                }
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            using var outputWaitHandle = new AutoResetEvent(false);
            using var errorWaitHandle = new AutoResetEvent(false);

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                {
                    outputWaitHandle.Set();
                }
                else
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                {
                    errorWaitHandle.Set();
                }
                else
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            _log.Info($"Running git command: 'git {command}' in '{_workingDirectory}'");
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            if (process.WaitForExit((int)timeout.TotalMilliseconds) &&
                outputWaitHandle.WaitOne(timeout) &&
                errorWaitHandle.WaitOne(timeout))
            {
                if (process.ExitCode == 0)
                {
                    _log.Info("Git command finished successfully.");
                    return outputBuilder.ToString().Trim();
                }

                var errorOutput = errorBuilder.ToString().Trim();
                _log.Warn($"Git command failed with exit code {process.ExitCode}. Error: {errorOutput}");
                throw new InvalidOperationException($"Git command failed: {errorOutput}");
            }

            _log.Warn("Git command timed out.");
            if (!process.HasExited)
            {
                process.Kill();
            }
            throw new TimeoutException("Git command timed out.");
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
