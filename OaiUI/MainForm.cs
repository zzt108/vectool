using DocumentFormat.OpenXml.Drawing.Charts;
using oaiUI;
using oaiUI.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VecTool.Configuration;
using VecTool.Handlers;
using VecTool.RecentFiles;

namespace Vectool.OaiUI
{
    public partial class MainForm : Form
    {
        private readonly IUserInterface _userInterface;
        private readonly IRecentFilesManager _recentFilesManager;
        private List<string> _selectedFolders = new List<string>();

        // This is a placeholder for the status label from your designer
        private ToolStripStatusLabel statusLabel = new ToolStripStatusLabel();
        // This is a placeholder for the progress bar from your designer
        private ToolStripProgressBar progressBar = new ToolStripProgressBar();

        public MainForm()
        {
            InitializeComponent();

            // Initialize UI and managers
            _userInterface = new WinFormsUserInterface(statusLabel, progressBar);

            var recentFilesConfig = RecentFilesConfig.FromAppConfig();
            var recentFilesStore = new FileRecentFilesStore(recentFilesConfig);
            _recentFilesManager = new RecentFilesManager(recentFilesConfig, recentFilesStore);

            string vectorStoreFoldersPath = System.Configuration.ConfigurationManager.AppSettings["vectorStoreFoldersPath"] ?? "vectorStoreFolders.json";

            // Wire up UI events
            WireUpEvents();

            // Load initial data
            LoadSettingsTab();
        }

        private void WireUpEvents()
        {
            // Main Actions
            runTestsToolStripMenuItem.Click += btnRunTests_Click;
            getGitChangesToolStripMenuItem.Click += btnGetGitChangesClick;
            fileSizeSummaryToolStripMenuItem.Click += btnFileSizeSummaryClick;
            convertToDocxToolStripMenuItem.Click += btnConvertToDocxClick;
            convertToMdToolStripMenuItem.Click += btnConvertToMDClick;
            convertToPdfToolStripMenuItem.Click += btnConvertToPdfClick;
            exitToolStripMenuItem.Click += exitToolStripMenuItemClick;

            // Settings Tab
            btnSaveVsSettings.Click += btnSaveVsSengsClick;
            cmbSettingsVectorStore.SelectedIndexChanged += cmbSettingsVectorStore_SelectedIndexChanged;
            chkInheritExcludedFiles.CheckedChanged += chkInheritExcludedFiles_CheckedChanged;
            chkInheritExcludedFolders.CheckedChanged += chkInheritExcludedFolders_CheckedChanged;

            // Recent Files Panel
            // Assuming you have a control named 'recentFilesPanel' of type RecentFilesPanel
            // recentFilesPanel.Manager = _recentFilesManager;
            // recentFilesPanel.ItemClicked += (s, e) => OpenFile(e.FilePath);
        }

        private void OpenFile(string filePath)
        {
            try
            {
                // Implement your file opening logic here, e.g., Process.Start
                _userInterface.ShowMessage($"Opening {filePath}...", "Info", MessageType.Information);
            }
            catch (Exception ex)
            {
                _userInterface.ShowMessage($"Error opening file: {ex.Message}", "Error", MessageType.Error);
            }
        }

        #region Settings Tab Logic

