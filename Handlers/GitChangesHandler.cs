namespace VecTool.Handlers;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VecTool.Core;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;

/// <summary>
/// Handler for extracting and formatting Git changes from repositories.
/// </summary>
public sealed class GitChangesHandler : FileHandlerBase
{
    private readonly string _aiPrompt;

    public GitChangesHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
        : base(ui, recentFilesManager)
    {
        _aiPrompt = ConfigurationManager.AppSettings["gitAiPrompt"]
            ?? "Analyze the following Git changes and provide a concise, descriptive commit message.";
    }

    /// <summary>
    /// Extracts Git changes from all repositories in the given folders and saves to Markdown.
    /// </summary>
    public async Task<string> GetGitChangesAsync(List<string> folderPaths, string outputPath)
    {
        if (folderPaths == null || folderPaths.Count == 0)
            throw new ArgumentException("No folders provided", nameof(folderPaths));

        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path required", nameof(outputPath));

        try
        {
            ui?.UpdateStatus("Analyzing Git repositories...");
            log.Info($"Starting Git changes analysis: {folderPaths.Count} folders");

            var allChanges = new StringBuilder();
            allChanges.AppendLine("# AI Prompt for Commit Message");
            allChanges.AppendLine();
            allChanges.AppendLine(_aiPrompt);
            allChanges.AppendLine();
            allChanges.AppendLine("---");
            allChanges.AppendLine();

            var processedRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folderPath in folderPaths)
            {
                if (Core.GitRunner.IsGitRepository(folderPath))
                {
                    await ProcessGitRepositoryAsync(folderPath, outputPath, allChanges, processedRepos);
                }
                else
                {
                    await FindGitRepositoriesRecursivelyAsync(folderPath, outputPath, allChanges, processedRepos);
                }
            }

            await File.WriteAllTextAsync(outputPath, allChanges.ToString());

            // Register generated file
            if (_recentFilesManager != null)
            {
                var fi = new FileInfo(outputPath);
                _recentFilesManager.RegisterGeneratedFile(
                    outputPath,
                    RecentFileType.GitChanges,
                    folderPaths,
                    fi.Exists ? fi.Length : 0);
            }

            ui?.UpdateStatus($"Git changes saved: {outputPath}");
            log.Info($"Git changes analysis completed: {outputPath}");

            return allChanges.ToString();
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Failed to analyze Git changes: {outputPath}");
            throw;
        }
    }

    /// <summary>
    /// Synchronous wrapper for async method.
    /// </summary>
    public string GetGitChanges(List<string> folderPaths, string outputPath)
        => GetGitChangesAsync(folderPaths, outputPath).GetAwaiter().GetResult();

    private async Task ProcessGitRepositoryAsync(
        string repoPath,
        string outputPath,
        StringBuilder mainChanges,
        HashSet<string> processedRepos)
    {
        var fullPath = Path.GetFullPath(repoPath);
        
        if (!processedRepos.Add(fullPath))
            return;

        mainChanges.AppendLine($"## Git Changes for: {repoPath}");
        mainChanges.AppendLine();

        var gitRunner = new GitRunner(repoPath);

        try
        {
            // Status
            var statusChanges = await gitRunner.GetStatusAsync();
            mainChanges.AppendLine("### Status Changes");
            mainChanges.AppendLine();
            mainChanges.AppendLine("```");
            mainChanges.AppendLine(statusChanges);
            mainChanges.AppendLine("```");
            mainChanges.AppendLine();

            // Diff
            var diffChanges = await gitRunner.GetDiffAsync();
            mainChanges.AppendLine("### Diff Changes");
            mainChanges.AppendLine();
            mainChanges.AppendLine("```");
            mainChanges.AppendLine(diffChanges);
            mainChanges.AppendLine("```");
            mainChanges.AppendLine();

            // Submodules
            var submodulesOutput = await gitRunner.GetSubmodulesAsync();
            
            if (!string.IsNullOrWhiteSpace(submodulesOutput))
            {
                mainChanges.AppendLine("### Submodules");
                mainChanges.AppendLine();

                var submoduleLines = submodulesOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in submoduleLines)
                {
                    var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (parts.Length >= 2)
                    {
                        var submodulePath = parts[1];
                        var fullSubmodulePath = Path.Combine(repoPath, submodulePath);

                        if (Directory.Exists(fullSubmodulePath))
                        {
                            mainChanges.AppendLine($"- {submodulePath}");

                            if (!GitRunner.IsGitRepository(fullSubmodulePath))
                            {
                                mainChanges.AppendLine($"  - Skipped (submodule not initialized). Run `git submodule update --init --recursive` in {repoPath}.");
                                continue;
                            }

                            var submoduleRunner = new GitRunner(fullSubmodulePath);
                            var submoduleStatus = await submoduleRunner.GetStatusAsync();

                            if (!string.IsNullOrWhiteSpace(submoduleStatus) && 
                                !submoduleStatus.Contains("nothing to commit"))
                            {
                                var submoduleFileName = $"{Path.GetFileNameWithoutExtension(outputPath)}-{Path.GetFileName(repoPath)}-{submodulePath.Replace(Path.DirectorySeparatorChar, '-')}-git-changes.md";
                                var submoduleFilePath = Path.Combine(Path.GetDirectoryName(outputPath)!, submoduleFileName);

                                var submoduleChanges = new StringBuilder();
                                submoduleChanges.AppendLine("# AI Prompt for Commit Message");
                                submoduleChanges.AppendLine();
                                submoduleChanges.AppendLine(_aiPrompt);
                                submoduleChanges.AppendLine();
                                submoduleChanges.AppendLine("---");
                                submoduleChanges.AppendLine();
                                submoduleChanges.AppendLine($"## Git Changes for Submodule: {submodulePath} in {repoPath}");
                                submoduleChanges.AppendLine();

                                submoduleChanges.AppendLine("### Status Changes");
                                submoduleChanges.AppendLine();
                                submoduleChanges.AppendLine("```");
                                submoduleChanges.AppendLine(submoduleStatus);
                                submoduleChanges.AppendLine("```");
                                submoduleChanges.AppendLine();

                                var submoduleDiff = await submoduleRunner.GetDiffAsync();
                                submoduleChanges.AppendLine("### Diff Changes");
                                submoduleChanges.AppendLine();
                                submoduleChanges.AppendLine("```");
                                submoduleChanges.AppendLine(submoduleDiff);
                                submoduleChanges.AppendLine("```");
                                submoduleChanges.AppendLine();

                                await File.WriteAllTextAsync(submoduleFilePath, submoduleChanges.ToString());
                                mainChanges.AppendLine($"  - Changes saved to: {submoduleFileName}");
                            }
                            else
                            {
                                mainChanges.AppendLine("  - No changes");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Error processing git repository: {repoPath}");
            mainChanges.AppendLine($"**Error processing repository:** {ex.Message}");
        }

        mainChanges.AppendLine();
    }

    private async Task FindGitRepositoriesRecursivelyAsync(
        string folderPath,
        string outputPath,
        StringBuilder allChanges,
        HashSet<string> processedRepos)
    {
        try
        {
            foreach (var subDir in Directory.GetDirectories(folderPath))
            {
                var dirName = Path.GetFileName(subDir);

                // Skip common non-repository directories
                if (dirName.StartsWith(".") || 
                    dirName == "node_modules" || 
                    dirName == "bin" || 
                    dirName == "obj")
                    continue;

                if (GitRunner.IsGitRepository(subDir))
                {
                    await ProcessGitRepositoryAsync(subDir, outputPath, allChanges, processedRepos);
                }
                else
                {
                    await FindGitRepositoriesRecursivelyAsync(subDir, outputPath, allChanges, processedRepos);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error(ex, $"Error searching for Git repositories in: {folderPath}");
        }
    }
}
