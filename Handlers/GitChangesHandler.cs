namespace VecTool.Handlers;

using LogCtxShared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VecTool.Configuration;
using VecTool.Core;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;

/// <summary>
/// Handler for extracting and formatting Git changes from repositories.
/// </summary>
public sealed class GitChangesHandler : FileHandlerBase<GitChangesHandler>
{
    private readonly string _aiPrompt;
    private readonly FileSystemTraverser traverser;

    public GitChangesHandler(ILogger<GitChangesHandler> logger, IUserInterface? ui, IRecentFilesManager? recentFilesManager, string? rootPath = null)
        : base(logger, ui, recentFilesManager)
    {
        _aiPrompt = ConfigurationManager.AppSettings["gitAiPrompt"]
            ?? "Analyze the following Git changes and provide a concise, descriptive commit message.";
        traverser = new FileSystemTraverser(ui);
    }

    /// <summary>
    /// Extracts Git changes from all repositories in the given folders and saves to Markdown.
    /// </summary>
    public async Task<string> GetGitChangesAsync(List<string> folderPaths, string outputPath, VectorStoreConfig config)
    {
        if (folderPaths == null || folderPaths.Count == 0)
            throw new ArgumentException("No folders provided", nameof(folderPaths));

        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path required", nameof(outputPath));

        try
        {
            Ui?.UpdateStatus("Analyzing Git repositories...");
            logger.LogInformation($"Starting Git changes analysis: {folderPaths.Count} folders");

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
                // Use traverser to get only non-excluded folders
                // This ensures folders containing only .git (no exportable files) are detected
                var allowedFolders = traverser
                    .EnumerateFoldersRespectingExclusions(folderPath, config)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                logger.LogDebug($"Found {allowedFolders.Count} non-excluded folders in {folderPath}");

                // Find Git repos ONLY in allowed folders
                var gitRepos = allowedFolders
                    .Where(dir => GitRunner.IsGitRepository(dir))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                using var _ = logger.SetContext().Add("basePath", folderPath);
                logger.LogInformation($"Found {gitRepos.Count} Git repositories to process");

                foreach (var repoPath in gitRepos)
                {
                    await ProcessGitRepositoryAsync(repoPath, outputPath, allChanges, processedRepos);
                }
            }

            await File.WriteAllTextAsync(outputPath, allChanges.ToString());

            // Register generated file
            if (RecentFilesManager != null)
            {
                var fi = new FileInfo(outputPath);
                RecentFilesManager.RegisterGeneratedFile(
                    outputPath,
                    RecentFileType.Git_Md,
                    folderPaths,
                    fi.Exists ? fi.Length : 0);
            }

            Ui?.UpdateStatus($"Git changes saved: {outputPath}");
            logger.LogInformation($"Git changes analysis completed: {outputPath}");

            return allChanges.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to analyze Git changes: {outputPath}");
            throw;
        }
    }

    /// <summary>
    /// Synchronous wrapper for async method.
    /// </summary>
    public string GetGitChanges(List<string> folderPaths, string outputPath, VectorStoreConfig vectorStoreConfig)
        => GetGitChangesAsync(folderPaths, outputPath, vectorStoreConfig).GetAwaiter().GetResult();

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
            logger.LogError(ex, $"LogError processing git repository: {repoPath}");
            mainChanges.AppendLine($"**LogError processing repository:** {ex.Message}");
        }

        mainChanges.AppendLine();
    }
}