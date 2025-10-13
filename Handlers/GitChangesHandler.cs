// Handlers/GitChangesHandler.cs
// Migrated from NLogShared/CtxLogger to NLog with message-template logging per guide.

using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VecTool.Core;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    /// <summary>
    /// Handler for extracting and formatting Git changes from repositories.
    /// This class is the entry point and orchestrator, delegating complex tasks
    /// like submodule processing and recursive searching to other partial classes.
    /// </summary>
    public partial class GitChangesHandler : FileHandlerBase
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly string aiPrompt;

        public GitChangesHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
            : base(ui, recentFilesManager)
        {
            // Fallback to a default prompt if not configured in app.config
            aiPrompt = ConfigurationManager.AppSettings["gitAiPrompt"]
                ?? "Analyze the following Git changes and provide a concise, descriptive commit message.";
        }

        /// <summary>
        /// Extracts Git changes from all repositories in the given folders and saves the result
        /// to a Markdown file. It acts as an orchestrator, handling both root-level repositories
        /// and recursively discovered ones.
        /// </summary>
        public async Task<string> GetGitChangesAsync(List<string> folderPaths, string outputPath)
        {
            if (folderPaths == null || !folderPaths.Any())
                throw new ArgumentException("No folders were provided.", nameof(folderPaths));

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("An output path is required.", nameof(outputPath));

            try
            {
                ui?.UpdateStatus("Analyzing Git repositories...");
                log.Info("Starting Git changes analysis for {FolderCount} folders.", folderPaths.Count);

                var allChanges = new StringBuilder();
                allChanges.AppendLine("# AI Prompt for Commit Message");
                allChanges.AppendLine();
                allChanges.AppendLine(aiPrompt);
                allChanges.AppendLine();
                allChanges.AppendLine("---");
                allChanges.AppendLine();

                var processedRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var folderPath in folderPaths)
                {
                    if (GitRunner.IsGitRepository(folderPath))
                    {
                        await ProcessGitRepositoryAsync(folderPath, outputPath, allChanges, processedRepos);
                    }
                    else
                    {
                        // Delegate recursive search to the RepositoryTraversal partial class
                        var nestedRepos = await FindGitRepositoriesRecursivelyAsync(folderPath);
                        foreach (var repo in nestedRepos)
                        {
                            await ProcessGitRepositoryAsync(repo, outputPath, allChanges, processedRepos);
                        }
                    }
                }

                await File.WriteAllTextAsync(outputPath, allChanges.ToString());

                // Register the generated file with the Recent Files manager
                if (recentFilesManager != null)
                {
                    var fileInfo = new FileInfo(outputPath);
                    recentFilesManager.RegisterGeneratedFile(
                        outputPath,
                        RecentFileType.GitChanges,
                        folderPaths,
                        fileInfo.Exists ? fileInfo.Length : 0);
                }

                ui?.UpdateStatus($"Git changes analysis complete. Saved to {outputPath}");
                log.Info("Git changes analysis successfully completed. Output {OutputPath}", outputPath);
                return allChanges.ToString();
            }
            catch (Exception ex)
            {
                var evt = new LogEventInfo(LogLevel.Error, log.Name, "Failed to complete Git changes analysis for output path");
                evt.Exception = ex;
                evt.Properties["OutputPath"] = outputPath;
                log.Log(evt);
                // Rethrow to allow the UI layer to handle and display the error
                throw;
            }
        }

        /// <summary>
        /// Provides a synchronous wrapper for the async GetGitChangesAsync method, primarily for legacy compatibility.
        /// </summary>
        public string GetGitChanges(List<string> folderPaths, string outputPath)
            => GetGitChangesAsync(folderPaths, outputPath).GetAwaiter().GetResult();

        /// <summary>
        /// Processes a single Git repository, gathering its status, diff, and delegating submodule processing.
        /// </summary>
        private async Task ProcessGitRepositoryAsync(
            string repoPath,
            string outputPath,
            StringBuilder mainChanges,
            HashSet<string> processedRepos)
        {
            var fullPath = Path.GetFullPath(repoPath);
            if (!processedRepos.Add(fullPath))
            {
                log.Trace("Skipping already processed repository {RepoPath}", fullPath);
                return;
            }
            // Avoids processing the same repository twice if selected in multiple ways.

            mainChanges.AppendLine($"## Git Changes for `{repoPath}`");
            mainChanges.AppendLine();

            var gitRunner = new GitRunner(repoPath);

            try
            {
                // Get repository status
                var statusChanges = await gitRunner.GetStatusAsync();

                mainChanges.AppendLine("### Status Changes");
                mainChanges.AppendLine();
                mainChanges.AppendLine(string.IsNullOrWhiteSpace(statusChanges) ? "No status changes." : statusChanges);
                mainChanges.AppendLine();

                // Get repository diff
                var diffChanges = await gitRunner.GetDiffAsync();

                mainChanges.AppendLine("### Diff Changes");
                mainChanges.AppendLine();
                mainChanges.AppendLine(string.IsNullOrWhiteSpace(diffChanges) ? "No diff changes." : diffChanges);
                mainChanges.AppendLine();

                // Delegate submodule processing to the SubmoduleProcessor partial class
                await ProcessSubmodulesAsync(repoPath, outputPath, mainChanges);
            }
            catch (Exception ex)
            {
                var evt = new LogEventInfo(LogLevel.Error, log.Name, "Error processing git repository");
                evt.Exception = ex;
                evt.Properties["RepoPath"] = repoPath;
                log.Log(evt);
                mainChanges.AppendLine($"*Error processing repository: {ex.Message}*");
                mainChanges.AppendLine();
            }
        }
    }
}
