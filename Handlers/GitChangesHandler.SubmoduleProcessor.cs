using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VecTool.Core;

namespace VecTool.Handlers
{
    /// <summary>
    /// Responsibility: Handles processing of Git submodules within a repository.
    /// </summary>
    public partial class GitChangesHandler
    {
        private record SubmoduleInfo(string Commit, string Path, string Status);

        /// <summary>
        /// Parses the output of "git submodule status" into a structured list.
        /// </summary>
        private async Task<IEnumerable<SubmoduleInfo>> GetSubmodulesAsync(string repositoryPath)
        {
            var gitRunner = new GitRunner(repositoryPath);
            var output = await gitRunner.GetSubmodulesAsync();

            if (string.IsNullOrWhiteSpace(output))
            {
                return Enumerable.Empty<SubmoduleInfo>();
            }

            var submodules = new List<SubmoduleInfo>();
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Trim().Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    // Format can be: "-commit path (branch)" or "+commit path (branch)"
                    // We only need the commit and path.
                    submodules.Add(new SubmoduleInfo(parts[0], parts[1], parts.Length > 2 ? parts[2] : string.Empty));
                }
            }
            return submodules;
        }

        /// <summary>
        /// Iterates through submodules, checks for changes, and generates separate reports for them.
        /// </summary>
        private async Task ProcessSubmodulesAsync(string repoPath, string outputPath, StringBuilder mainChanges)
        {
            var submodules = await GetSubmodulesAsync(repoPath);
            if (!submodules.Any())
            {
                return;
            }

            mainChanges.AppendLine("### Submodules");
            mainChanges.AppendLine();

            foreach (var submodule in submodules)
            {
                var fullSubmodulePath = Path.Combine(repoPath, submodule.Path);
                mainChanges.AppendLine($"- Submodule: `{submodule.Path}`");

                if (!Directory.Exists(fullSubmodulePath) || !GitRunner.IsGitRepository(fullSubmodulePath))
                {
                    mainChanges.AppendLine("  - `Skipped`: Submodule is not initialized. Run `git submodule update --init --recursive`.");
                    continue;
                }

                var submoduleRunner = new GitRunner(fullSubmodulePath);
                var submoduleStatus = await submoduleRunner.GetStatusAsync();

                if (string.IsNullOrWhiteSpace(submoduleStatus) || submoduleStatus.Contains("nothing to commit"))
                {
                    mainChanges.AppendLine("  - `No changes detected`.");
                    continue;
                }

                // Generate a unique filename for the submodule report
                var submoduleFileName = $"{Path.GetFileNameWithoutExtension(outputPath)}-{Path.GetFileName(repoPath)}-{submodule.Path.Replace(Path.DirectorySeparatorChar, '_')}-git-changes.md";
                var submoduleFilePath = Path.Combine(Path.GetDirectoryName(outputPath)!, submoduleFileName);

                var submoduleChanges = new StringBuilder();
                submoduleChanges.AppendLine($"# Git Changes for Submodule: `{submodule.Path}` (in `{repoPath}`)");
                submoduleChanges.AppendLine();
                submoduleChanges.AppendLine("## AI Prompt for Commit Message");
                submoduleChanges.AppendLine(aiPrompt);
                submoduleChanges.AppendLine("---");

                submoduleChanges.AppendLine("## Status Changes");
                submoduleChanges.AppendLine("```");
                submoduleChanges.AppendLine(submoduleStatus);
                submoduleChanges.AppendLine("```");
                submoduleChanges.AppendLine();

                var submoduleDiff = await submoduleRunner.GetDiffAsync();
                submoduleChanges.AppendLine("## Diff Changes");
                submoduleChanges.AppendLine("```");
                submoduleChanges.AppendLine(submoduleDiff);
                submoduleChanges.AppendLine("```");
                submoduleChanges.AppendLine();

                await File.WriteAllTextAsync(submoduleFilePath, submoduleChanges.ToString());
                mainChanges.AppendLine($"  - `Changes detected`. See detailed report: `{submoduleFileName}`");
            }
            mainChanges.AppendLine();
        }
    }
}

