using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Core
{
    /// <summary>
    /// Async git command runner to avoid StandardOutput deadlocks.
    /// </summary>
    public sealed class GitRunner
    {
        private readonly string _workingDirectory;

        public GitRunner(string workingDirectory)
        {
            _workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
        }

        public async Task<string> RunGitCommandAsync(string arguments, int timeoutSeconds = 60)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = _workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();

                // Read both streams in parallel to avoid deadlock
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // Wait for process to complete with timeout
                if (!process.WaitForExit(timeoutSeconds * 1000))
                {
                    process.Kill();
                    throw new TimeoutException($"Git command timed out after {timeoutSeconds} seconds: git {arguments}");
                }

                // Get results from both streams
                string output = await outputTask;
                string error = await errorTask;

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Git command failed (exit code {process.ExitCode}): {error}");
                }

                return output.Trim();
            }
            catch (Exception ex) when (!(ex is TimeoutException || ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Error executing git command: {ex.Message}", ex);
            }
            finally
            {
                process?.Dispose();
            }
        }

        public async Task<string> GetStatusAsync()
        {
            return await RunGitCommandAsync("status --porcelain");
        }

        public async Task<string> GetDiffAsync()
        {
            var unstaged = await RunGitCommandAsync("diff");
            var staged = await RunGitCommandAsync("diff --cached");

            if (string.IsNullOrWhiteSpace(unstaged) && string.IsNullOrWhiteSpace(staged))
            {
                return "No diff changes.";
            }

            var result = "";
            if (!string.IsNullOrWhiteSpace(unstaged))
            {
                result += "Unstaged Changes:\n" + unstaged + "\n\n";
            }
            if (!string.IsNullOrWhiteSpace(staged))
            {
                result += "Staged Changes:\n" + staged + "\n\n";
            }

            return result.Trim();
        }

        public async Task<string> GetSubmodulesAsync()
        {
            return await RunGitCommandAsync("submodule status");
        }

        public static bool IsGitRepository(string folderPath)
        {
            string gitPath = System.IO.Path.Combine(folderPath, ".git");
            return System.IO.Directory.Exists(gitPath) || System.IO.File.Exists(gitPath);
        }
    }
}
