using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using NLogS = NLogShared;

namespace DocXHandler
{
    public class GitChangesHandler : FileHandlerBase
    {
        private static NLogS.CtxLogger log = new();

        public string GetGitChanges(List<string> folderPaths, string outputPath)
        {

            // Get the AI prompt from app.config
            string aiPrompt = ConfigurationManager.AppSettings["gitAiPrompt"] ??
                "Analyze the following Git changes and provide a concise, descriptive commit message.";

            StringBuilder allChanges = new StringBuilder();

            // Add the AI prompt at the beginning of the document
            allChanges.AppendLine("# AI Prompt for Commit Message");
            allChanges.AppendLine();
            allChanges.AppendLine(aiPrompt);
            allChanges.AppendLine();
            allChanges.AppendLine("---");
            allChanges.AppendLine();

            foreach (var folderPath in folderPaths)
            {
                if (IsGitRepository(folderPath))
                {
                    allChanges.AppendLine($"# Git Changes for: {folderPath}");
                    allChanges.AppendLine();

                    // Get status changes
                    string statusChanges = GetGitStatus(folderPath);
                    allChanges.AppendLine("## Status Changes");
                    allChanges.AppendLine("```");
                    allChanges.AppendLine(statusChanges);
                    allChanges.AppendLine("```");
                    allChanges.AppendLine();

                    // Get diff changes
                    string diffChanges = GetGitDiff(folderPath);
                    allChanges.AppendLine("## Diff Changes");
                    allChanges.AppendLine("```");
                    allChanges.AppendLine(diffChanges);
                    allChanges.AppendLine("```");
                    allChanges.AppendLine();
                }
                else
                {
                    // Check subdirectories for Git repositories
                    FindGitRepositoriesRecursively(folderPath, allChanges);
                }
            }

            // Save all changes to the output file
            File.WriteAllText(outputPath, allChanges.ToString());

            return allChanges.ToString();
        }

        private bool IsGitRepository(string folderPath)
        {
            return Directory.Exists(Path.Combine(folderPath, ".git"));
        }

        private void FindGitRepositoriesRecursively(string folderPath, StringBuilder allChanges)
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
                        allChanges.AppendLine($"# Git Changes for: {subDir}");
                        allChanges.AppendLine();

                        // Get status changes
                        string statusChanges = GetGitStatus(subDir);
                        allChanges.AppendLine("## Status Changes");
                        allChanges.AppendLine("```");
                        allChanges.AppendLine(statusChanges);
                        allChanges.AppendLine("```");
                        allChanges.AppendLine();

                        // Get diff changes
                        string diffChanges = GetGitDiff(subDir);
                        allChanges.AppendLine("## Diff Changes");
                        allChanges.AppendLine("```");
                        allChanges.AppendLine(diffChanges);
                        allChanges.AppendLine("```");
                        allChanges.AppendLine();
                    }
                    else
                    {
                        // Continue searching in subdirectories
                        FindGitRepositoriesRecursively(subDir, allChanges);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error searching for Git repositories in {folderPath}");
            }
        }

        private string GetGitStatus(string repositoryPath)
        {
            return ExecuteGitCommand(repositoryPath, "status");
        }

        private string GetGitDiff(string repositoryPath)
        {
            return ExecuteGitCommand(repositoryPath, "diff");
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
                        log.Error(null,"Failed to start Git process");
                        return "Error: Failed to start Git process";
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        log.Error(null,$"Git error: {error}");
                        return $"Error: {error}";
                    }

                    return output;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Exception executing Git command: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }
}
