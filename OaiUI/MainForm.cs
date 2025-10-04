using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VecTool.Configuration;
using VecTool.Handlers;
using oaiUI; // Assuming for WinFormsUserInterface

namespace Vectool.OaiUI
{
    public partial class MainForm : Form
    {
        private WinFormsUserInterface userInterface;
        private List<string> selectedFolders = new();

        public MainForm()
        {
            InitializeComponent();
            userInterface = new WinFormsUserInterface(statusLabel, progressBar);
            WireUpEvents();
            LoadVectorStoresIntoComboBox();
        }

        private void WireUpEvents()
        {
            runTestsToolStripMenuItem.Click += btnRunTestsClick;
            convertToDocxToolStripMenuItem.Click += btnConvertToDocxClick;
            convertToMdToolStripMenuItem.Click += btnConvertToMDClick;
            convertToPdfToolStripMenuItem.Click += btnConvertToPdfClick;
            getGitChangesToolStripMenuItem.Click += btnGetGitChangesClick;
            fileSizeSummaryToolStripMenuItem.Click += btnFileSizeSummaryClick;
            exitToolStripMenuItem.Click += exitToolStripMenuItemClick;

            comboBoxVectorStores.SelectedIndexChanged += comboBoxVectorStoresSelectedIndexChanged;
            btnCreateNewVectorStore.Click += btnCreateNewVectorStoreClick;
        }

        private void LoadVectorStoresIntoComboBox()
        {
            try
            {
                var allConfigs = VectorStoreConfig.LoadAll();
                var names = allConfigs?.Keys
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>();

                comboBoxVectorStores.Items.Clear();
                comboBoxVectorStores.Items.AddRange(names.Cast<object>().ToArray());

                if (comboBoxVectorStores.Items.Count > 0)
                {
                    if (comboBoxVectorStores.SelectedIndex < 0)
                        comboBoxVectorStores.SelectedIndex = 0;
                }
                else
                {
                    listBoxSelectedFolders.Items.Clear();
                }
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"Failed to load vector stores: {ex.Message}", "Warning", MessageType.Warning);
            }
        }

        private void comboBoxVectorStoresSelectedIndexChanged(object? sender, EventArgs e)
        {
            var selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedVectorStore))
            {
                listBoxSelectedFolders.Items.Clear();
                return;
            }

            LoadFoldersForVectorStore(selectedVectorStore);
        }

        private void LoadFoldersForVectorStore(string vectorStoreName)
        {
            try
            {
                var allConfigs = VectorStoreConfig.LoadAll();
                if (allConfigs != null && allConfigs.TryGetValue(vectorStoreName, out var config))
                {
                    selectedFolders = config.FolderPaths?.ToList() ?? new List<string>();
                    listBoxSelectedFolders.Items.Clear();
                    listBoxSelectedFolders.Items.AddRange(selectedFolders.Cast<object>().ToArray());
                }
                else
                {
                    selectedFolders = new List<string>();
                    listBoxSelectedFolders.Items.Clear();
                }
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"Failed to load folders: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private void btnCreateNewVectorStoreClick(object? sender, EventArgs e)
        {
            var newName = txtNewVectorStoreName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                userInterface.ShowMessage("Please enter a vector store name.", "Input Required", MessageType.Warning);
                return;
            }

            try
            {
                var allConfigs = VectorStoreConfig.LoadAll() ?? new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);

                if (allConfigs.ContainsKey(newName))
                {
                    userInterface.ShowMessage($"Vector store '{newName}' already exists.", "Duplicate Name", MessageType.Warning);
                    return;
                }

                var global = VectorStoreConfig.FromAppConfig();
                var newConfig = new VectorStoreConfig
                {
                    FolderPaths = new List<string>(),
                    ExcludedFiles = global.ExcludedFiles?.ToList() ?? new List<string>(),
                    ExcludedFolders = global.ExcludedFolders?.ToList() ?? new List<string>()
                };

                allConfigs[newName] = newConfig;
                VectorStoreConfig.SaveAll(allConfigs);

                LoadVectorStoresIntoComboBox();
                comboBoxVectorStores.SelectedItem = newName;
                txtNewVectorStoreName.Text = string.Empty;

                userInterface.ShowMessage($"Vector store '{newName}' created successfully.", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"Failed to create vector store: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private async void btnRunTestsClick(object? sender, EventArgs e)
        {
            try
            {
                runTestsToolStripMenuItem.Enabled = false;
                userInterface.WorkStart("Running tests...", selectedFolders);

                string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedVectorStore))
                {
                    userInterface.ShowMessage("Please select a vector store first.", "No Vector Store", MessageType.Warning);
                    return;
                }

                string? solutionPath = FindSolutionFile();
                if (solutionPath == null)
                {
                    userInterface.ShowMessage("Could not find VecTool.sln in the directory tree.", "Solution Not Found", MessageType.Error);
                    return;
                }

                var testRunner = new TestRunnerHandler(userInterface, null); 
                var outputPath = await testRunner.RunTestsAsync(solutionPath, selectedVectorStore, selectedFolders);

                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    userInterface.ShowMessage($"Tests completed successfully. Saved to {outputPath}", "Success", MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"Error running tests: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
                runTestsToolStripMenuItem.Enabled = true;
            }
        }

        private void btnGetGitChangesClick(object? sender, EventArgs e)
        {
            userInterface.ShowMessage("Get Git Changes not implemented yet.", "TODO", MessageType.Information);
        }

        private void btnFileSizeSummaryClick(object? sender, EventArgs e)
        {
            userInterface.ShowMessage("File Size Summary not implemented yet.", "TODO", MessageType.Information);
        }

        private void btnConvertToDocxClick(object? sender, EventArgs e)
        {
            userInterface.ShowMessage("Convert to DOCX not implemented yet.", "TODO", MessageType.Information);
        }

        private void btnConvertToMDClick(object? sender, EventArgs e)
        {
            userInterface.ShowMessage("Convert to Markdown not implemented yet.", "TODO", MessageType.Information);
        }

        private void btnConvertToPdfClick(object? sender, EventArgs e)
        {
            userInterface.ShowMessage("Convert to PDF not implemented yet.", "TODO", MessageType.Information);
        }

        private void exitToolStripMenuItemClick(object? sender, EventArgs e)
        {
            Application.Exit();
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
    }
}
