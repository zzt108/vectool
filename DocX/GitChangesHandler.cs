using Core;
using NLogS = NLogShared;
using System.Configuration;
using System.Text;
using DocXHandler.RecentFiles;

namespace DocXHandler
{
    public class GitChangesHandler : FileHandlerBase
    {
        private static NLogS.CtxLogger log = new();
        private string aiPrompt;

        public GitChangesHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager) : base(ui, recentFilesManager)
        {
        }

        public async Task<string> GetGitChangesAsync(List<string> folderPaths, string outputPath)
        {
            aiPrompt = ConfigurationManager.AppSettings["gitAiPrompt"] ?? "Analyze the following Git changes and provide a concise, descriptive commit message."; // [attached_file:1]

            var allChanges = new StringBuilder();
            allChanges.AppendLine("AI Prompt for Commit Message");
            allChanges.AppendLine();
            allChanges.AppendLine(aiPrompt);
            allChanges.AppendLine();
            allChanges.AppendLine("---");
            allChanges.AppendLine(); // [attached_file:1]

            var processedRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var folderPath in folderPaths)
            {
                if (GitRunner.IsGitRepository(folderPath))
                {
                    await ProcessGitRepositoryAsync(folderPath, outputPath, allChanges, processedRepos); // [attached_file:1]
                }
                else
                {
                    await FindGitRepositoriesRecursivelyAsync(folderPath, outputPath, allChanges, processedRepos); // [attached_file:1]
                }
            }

            await File.WriteAllTextAsync(outputPath, allChanges.ToString()); // [attached_file:1]

            // Register generated file - legitimate
            if (_recentFilesManager != null && File.Exists(outputPath))
            {
                var fileInfo = new FileInfo(outputPath);
                _recentFilesManager.RegisterGeneratedFile(
                    outputPath,
                    RecentFileType.Md,
                    folderPaths,
                    fileInfo.Length
                );
            }

