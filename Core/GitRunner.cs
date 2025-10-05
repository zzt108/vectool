// ✅ FULL FILE VERSION
using NLogShared;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VecTool.Core.Abstractions;

namespace VecTool.Core
{
    /// <summary>
    /// Executes Git commands against a repository and returns outputs or throws on failures.
    /// </summary>
    public sealed class GitRunner : IGitRunner
    {
        private static readonly CtxLogger log = new();
        private readonly string workingDirectory;

        public GitRunner(string workingDirectory)
        {
            this.workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
        }

        /// <summary>
        /// Runs an arbitrary git command in the configured working directory with a timeout.
        /// Throws TimeoutException on timeout and InvalidOperationException on non-zero exit.
        /// </summary>
        public async Task<string> RunGitCommandAsync(string command, int timeoutSeconds = 60)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Git command is required.", nameof(command));

            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = command,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = false
            };

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

            log.Info($"Running git command: git {command} in {workingDirectory}");

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start git process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // ignored
                }

                log.Warn($"Git command timed out after {timeoutSeconds}s.");
                throw new TimeoutException("Git command timed out.");
            }

            // Ensure async reads complete
            process.WaitForExit();

            var exitCode = process.ExitCode;
            var stdOut = outputBuilder.ToString().Trim();
            var stdErr = errorBuilder.ToString().Trim();

            if (exitCode == 0)
            {
                log.Info("Git command finished successfully.");
                return stdOut;
            }

            log.Warn($"Git command failed with exit code {exitCode}. Error: {stdErr}");
            throw new InvalidOperationException($"Git command failed: {stdErr}");
        }

        /// <summary>
        /// Gets the git status (porcelain) of the repository, returning an error string for
        /// well-known issues like “dubious ownership” instead of throwing.
        /// </summary>
        public async Task<string> GetStatusAsync()
        {
            try
            {
                return await RunGitCommandAsync("status --porcelain").ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.Message.Contains("dubious ownership", StringComparison.OrdinalIgnoreCase))
            {
                var safeDirectoryCommand = $"git config --global --add safe.directory \"{workingDirectory}\"";
                log.Warn($"Dubious ownership detected in {workingDirectory}. Solution: {safeDirectoryCommand}");
                return $"Error: Dubious ownership in {workingDirectory}. Fix with: {safeDirectoryCommand}";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets combined diff of unstaged and staged changes with simple headings.
        /// </summary>
        public async Task<string> GetDiffAsync()
        {
            var unstaged = await RunGitCommandAsync("diff").ConfigureAwait(false);
            var staged = await RunGitCommandAsync("diff --cached").ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(unstaged) && string.IsNullOrWhiteSpace(staged))
                return "No diff changes.";

            var result = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(unstaged))
            {
                result.AppendLine("Unstaged Changes");
                result.AppendLine(unstaged);
            }

            if (!string.IsNullOrWhiteSpace(staged))
            {
                result.AppendLine("Staged Changes");
                result.AppendLine(staged);
            }

            return result.ToString().Trim();
        }

        /// <summary>
        /// Gets the status of Git submodules.
        /// </summary>
        public async Task<string> GetSubmodulesAsync()
        {
            return await RunGitCommandAsync("submodule status").ConfigureAwait(false);
        }

        /// <summary>
        /// Convenience overload for UI code that doesn’t pass a working directory explicitly.
        /// Forwards to the interface method using the instance’s configured working directory.
        /// </summary>
        public Task<string> GetCurrentBranchAsync(CancellationToken cancellationToken = default)
            => GetCurrentBranchAsync(workingDirectory, cancellationToken);

        /// <summary>
        /// Returns the current branch name from the repository or "unknown" on errors.
        /// </summary>
        public async Task<string> GetCurrentBranchAsync(string workingDirectory, CancellationToken cancellationToken = default)
        {
            try
            {
                // Use an explicit runner bound to the provided directory to honor the parameter.
                var runner = new GitRunner(workingDirectory);
                var result = await runner.RunGitCommandAsync("branch --show-current").ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(result) ? "unknown" : result.Trim();
            }
            catch
            {
                return "unknown";
            }
        }

        /// <summary>
        /// Checks if the provided folder is a Git repository by looking for a .git marker.
        /// </summary>
        public static bool IsGitRepository(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return false;

            var gitPath = Path.Combine(folderPath, ".git");
            return Directory.Exists(gitPath) || File.Exists(gitPath);
        }
    }
}
