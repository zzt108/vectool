using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using NLogS = NLogShared;

namespace DocXHandler
{
    public class GitChangesHandler(IUserInterface ui) : FileHandlerBase(ui)
    {
        private static NLogS.CtxLogger _log = new();
        private string _aiPrompt;

        public string GetGitChanges(List<string> folderPaths, string outputPath)
        {
            // Get the AI prompt from app.config
            _aiPrompt = ConfigurationManager.AppSettings["gitAiPrompt"] ??
                "Analyze the following Git changes and provide a concise, descriptive commit message.";

            StringBuilder allChanges = new StringBuilder();

            // Add the AI prompt at the beginning of the document
            allChanges.AppendLine("# AI Prompt for Commit Message");
            allChanges.AppendLine();
            allChanges.AppendLine(_aiPrompt);
            allChanges.AppendLine();
            allChanges.AppendLine("---");
            allChanges.AppendLine();

            // Track processed repositories to avoid duplicates
            HashSet<string> processedRepos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folderPath in folderPaths)
            {
                if (IsGitRepository(folderPath))
                {
                    ProcessGitRepository(folderPath, outputPath, allChanges, processedRepos);
                }
                else
                {
                    // Check subdirectories for Git repositories
                    FindGitRepositoriesRecursively(folderPath, outputPath, allChanges, processedRepos);
                }
            }

            // Save all changes to the output file
            File.WriteAllText(outputPath, allChanges.ToString());

            return allChanges.ToString();
        }

        private void ProcessGitRepository(string repoPath, string outputPath, StringBuilder mainChanges, HashSet<string> processedRepos)
        {
            // Skip if already processed
            if (!processedRepos.Add(Path.GetFullPath(repoPath)))
            {
                return;
            }

            // Process the main repository
            mainChanges.AppendLine($"# Git Changes for: {repoPath}");
            mainChanges.AppendLine();

            // Get status changes
            string statusChanges = GetGitStatus(repoPath);
            mainChanges.AppendLine("## Status Changes");
            mainChanges.AppendLine("```");
        
            mainChanges.AppendLine(statusChanges);
            mainChanges.AppendLine("```");
            mainChanges.AppendLine();

            // Get diff changes
            string diffChanges = GetGitDiff(repoPath);
            mainChanges.AppendLine("## Diff Changes");
            mainChanges.AppendLine("```");


            mainChanges.AppendLine(diffChanges);
            mainChanges.AppendLine("```");
            mainChanges.AppendLine();

            // Check for submodules
            string submodulesOutput = ExecuteGitCommand(repoPath, "submodule status");
            if (!string.IsNullOrWhiteSpace(submodulesOutput))
            {
                mainChanges.AppendLine("## Submodules");
                mainChanges.AppendLine();

                // Parse submodule paths
                var submoduleLines = submodulesOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in submoduleLines)
                {
                    // Submodule format is typically: [+]<sha1> <path> (<description>)
                    string[] parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        string submodulePath = parts[1];
                        string fullSubmodulePath = Path.Combine(repoPath, submodulePath);

                        if (Directory.Exists(fullSubmodulePath))
                        {
                            mainChanges.AppendLine($"- {submodulePath}");

                            // Check if the submodule has changes
                            string submoduleStatus = GetGitStatus(fullSubmodulePath);
                            if (!string.IsNullOrWhiteSpace(submoduleStatus) &&
                                !submoduleStatus.Contains("nothing to commit, working tree clean"))
                            {
                                // Create a separate file for the submodule changes
                                //string submoduleFileName = Path.GetFileNameWithoutExtension(outputPath) +
                                //    $"-{Path.GetFileName(repoPath)}-{submodulePath.Replace('/', '-')}-git-changes.md";
                                string submoduleFileName = $"{Path.GetFileName(repoPath)}-{submodulePath.Replace('/', '-')}-git-changes.md";
                                string submoduleFilePath = Path.Combine(Path.GetDirectoryName(outputPath), submoduleFileName);

                                StringBuilder submoduleChanges = new StringBuilder();

                                // Add the AI prompt at the beginning of the document
                                submoduleChanges.AppendLine("# AI Prompt for Commit Message");
                                submoduleChanges.AppendLine();
                                submoduleChanges.AppendLine(_aiPrompt);
                                submoduleChanges.AppendLine();
                                submoduleChanges.AppendLine("---");
                                submoduleChanges.AppendLine();

                                submoduleChanges.AppendLine($"# Git Changes for Submodule: {submodulePath} in {repoPath}");
                                submoduleChanges.AppendLine();

                                // Get status changes for submodule
                                submoduleChanges.AppendLine("## Status Changes");
                                submoduleChanges.AppendLine("```");


                                submoduleChanges.AppendLine(submoduleStatus);
                                submoduleChanges.AppendLine("```");
                                submoduleChanges.AppendLine();

                                // Get diff changes for submodule
                                string submoduleDiff = GetGitDiff(fullSubmodulePath);
                                submoduleChanges.AppendLine("## Diff Changes");
                                submoduleChanges.AppendLine("```");


                                submoduleChanges.AppendLine(submoduleDiff);
                                submoduleChanges.AppendLine("```");

                                // Save the submodule changes to a separate file
                                File.WriteAllText(submoduleFilePath, submoduleChanges.ToString());

                                // Add a reference to the submodule file in the main changes
                                mainChanges.AppendLine($"  - Changes saved to: {submoduleFileName}");
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

        private bool IsGitRepository(string folderPath)
        {
            string gitPath = Path.Combine(folderPath, ".git");
            return Directory.Exists(gitPath) || File.Exists(gitPath);
        }

        private void FindGitRepositoriesRecursively(string folderPath, string outputPath, StringBuilder allChanges, HashSet<string> processedRepos)
        {
            try
            {
                foreach (var subDir in Directory.GetDirectories(folderPath))
                {
                    // Skip excluded folders like .git, node_modules, etc.
                    string dirName = Path.GetFileName(subDir);
                    if (dirName.StartsWith(".") || dirName == "node_modules" || dirName == "bin" || dirName == "obj")
                        continue;

                    if (IsGitRepository(subDir))
                    {
                        ProcessGitRepository(subDir, outputPath, allChanges, processedRepos);
                    }
                    else
                    {
                        // Continue searching in subdirectories
                        FindGitRepositoriesRecursively(subDir, outputPath, allChanges, processedRepos);
                    }
                }
            }
            catch (Exception ex)
            {
                FileHandlerBase._log.Error(ex, $"Error searching for Git repositories in {folderPath}");
            }
        }

        private string GetGitStatus(string repositoryPath)
        {
            return ExecuteGitCommand(repositoryPath, "status");
        }

        private string GetGitDiff(string repositoryPath)
        {
            var unstaged = ExecuteGitCommand(repositoryPath, "diff");
            var staged = ExecuteGitCommand(repositoryPath, "diff --cached");

            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(unstaged))
            {
                sb.AppendLine("### Unstaged Changes");
                sb.AppendLine(unstaged);
            }
            if (!string.IsNullOrWhiteSpace(staged))
            {
                sb.AppendLine("### Staged Changes");
                sb.AppendLine(staged);
            }
            if (sb.Length == 0)
            {
                sb.AppendLine("No diff changes.");
            }
            return sb.ToString();
        }

        private string ExecuteGitCommand(string workingDirectory, string arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        _log.Error(null,"Failed to start Git process");
                        return "Error: Failed to start Git process";
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        _log.Error(null,$"Git error: {error}");
                        return $"Error: {error}";
                    }

                    return output;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Exception executing Git command: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }
}
