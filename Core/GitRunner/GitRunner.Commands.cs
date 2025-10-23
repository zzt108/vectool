// ✅ FULL FILE VERSION
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VecTool.Core
{
    /// <summary>
    /// Git-specific command wrappers for GitRunner.
    /// </summary>
    public sealed partial class GitRunner
    {
        /// <summary>
        /// Checks if a directory is a Git repository.
        /// </summary>
        public static bool IsGitRepository(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var gitDir = Path.Combine(path, ".git");
            return Directory.Exists(gitDir) || File.Exists(gitDir); // Supports submodules (file reference)
        }

        /// <summary>
        /// Gets the current branch name from the repository or "unknown" on errors.
        /// </summary>
        public async Task<string> GetCurrentBranchAsync(CancellationToken cancellationToken = default)
        {
            return await GetCurrentBranchAsync(workingDirectory, cancellationToken);
        }

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
        /// Gets the git status output (staged, unstaged, untracked files).
        /// </summary>
        public async Task<string> GetStatusAsync()
        {
            return await RunGitCommandAsync("status").ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the git diff output for uncommitted changes.
        /// </summary>
        public async Task<string> GetDiffAsync()
        {
            return await RunGitCommandAsync("diff").ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the status of Git submodules.
        /// </summary>
        public async Task<string> GetSubmodulesAsync()
        {
            return await RunGitCommandAsync("submodule status").ConfigureAwait(false);
        }
    }
}