        private void LoadSettingsTab()
        {
            try
            {
                var allConfigs = VectorStoreConfig.LoadAll();
                var names = allConfigs?.Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>();

                cmbSettingsVectorStore.Items.Clear();
                cmbSettingsVectorStore.Items.AddRange(names.Cast<object>().ToArray());

                if (cmbSettingsVectorStore.Items.Count > 0)
                {
                    cmbSettingsVectorStore.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                // Defensive: ignore failures to not block UI
                _userInterface.ShowMessage($"Failed to load settings: {ex.Message}", "Warning", MessageType.Warning);
            }
        }

        private void cmbSettingsVectorStore_SelectedIndexChanged(object? sender, EventArgs e)
        {
            LoadSettingsSelection(cmbSettingsVectorStore.Text?.Trim());
        }

        private void LoadSettingsSelection(string? name)
        {
            var global = VectorStoreConfig.FromAppConfig();
            var all = VectorStoreConfig.LoadAll();

            if (string.IsNullOrWhiteSpace(name))
            {
                txtExcludedFiles.Text = string.Empty;
                txtExcludedFolders.Text = string.Empty;
                chkInheritExcludedFiles.Checked = true;
                chkInheritExcludedFolders.Checked = true;
                txtExcludedFiles.Enabled = false;
                txtExcludedFolders.Enabled = false;
                return;
            }

            var per = all?.GetValueOrDefault(name);
            var vm = PerVectorStoreSettings.From(name!, global, per);

            chkInheritExcludedFiles.Checked = !vm.UseCustomExcludedFiles;
            chkInheritExcludedFolders.Checked = !vm.UseCustomExcludedFolders;
            txtExcludedFiles.Text = string.Join(Environment.NewLine, vm.CustomExcludedFiles);
            txtExcludedFolders.Text = string.Join(Environment.NewLine, vm.CustomExcludedFolders);
            txtExcludedFiles.Enabled = vm.UseCustomExcludedFiles;
            txtExcludedFolders.Enabled = vm.UseCustomExcludedFolders;
        }

        private void btnSaveVsSengsClick(object? sender, EventArgs e)
        {
            var name = cmbSettingsVectorStore.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                _userInterface.ShowMessage("Please enter or select a vector store name.", "Settings", MessageType.Warning);
                return;
            }

            var global = VectorStoreConfig.FromAppConfig();
            var all = VectorStoreConfig.LoadAll();
            var files = txtExcludedFiles.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var folders = txtExcludedFolders.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var vm = new PerVectorStoreSettings(
                name,
                useCustomExcludedFiles: !chkInheritExcludedFiles.Checked,
                useCustomExcludedFolders: !chkInheritExcludedFolders.Checked,
                customExcludedFiles: files,
                customExcludedFolders: folders);

            PerVectorStoreSettings.Save(all ?? new Dictionary<string, VectorStoreConfig>(), vm, global!);
            VectorStoreConfig.SaveAll(all);  // CHANGED from SaveDictionary

            _userInterface.ShowMessage($"Settings for '{name}' saved successfully.", "Success", MessageType.Information);  // CHANGED Info to Information
        }

        #endregion

        #region Action Event Handlers

        private async void btnRunTests_Click(object? sender, EventArgs e)
        {
            try
            {
                runTestsToolStripMenuItem.Enabled = false;
                _userInterface.WorkStart("Running tests...", _selectedFolders);

                string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedVectorStore))
                {
                    _userInterface.ShowMessage("Please select a vector store first.", "No Vector Store", MessageType.Warning);
                    return;
                }

                string? solutionPath = FindSolutionFile();
                if (solutionPath == null)
                {
                    _userInterface.ShowMessage("Could not find VecTool.sln in the directory tree.", "Solution Not Found", MessageType.Error);
                    return;
                }

                var testRunner = new TestRunnerHandler(_userInterface, _recentFilesManager);
                var outputPath = await testRunner.RunTestsAsync(solutionPath, selectedVectorStore, _selectedFolders);

                if (!string.IsNullOrWhiteSpace(outputPath))
                {
                    _userInterface.ShowMessage($"Tests completed successfully. Saved to {outputPath}", "Success", MessageType.Information);
                }
            }
            catch (Exception ex)
            {
                _userInterface.ShowMessage($"Error running tests: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                _userInterface.WorkFinish();
                runTestsToolStripMenuItem.Enabled = true;
            }
        }

        private void btnGetGitChangesClick(object? sender, EventArgs e)
        {
            // TODO: Implement Git changes feature logic here
            _userInterface.ShowMessage("Get Git Changes not implemented yet.", "TODO", MessageType.Information);
        }

        private void btnFileSizeSummaryClick(object? sender, EventArgs e)
        {
            // TODO: Implement File Size Summary feature logic here
            _userInterface.ShowMessage("File Size Summary not implemented yet.", "TODO", MessageType.Information);
        }

        private void btnConvertToDocxClick(object? sender, EventArgs e)
        {
            // TODO: Implement Convert to DOCX feature logic here
            _userInterface.ShowMessage("Convert to DOCX not implemented yet.", "TODO", MessageType.Information);
        }

        private void btnConvertToMDClick(object? sender, EventArgs e)
        {
            // TODO: Implement Convert to Markdown feature logic here
            _userInterface.ShowMessage("Convert to Markdown not implemented yet.", "TODO", MessageType.Information);
        }

        private void btnConvertToPdfClick(object? sender, EventArgs e)
        {
            // TODO: Implement Convert to PDF feature logic here
            _userInterface.ShowMessage("Convert to PDF not implemented yet.", "TODO", MessageType.Information);
        }

        private void exitToolStripMenuItemClick(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Traverses parent directories to locate the solution file (VecTool.sln).
        /// </summary>
        private string? FindSolutionFile()
        {
            var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (currentDir != null)
            {
                var solutionFile = Path.Combine(currentDir.FullName, "VecTool.sln");
                if (File.Exists(solutionFile))
                {
                    return solutionFile;
                }
                currentDir = currentDir.Parent;
            }
            return null;
        }

        #endregion
    }
}
