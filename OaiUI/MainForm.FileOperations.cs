// ✅ FULL FILE VERSION
// Path: src/VecTool.UI/OaiUI/MainForm.FileOperations.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VecTool.Configuration;
using VecTool.Core;
using VecTool.Handlers;

namespace Vectool.UI
{
    public partial class MainForm : Form
    {
        // Menu: Get Git Changes
        private async void getGitChangesToolStripMenuItemClick(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? default, "_");
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true), "_");
            var defaultFileName = $"{vsName}.{branchName}.changes.md";

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Git Changes As...",
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                FileName = defaultFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var outputPath = saveFileDialog.FileName;

            try
            {
                userInterface.WorkStart("Generating Git changes file...", selectedFolders);
                var handler = new GitChangesHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.GetGitChanges(selectedFolders, outputPath)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated file at {outputPath}", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"An error occurred: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        // Menu: Convert to MD
        private async void convertToMdToolStripMenuItemClick(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? default, "_");
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true), "_");
            var defaultFileName = $"{vsName}.{branchName}.md";

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save as Markdown...",
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                FileName = defaultFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var outputPath = saveFileDialog.FileName;
            var config = GetCurrentVectorStoreConfig();

            try
            {
                userInterface.WorkStart("Generating MD file...", selectedFolders);
                var handler = new MDHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.ExportSelectedFolders(selectedFolders, outputPath, config)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated file at {outputPath}", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"An error occurred: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        // Menu: File Size Summary
        private async void fileSizeSummaryToolStripMenuItemClick(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? default, "_");
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true), "_");
            var defaultFileName = $"{vsName}.{branchName}.summary.txt";

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save File Size Summary As...",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = defaultFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var outputPath = saveFileDialog.FileName;
            var config = GetCurrentVectorStoreConfig();

            try
            {
                userInterface.WorkStart("Generating file size summary...", selectedFolders);
                var handler = new FileSizeSummaryHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.GenerateFileSizeSummary(selectedFolders, outputPath, config)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated file at {outputPath}", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"An error occurred: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        // Menu: Run Tests
        private async void runTestsToolStripMenuItemClick(object? sender, EventArgs e)
        {
            var solutionPath = FindSolutionFile();
            if (solutionPath is null)
            {
                userInterface.ShowMessage("Could not find VecTool.sln in parent directories.", "Solution Not Found", MessageType.Error);
                return;
            }

            var vsName = comboBoxVectorStores.SelectedItem?.ToString() ?? default;
            var handler = new TestRunnerHandler(userInterface, recentFilesManager);

            try
            {
                userInterface.WorkStart("Running unit tests...", selectedFolders);
                await handler.RunTestsAsync(solutionPath, vsName, selectedFolders).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"Test execution failed: {ex.Message}", "Test Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        // Helpers
        private async Task<string> GetCurrentBranchNameAsync()
        {
            try
            {
                var preferredWorkingDir = ResolvePreferredWorkingDirectory(selectedFolders);
                if (!string.IsNullOrWhiteSpace(preferredWorkingDir))
                {
                    var git = new GitRunner(preferredWorkingDir);
                    var branch = await git.GetCurrentBranchAsync().ConfigureAwait(false);
                    return string.IsNullOrWhiteSpace(branch) ? "unknown" : branch;
                }

                var solutionPath = FindSolutionFile();
                var solutionDir = solutionPath is null ? AppDomain.CurrentDomain.BaseDirectory : Path.GetDirectoryName(solutionPath)!;
                var gitFallback = new GitRunner(solutionDir);
                var fallbackBranch = await gitFallback.GetCurrentBranchAsync().ConfigureAwait(false);
                return string.IsNullOrWhiteSpace(fallbackBranch) ? "unknown" : fallbackBranch;
            }
            catch
            {
                return "unknown";
            }
        }

        private static string? ResolvePreferredWorkingDirectory(IReadOnlyList<string> folders)
        {
            if (folders == null || folders.Count == 0)
                return null;

            string? firstExisting = null;
            foreach (var folder in folders)
            {
                if (string.IsNullOrWhiteSpace(folder))
                    continue;

                if (firstExisting is null && Directory.Exists(folder))
                    firstExisting = folder;

                var root = FindRepoRoot(folder);
                if (!string.IsNullOrWhiteSpace(root))
                    return root;
            }

            return firstExisting;
        }

        private static string? FindRepoRoot(string? startPath)
        {
            if (string.IsNullOrWhiteSpace(startPath))
                return null;

            var dir = new DirectoryInfo(startPath);
            while (dir != null)
            {
                var gitDir = Path.Combine(dir.FullName, ".git");
                if (Directory.Exists(gitDir) || File.Exists(gitDir))
                    return dir.FullName;

                dir = dir.Parent;
            }

            return null;
        }

        private string? FindSolutionFile()
        {
            var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (currentDir != null)
            {
                var solutionFile = Path.Combine(currentDir.FullName, "VecTool.sln");
                if (File.Exists(solutionFile))
                    return solutionFile;

                currentDir = currentDir.Parent;
            }

            return null;
        }

        private static string SanitizeFileName(string input, string replacement)
        {
            var replChar = string.IsNullOrEmpty(replacement) ? replacement[0] : replacement[0];

            if (string.IsNullOrEmpty(input))
                return default;

            var sanitized = input;
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var ch in invalidChars)
                sanitized = sanitized.Replace(ch, replChar);

            foreach (var ch in new[] { ' ', '/', '\\', ':', '*', '?', '"', '<', '>', '|' })
                sanitized = sanitized.Replace(ch, replChar);

            var doubleRepl = new string(replChar, 2);
            var singleRepl = new string(replChar, 1);
            while (sanitized.Contains(doubleRepl, StringComparison.Ordinal))
                sanitized = sanitized.Replace(doubleRepl, singleRepl, StringComparison.Ordinal);

            sanitized = sanitized.Trim(replChar, '.', ' ');
            return string.IsNullOrWhiteSpace(sanitized) ? default : sanitized;
        }
    }
}
