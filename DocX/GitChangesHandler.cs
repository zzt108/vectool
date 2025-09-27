using Core;
using NLogS = NLogShared;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;

namespace DocXHandler
{
    public class GitChangesHandler : FileHandlerBase
    {
        private static NLogS.CtxLogger log = new();
        private string aiPrompt = "";

        public GitChangesHandler(IUserInterface? ui) : base(ui)
        {
        }

        public async Task<string> GetGitChangesAsync(List<string> folderPaths, string outputPath)
        {
            // Get the AI prompt from app.config
            aiPrompt = ConfigurationManager.AppSettings["gitAiPrompt"] ??
                       "Analyze the following Git changes and provide a concise, descriptive commit message.";

            StringBuilder allChanges = new StringBuilder();

            // Add the AI prompt at the beginning of the document
            allChanges.AppendLine("# AI Prompt for Commit Message");
            allChanges.AppendLine();
            allChanges.AppendLine(aiPrompt);
            allChanges.AppendLine();
            allChanges.AppendLine("---");
            allChanges.AppendLine();

            // Track processed repositories to avoid duplicates
            HashSet<string> processedRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folderPath in folderPaths)
            {
                if (GitRunner.IsGitRepository(folderPath))
                {
                    await ProcessGitRepositoryAsync(folderPath, outputPath, allChanges, processedRepos);
                }
                else
                {
                    // Check subdirectories for Git repositories
                    await FindGitRepositoriesRecursivelyAsync(folderPath, outputPath, allChanges, processedRepos);
                }
            }

            // Save all changes to the output file
            await File.WriteAllTextAsync(outputPath, allChanges.ToString());
            return allChanges.ToString();
        }

        // Legacy sync method for backwards compatibility
        public string GetGitChanges(List<string> folderPaths, string outputPath)
        {
            return GetGitChangesAsync(folderPaths, outputPath).GetAwaiter().GetResult();
        }

        private async Task ProcessGitRepositoryAsync(string repoPath, string outputPath, StringBuilder mainChanges, HashSet<string> processedRepos)
        {
            // Skip if already processed
            if (!processedRepos.Add(Path.GetFullPath(repoPath))) return;

            // Process the main repository
            mainChanges.AppendLine($"## Git Changes for {repoPath}");
            mainChanges.AppendLine();

            var gitRunner = new GitRunner(repoPath);

            try
            {
                // Get status changes
                string statusChanges = await gitRunner.GetStatusAsync();
                mainChanges.AppendLine("### Status Changes");
                mainChanges.AppendLine();
                mainChanges.AppendLine(statusChanges);
                mainChanges.AppendLine();

                // Get diff changes
                string diffChanges = await gitRunner.GetDiffAsync();
                mainChanges.AppendLine("### Diff Changes");
                mainChanges.AppendLine();
                mainChanges.AppendLine(diffChanges);
                mainChanges.AppendLine();

                // Check for submodules
                string submodulesOutput = await gitRunner.GetSubmodulesAsync();
                if (!string.IsNullOrWhiteSpace(submodulesOutput))
                {
                    mainChanges.AppendLine("### Submodules");
                    mainChanges.AppendLine();

                    // Parse submodule paths
                    var submoduleLines = submodulesOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in submoduleLines)
                    {
                        // Submodule format is typically "sha1 path (description)"
                        string[] parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            string submodulePath = parts[1];
                            string fullSubmodulePath = Path.Combine(repoPath, submodulePath);

                            if (Directory.Exists(fullSubmodulePath))
                            {
                                mainChanges.AppendLine($"- {submodulePath}");

                                // Check if the submodule has changes
                                var submoduleRunner = new GitRunner(fullSubmodulePath);
                                string submoduleStatus = await submoduleRunner.GetStatusAsync();

                                if (!string.IsNullOrWhiteSpace(submoduleStatus) &&
                                    !submoduleStatus.Contains("nothing to commit, working tree clean"))
                                {
                                    // Create a separate file for the submodule changes
                                    string submoduleFileName = $"{Path.GetFileNameWithoutExtension(outputPath)}-{Path.GetFileName(repoPath)}-{submodulePath.Replace(Path.DirectorySeparatorChar, '-')}-git-changes.md";
                                    string submoduleFilePath = Path.Combine(Path.GetDirectoryName(outputPath)!, submoduleFileName);

                                    StringBuilder submoduleChanges = new StringBuilder();

                                    // Add the AI prompt at the beginning of the document
                                    submoduleChanges.AppendLine("# AI Prompt for Commit Message");
                                    submoduleChanges.AppendLine();
                                    submoduleChanges.AppendLine(aiPrompt);
                                    submoduleChanges.AppendLine();
                                    submoduleChanges.AppendLine("---");
                                    submoduleChanges.AppendLine();

                                    submoduleChanges.AppendLine($"## Git Changes for Submodule {submodulePath} in {repoPath}");
                                    submoduleChanges.AppendLine();

                                    // Get status changes for submodule
                                    submoduleChanges.AppendLine("### Status Changes");
                                    submoduleChanges.AppendLine();
                                    submoduleChanges.AppendLine(submoduleStatus);
                                    submoduleChanges.AppendLine();

                                    // Get diff changes for submodule
                                    string submoduleDiff = await submoduleRunner.GetDiffAsync();
                                    submoduleChanges.AppendLine("### Diff Changes");
                                    submoduleChanges.AppendLine();
                                    submoduleChanges.AppendLine(submoduleDiff);
                                    submoduleChanges.AppendLine();

                                    // Save the submodule changes to a separate file
                                    await File.WriteAllTextAsync(submoduleFilePath, submoduleChanges.ToString());

                                    // Add a reference to the submodule file in the main changes
                                    mainChanges.AppendLine($"  - Changes saved to {submoduleFileName}");
                                }
                                else
                                {
                                    mainChanges.AppendLine("  - No changes");
                                }
                            }
                        }
                    }

                    mainChanges.AppendLine();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error processing git repository {repoPath}");
                mainChanges.AppendLine($"Error processing repository: {ex.Message}");
                mainChanges.AppendLine();
            }
        }

        private async Task FindGitRepositoriesRecursivelyAsync(string folderPath, string outputPath, StringBuilder allChanges, HashSet<string> processedRepos)
        {
            try
            {
                foreach (var subDir in Directory.GetDirectories(folderPath))
                {
                    // Skip excluded folders like .git, node_modules, etc.
                    string dirName = Path.GetFileName(subDir);
                    if (dirName.StartsWith(".") || dirName == "node_modules" || dirName == "bin" || dirName == "obj")
                        continue;

                    if (GitRunner.IsGitRepository(subDir))
                    {
                        await ProcessGitRepositoryAsync(subDir, outputPath, allChanges, processedRepos);
                    }
                    else
                    {
                        // Continue searching in subdirectories
                        await FindGitRepositoriesRecursivelyAsync(subDir, outputPath, allChanges, processedRepos);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error searching for Git repositories in {folderPath}");
            }
        }
    }
}
