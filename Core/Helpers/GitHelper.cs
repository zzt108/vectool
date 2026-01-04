#nullable enable

using LogCtxShared;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using VecTool.Configuration.Logging;

namespace VecTool.Core.Helpers
{
    /// <summary>
    /// Static helper class for executing common Git commands via Process.Start.
    /// </summary>
    public static class GitHelper
    {
        private static readonly ILogger logger =
            AppLogger.Create("GitHelper");

        /// <summary>
        /// Executes 'git diff' and returns the unstaged changes.
        /// </summary>
        /// <param name="repoPath">Repository root path.</param>
        /// <returns>Git diff output or empty string on error.</returns>
        public static string GetUnstagedChanges(string repoPath)
        {
            using var ctx = logger.SetContext(new Props().Add("repoPath", repoPath));

            if (!IsGitRepository(repoPath))
            {
                logger.LogWarning($"Path is not a git repository: {repoPath}");
                return string.Empty;
            }

            try
            {
                return ExecuteGitCommand(repoPath, "diff");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get unstaged changes");
                return string.Empty;
            }
        }

        /// <summary>
        /// Executes 'git status --porcelain' and returns the list of changed files.
        /// </summary>
        /// <param name="repoPath">Repository root path.</param>
        /// <returns>List of changed file paths.</returns>
        public static List<string> GetChangedFiles(string repoPath)
        {
            using var ctx = logger.SetContext(new Props().Add("repoPath", repoPath));

            if (!IsGitRepository(repoPath))
            {
                logger.LogWarning($"Path is not a git repository: {repoPath}");
                return new List<string>();
            }

            try
            {
                var output = ExecuteGitCommand(repoPath, "status --porcelain");
                return output
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Length > 3 ? line.Substring(3).Trim() : string.Empty)
                    .Where(file => !string.IsNullOrWhiteSpace(file))
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get changed files");
                return new List<string>();
            }
        }

        /// <summary>
        /// Checks if a directory is a valid git repository.
        /// </summary>
        /// <param name="path">Directory path to check.</param>
        /// <returns>True if .git folder/file exists, false otherwise.</returns>
        public static bool IsGitRepository(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return false;
            }

            var gitDir = Path.Combine(path, ".git");
            return Directory.Exists(gitDir) || File.Exists(gitDir); // Supports submodules (file reference)
        }

        /// <summary>
        /// Executes a git command and returns the standard output.
        /// </summary>
        private static string ExecuteGitCommand(string workingDirectory, string arguments)
        {
            using var ctx = logger.SetContext(new Props()
                .Add("workingDirectory", workingDirectory)
                .Add("arguments", arguments));

            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start git process");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                logger.LogWarning($"Git command failed with exit code {process.ExitCode}: {error}");
                return string.Empty;
            }

            logger.LogDebug($"Git command succeeded: {arguments}");
            return output.Trim();
        }
    }
}