            return allChanges.ToString(); // [attached_file:1]
        }

        // Legacy sync wrapper
        public string GetGitChanges(List<string> folderPaths, string outputPath)
            => GetGitChangesAsync(folderPaths, outputPath).GetAwaiter().GetResult(); // [attached_file:1]

        private async Task ProcessGitRepositoryAsync(string repoPath, string outputPath, StringBuilder mainChanges, HashSet<string> processedRepos)
        {
            if (!processedRepos.Add(Path.GetFullPath(repoPath)))
                return; // [attached_file:1]

            mainChanges.AppendLine($"Git Changes for {repoPath}");
            mainChanges.AppendLine(); // [attached_file:1]

            var gitRunner = new GitRunner(repoPath);
            try
            {
                // Status
                var statusChanges = await gitRunner.GetStatusAsync();
                mainChanges.AppendLine("Status Changes");
                mainChanges.AppendLine();
                mainChanges.AppendLine(statusChanges);
                mainChanges.AppendLine(); // [attached_file:1]

                // Diff
                var diffChanges = await gitRunner.GetDiffAsync();
                mainChanges.AppendLine("Diff Changes");
                mainChanges.AppendLine();
                mainChanges.AppendLine(diffChanges);
                mainChanges.AppendLine(); // [attached_file:1]

                // Submodules
                var submodulesOutput = await gitRunner.GetSubmodulesAsync();
                if (!string.IsNullOrWhiteSpace(submodulesOutput))
                {
                    mainChanges.AppendLine("Submodules");
                    mainChanges.AppendLine(); // [attached_file:1]

                    var submoduleLines = submodulesOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in submoduleLines)
                    {
                        // Typical: "<sha> <path> (desc)" or with prefix markers
                        var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            var submodulePath = parts[1];
                            var fullSubmodulePath = Path.Combine(repoPath, submodulePath);
                            if (Directory.Exists(fullSubmodulePath))
                            {
                                mainChanges.AppendLine($"- {submodulePath}"); // [attached_file:1]

                                // GUARD: if the submodule is not an actual git repository, skip gracefully
                                if (!GitRunner.IsGitRepository(fullSubmodulePath))
                                {
                                    mainChanges.AppendLine($"  - Skipped: submodule not initialized. Run `git submodule update --init --recursive` in `{repoPath}`."); // [attached_file:1][web:11][web:7]
                                    continue;
                                }

                                var submoduleRunner = new GitRunner(fullSubmodulePath);
                                var submoduleStatus = await submoduleRunner.GetStatusAsync();
                                if (!string.IsNullOrWhiteSpace(submoduleStatus) && !submoduleStatus.Contains("nothing to commit"))
                                {
                                    // Save submodule changes to a separate file
                                    var submoduleFileName =
                                        $"{Path.GetFileNameWithoutExtension(outputPath)}-{Path.GetFileName(repoPath)}-{submodulePath.Replace(Path.DirectorySeparatorChar, '-')}-git-changes.md";
                                    var submoduleFilePath = Path.Combine(Path.GetDirectoryName(outputPath)!, submoduleFileName); // [attached_file:1]

                                    var submoduleChanges = new StringBuilder();
                                    submoduleChanges.AppendLine("AI Prompt for Commit Message");
                                    submoduleChanges.AppendLine();
                                    submoduleChanges.AppendLine(aiPrompt);
                                    submoduleChanges.AppendLine();
                                    submoduleChanges.AppendLine("---");
                                    submoduleChanges.AppendLine();
                                    submoduleChanges.AppendLine($"Git Changes for Submodule {submodulePath} in {repoPath}");
                                    submoduleChanges.AppendLine(); // [attached_file:1]

                                    submoduleChanges.AppendLine("Status Changes");
                                    submoduleChanges.AppendLine();
                                    submoduleChanges.AppendLine(submoduleStatus);
                                    submoduleChanges.AppendLine(); // [attached_file:1]

                                    var submoduleDiff = await submoduleRunner.GetDiffAsync();
                                    submoduleChanges.AppendLine("Diff Changes");
                                    submoduleChanges.AppendLine();
                                    submoduleChanges.AppendLine(submoduleDiff);
                                    submoduleChanges.AppendLine(); // [attached_file:1]

                                    await File.WriteAllTextAsync(submoduleFilePath, submoduleChanges.ToString());
                                    mainChanges.AppendLine($"  - Changes saved to {submoduleFileName}"); // [attached_file:1]
                                }
                                else
                                {
                                    mainChanges.AppendLine("  - No changes"); // [attached_file:1]
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error processing git repository {repoPath}");
                mainChanges.AppendLine($"Error processing repository: {ex.Message}"); // [attached_file:1]
                mainChanges.AppendLine(); // [attached_file:1]
            }
        }

        private async Task FindGitRepositoriesRecursivelyAsync(string folderPath, string outputPath, StringBuilder allChanges, HashSet<string> processedRepos)
        {
            try
            {
                foreach (var subDir in Directory.GetDirectories(folderPath))
                {
                    var dirName = Path.GetFileName(subDir);
                    if (dirName.StartsWith(".") || dirName == "node_modules" || dirName == "bin" || dirName == "obj")
                        continue; // [attached_file:1]

                    if (GitRunner.IsGitRepository(subDir))
                    {
                        await ProcessGitRepositoryAsync(subDir, outputPath, allChanges, processedRepos); // [attached_file:1]
                    }
                    else
                    {
                        await FindGitRepositoriesRecursivelyAsync(subDir, outputPath, allChanges, processedRepos); // [attached_file:1]
                    }
                }
                // Register generated file - maybe not legit? No file writes here
                if (_recentFilesManager != null && File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    _recentFilesManager.RegisterGeneratedFile(
                        outputPath,
                        RecentFileType.Md,
                        new List<string> { folderPath },
                        fileInfo.Length
                    );
                }

            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error searching for Git repositories in {folderPath}"); // [attached_file:1]
            }
        }
    }
}
