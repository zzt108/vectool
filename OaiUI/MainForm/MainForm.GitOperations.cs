// ✅ FULL FILE VERSION
// File: OaiUI/MainForm.GitOperations.cs

using VecTool.Core;
using VecTool.Core.Helpers;
using VecTool.Handlers;

namespace Vectool.OaiUI
{
    /// <summary>
    /// MainForm partial: Git operations (branch detection, parallel execution).
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// Executes Git changes extraction and MD export in parallel.
        /// </summary>
        private async Task ExecuteGitChangesAndMdParallelAsync(string gitOutputPath, string mdOutputPath)
        {
            userInterface.WorkStart("Generating Git changes and MD export...", selectedFolders);

            var gitHandler = new GitChangesHandler(logger, userInterface, recentFilesManager);
            var mdHandler = new MDHandler(logger, userInterface, recentFilesManager);
            var vectorStoreConfig = GetCurrentVectorStoreConfig();

            // Execute both operations in parallel
            var gitTask = Task.Run(async () => await gitHandler.GetGitChangesAsync(selectedFolders, gitOutputPath, vectorStoreConfig).ConfigureAwait(false));
            var mdTask = mdHandler.ExportSelectedFoldersAsync(mdOutputPath, vectorStoreConfig);

            await Task.WhenAll(gitTask, mdTask).ConfigureAwait(true);
        }

        /// <summary>
        /// Gets the current Git branch name from the selected vector store's repository.
        /// </summary>
        private async Task<string> GetCurrentBranchNameAsync()
        {
            try
            {
                // ✅ Prefer deriving branch from the selected vector store folder's repo, not the app repo.
                var preferredWorkingDir = Configuration.ResolvePreferredWorkingDirectory(GetCurrentVectorStoreConfig().FolderPaths);
                if (!string.IsNullOrWhiteSpace(preferredWorkingDir))
                {
                    var git = new GitRunner(preferredWorkingDir);
                    var branch = await git.GetCurrentBranchAsync().ConfigureAwait(false);
                    return string.IsNullOrWhiteSpace(branch) ? "unknown" : branch;
                }

                // ✅ Fallback to previous behavior: solution directory
                var solutionPath = Configuration.FindSolutionFiles(GetCurrentVectorStoreConfig()).FirstOrDefault();
                var solutionDir = solutionPath is null
                    ? AppDomain.CurrentDomain.BaseDirectory
                    : Path.GetDirectoryName(solutionPath)!;

                var gitFallback = new GitRunner(solutionDir);
                var fallbackBranch = await gitFallback.GetCurrentBranchAsync().ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(fallbackBranch) ? "unknown" : fallbackBranch;
            }
            catch
            {
                return "unknown";
            }
        }
    }
}