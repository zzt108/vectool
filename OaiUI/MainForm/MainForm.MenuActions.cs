// File: OaiUI/MainForm.MenuActions.cs

using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using VecTool.Core;
using VecTool.Handlers;

namespace Vectool.OaiUI
{
    /// <summary>
    /// MainForm partial: Menu action handlers (Convert to MD, Git Changes, File Size Summary, Run Tests, Exit).
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// Handler for "Convert to MD" menu item (Ctrl+M).
        /// </summary>
        private async void convertToMdToolStripMenuItemClick(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? default);
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true));
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
                userInterface.ShowMessage($"Successfully generated file at:\n{outputPath}", "Success", MessageType.Information);
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

        /// <summary>
        /// Handler for "Get Git Changes" menu item (Ctrl+G).
        /// </summary>
        private async void getGitChangesToolStripMenuItemClick(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? default);
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true));
            var gitChangesFileName = $"{vsName}.{branchName}.GIT.md";
            var mdExportFileName = $"{vsName}.{branchName}.md";

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Git Changes As...",
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                FileName = gitChangesFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var gitOutputPath = saveFileDialog.FileName;
            // ✅ Derive MD output path in same directory
            var mdOutputPath = Path.Combine(Path.GetDirectoryName(gitOutputPath)!, mdExportFileName);

            // ✅ Check if MD file exists and confirm overwrite once (ignored for now)
            if (false && File.Exists(mdOutputPath))
            {
                var overwrite = MessageBox.Show(
                    $"MD export file already exists:\n{mdOutputPath}?",
                    "Confirm Overwrite",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (overwrite != DialogResult.Yes)
                    return;
            }

            try
            {
                await ExecuteGitChangesAndMdParallelAsync(gitOutputPath, mdOutputPath).ConfigureAwait(true);
                userInterface.ShowMessage(
                    $"Successfully generated:\n- Git Changes: {gitOutputPath}\n- MD Export: {mdOutputPath}",
                    "Success",
                    MessageType.Information);
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"An error occurred: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }

            // ✅ Refresh Recent Files panel after both operations
            recentFilesPanel.RefreshList();
        }

        /// <summary>
        /// Handler for "File Size Summary" menu item (Ctrl+F).
        /// </summary>
        private async void fileSizeSummaryToolStripMenuItemClick(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? default);
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true));
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
                userInterface.ShowMessage($"Successfully generated file at:\n{outputPath}", "Success", MessageType.Information);
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

        /// <summary>
        /// Handler for "Run Tests" menu item (Ctrl+T).
        /// </summary>
        private async void runTestsToolStripMenuItemClick(object? sender, EventArgs e)
        {
            var currentVectorStore = GetCurrentVectorStoreConfig();
            if (currentVectorStore.FolderPaths.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var solutionPaths = Utilities.FindSolutionFiles(currentVectorStore);
            if (solutionPaths.Length == 0)
            {
                MessageBox.Show("Could not find the solution file.", "Solution Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // ✅ If multiple solutions found, let user choose
            string solutionPath;
            if (solutionPaths.Length > 1)
            {
                using var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Solution File",
                    Filter = "Solution files (*.sln)|*.sln|All files (*.*)|*.*",
                    InitialDirectory = Path.GetDirectoryName(solutionPaths[0]),
                    Multiselect = false
                };
                // ✅ Pre-populate with first found solution
                openFileDialog.FileName = Path.GetFileName(solutionPaths[0]);

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                    return;

                solutionPath = openFileDialog.FileName;
            }
            else
            {
                solutionPath = solutionPaths[0];
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? default);
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true));
            var testResultsFileName = $"{vsName}.{branchName}.TestResults.md";

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Git Changes As...",
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                FileName = testResultsFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var testResultsOutputPath = saveFileDialog.FileName;

            // ✅ Create the process runner and handler (kept local for MVP; DI-ready).
            var processRunner = new VecTool.Core.ProcessRunner();
            var handler = new VecTool.Handlers.TestRunnerHandler(
                solutionPath,
                testResultsOutputPath,
                processRunner,
                userInterface,
                recentFilesManager);

            try
            {
                // ✅ Optional: existing UI busy indicator hooks if available.
                userInterface.WorkStart("Running unit tests...", selectedFolders);

                // ✅ MODIFY - Pass computed branch name
                var message = await handler.RunTestsAsync(solutionPath, branchName, CancellationToken.None).ConfigureAwait(true);

                var isSuccess = message?.StartsWith("All tests passed.", StringComparison.OrdinalIgnoreCase);
                if (isSuccess.HasValue && isSuccess.Value)
                {
                    MessageBox.Show(message, "Test Results", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(message, "Test Results", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test execution failed: {ex.Message}", "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                userInterface.WorkFinish();
                recentFilesPanel.RefreshList();
            }
        }

        /// <summary>
        /// Handler for "Exit" menu item (Alt+F4).
        /// </summary>
        private void exitToolStripMenuItemClick(object? sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
