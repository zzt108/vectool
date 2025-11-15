using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using VecTool.Core.Helpers;
using VecTool.Handlers;
using VecTool.RecentFiles;

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
        private async void convertToMdToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? "default");
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true));
            var defaultFileName = RecentFilesOutputManager.Factory().BuildOutputPath($"{vsName}_{branchName}", RecentFileType.Codebase_Md);

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

                // 🔄 MODIFY - Refresh recent files after MD generation
                recentFilesPanel.RefreshList();
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
        private async void getGitChangesToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? "default");
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true));
            var gitChangesFileName = RecentFilesOutputManager.Factory().BuildOutputPath( $"{vsName}_{branchName}", RecentFileType.Git_Md);
            var mdExportFileName = RecentFilesOutputManager.Factory().BuildOutputPath($"{vsName}_{branchName}", RecentFileType.Codebase_Md);

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Git Changes As...",
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                FileName = gitChangesFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var gitOutputPath = saveFileDialog.FileName;

            // Derive MD output path in same directory
            var mdOutputPath = Path.Combine(Path.GetDirectoryName(gitOutputPath)!, mdExportFileName);

            // Check if MD file exists and confirm overwrite (once - ignored for now)
            if (false && File.Exists(mdOutputPath))
            {
                var overwrite = MessageBox.Show(
                    $"MD export file already exists:\n{mdOutputPath}",
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

            // Refresh Recent Files panel after both operations
            recentFilesPanel.RefreshList();
        }

        /// <summary>
        /// Handler for "File Size Summary" menu item (Ctrl+F).
        /// </summary>
        private async void fileSizeSummaryToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? "default");
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true));
            var defaultFileName = RecentFilesOutputManager.Factory().BuildOutputPath($"{vsName}_{branchName}", RecentFileType.Summary_Md);

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save File Size Summary As...",
                Filter = "Text files (*.md)|*.txt|All files (*.*)|*.*",
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

                // 🔄 MODIFY - Refresh recent files after file size summary
                recentFilesPanel.RefreshList();
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
        private async void runTestsToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            var currentVectorStore = GetCurrentVectorStoreConfig();

            if (currentVectorStore.FolderPaths.Count == 0)
            {
                userInterface.ShowMessage("Please select one or more folders first.", "No Folders Selected", MessageType.Warning);
                return;
            }

            var solutionPaths = Configuration.FindSolutionFiles(currentVectorStore);

            if (solutionPaths.Length == 0)
            {
                MessageBox.Show("Could not find the solution file.", "Solution Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // If multiple solutions found, let user choose
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

                // Pre-populate with first found solution
                openFileDialog.FileName = Path.GetFileName(solutionPaths[0]);

                if (openFileDialog.ShowDialog() != DialogResult.OK)
                    return;

                solutionPath = openFileDialog.FileName;
            }
            else
            {
                solutionPath = solutionPaths[0];
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? "default");
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true));
            var testResultsFileName = RecentFilesOutputManager.Factory().BuildOutputPath($"{vsName}_{branchName}", RecentFileType.TestResults_Md);


            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Git Changes As...",
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                FileName = testResultsFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var testResultsOutputPath = saveFileDialog.FileName;

            // Create the process runner and handler (kept local for MVP; DI-ready).
            var processRunner = new VecTool.Core.ProcessRunner();
            var handler = new VecTool.Handlers.TestRunnerHandler(
                solutionPath,
                testResultsOutputPath,
                processRunner,
                userInterface,
                recentFilesManager,
                branchName,
                vsName);

            try
            {
                // Optional: existing UI busy indicator hooks if available.
                userInterface.WorkStart("Running unit tests...", selectedFolders);

                // 🔄 MODIFY - Pass computed branch name
                var outputFile = await handler.RunTestsAsync(System.Threading.CancellationToken.None).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test execution failed: {ex.Message}", "Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }

            recentFilesPanel.RefreshList();
        }

        /// <summary>
        /// Handler for "Exit" menu item (Alt+F4).
        /// </summary>
        private void exitToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }
        /// <summary>Handler for Export to Repomix menu item (Ctrl+R).</summary>
        private async void exportToRepomixToolStripMenuItemClick(object? sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                userInterface.ShowMessage(
                    "Please select one or more folders first.",
                    "No Folders Selected",
                    MessageType.Warning);
                return;
            }

            var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? "default");
            var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true));

            var defaultFileName = RecentFilesOutputManager.Factory().BuildOutputPath(
                $"{vsName}_{branchName}",
                RecentFileType.Repomix_Xml);

            // ✅ Repomix expects to run in the target directory, so use first selected folder as target
            var targetDirectory = selectedFolders.First();

            using var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Repomix Export As...",
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                FileName = defaultFileName
            };

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            var outputPath = saveFileDialog.FileName;
            var config = GetCurrentVectorStoreConfig();

            try
            {
                var handler = new RepomixHandler(userInterface, recentFilesManager);
                var result = await handler.RunRepomixAsync(
                    targetDirectory,
                    outputPath,
                    config,
                    CancellationToken.None).ConfigureAwait(true);

                if (result != null)
                {
                    userInterface.ShowMessage(
                        $"Successfully generated Repomix export at:\n{result}",
                        "Success",
                        MessageType.Information);

                    // ✅ Refresh recent files panel
                    recentFilesPanel.RefreshList();
                }
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage(
                    $"An error occurred: {ex.Message}",
                    "Error",
                    MessageType.Error);
            }
        }

    }
}
