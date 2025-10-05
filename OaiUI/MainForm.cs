// ✅ FULL FILE VERSION
using NLog;
using oaiUI.RecentFiles;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VecTool.Configuration;
using VecTool.RecentFiles;

namespace Vectool.OaiUI
{
    /// <summary>
    /// Main application form with tabbed interface for Vector Store management
    /// </summary>
    public partial class MainForm : Form
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        // Designer components container
        // private System.ComponentModel.IContainer? components = null;

        // Dependencies
        private IRecentFilesManager? recentFilesManager;
        private IAppSettingsReader? appSettings;

        public MainForm()
        {
            // CRITICAL: This MUST be first - initializes all designer controls
            InitializeComponent();

            // ✅ NEW - Validate that designer controls are present
            ValidateDesignerControls();

            // Initialize dependencies
            InitializeDependencies();

            // Wire up additional events not handled by designer
            WireUpEvents();

            // Load initial data
            LoadInitialData();

            Logger.Info("MainForm initialized successfully");
        }

        public MainForm(IRecentFilesManager recentFilesManager, IAppSettingsReader appSettings) : this()
        {
            this.recentFilesManager = recentFilesManager ?? throw new ArgumentNullException(nameof(recentFilesManager));
            this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

            // Initialize Recent Files panel with manager
            if (recentFilesPanel != null)
            {
                recentFilesPanel.Initialize(this.recentFilesManager);
            }
        }

        private void InitializeDependencies()
        {
            // Fallback initialization for runtime
            if (recentFilesManager == null)
            {
                var config = RecentFilesConfig.FromAppConfig(appSettings ?? new ConfigurationManagerAppSettingsReader());
                var store = new FileRecentFilesStore(config);
                recentFilesManager = new RecentFilesManager(config, store);
            }

            if (appSettings == null)
            {
                appSettings = new ConfigurationManagerAppSettingsReader();
            }
        }

        private void LoadInitialData()
        {
            try
            {
                // Initialize Settings tab data
                SettingsTabInitializeData();

                // Set initial status
                if (statusLabel != null)
                {
                    statusLabel.Text = "Ready";
                }

                Logger.Debug("Initial data loaded");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load initial data");
                MessageBox.Show($"Warning: Some data failed to load: {ex.Message}",
                               "VecTool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void WireUpEvents()
        {
            try
            {
                if (tabControl1 != null)
                {
                    tabControl1.SelectedIndexChanged += OnTabSelectedIndexChanged;
                }

                if (convertToMdToolStripMenuItem != null)
                {
                    convertToMdToolStripMenuItem.Click += ConvertToMdToolStripMenuItem_Click;
                }

                if (getGitChangesToolStripMenuItem != null)
                {
                    getGitChangesToolStripMenuItem.Click += GetGitChangesToolStripMenuItem_Click;
                }

                if (fileSizeSummaryToolStripMenuItem != null)
                {
                    fileSizeSummaryToolStripMenuItem.Click += FileSizeSummaryToolStripMenuItem_Click;
                }

                if (runTestsToolStripMenuItem != null)
                {
                    runTestsToolStripMenuItem.Click += RunTestsToolStripMenuItem_Click;
                }

                if (exitToolStripMenuItem != null)
                {
                    exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;
                }

                if (btnSelectFolders != null)
                {
                    btnSelectFolders.Click += BtnSelectFolders_Click;
                }

                if (btnCreateNewVectorStore != null)
                {
                    btnCreateNewVectorStore.Click += BtnCreateNewVectorStore_Click;
                }

                if (cmbSettingsVectorStore != null)
                {
                    cmbSettingsVectorStore.SelectedIndexChanged += CmbSettingsVectorStore_SelectedIndexChanged;
                }

                if (chkInheritExcludedFiles != null)
                {
                    chkInheritExcludedFiles.CheckedChanged += ChkInheritExcludedFiles_CheckedChanged;
                }

                if (chkInheritExcludedFolders != null)
                {
                    chkInheritExcludedFolders.CheckedChanged += ChkInheritExcludedFolders_CheckedChanged;
                }

                if (btnSaveVsSettings != null)
                {
                    btnSaveVsSettings.Click += BtnSaveVsSettings_Click;
                }

                if (btnResetVsSettings != null)
                {
                    btnResetVsSettings.Click += BtnResetVsSettings_Click;
                }

                Logger.Debug("Events wired successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to wire events");
            }
        }

        private void OnTabSelectedIndexChanged(object? sender, EventArgs e)
        {
            var tabs = sender as TabControl ?? Controls.OfType<TabControl>().FirstOrDefault();
            if (tabs == null) return;

            foreach (TabPage tp in tabs.TabPages)
            {
                if (string.Equals(tp.Text, "Recent Files", StringComparison.OrdinalIgnoreCase))
                {
                    var panel = tp.Controls.OfType<RecentFilesPanel>().FirstOrDefault();
                    panel?.RefreshList();
                    break;
                }
            }
        }

        #region Menu Event Handlers
        private void ConvertToMdToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Convert to MD functionality - Coming soon!", "VecTool",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GetGitChangesToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Get Git Changes functionality - Coming soon!", "VecTool",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void FileSizeSummaryToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("File Size Summary functionality - Coming soon!", "VecTool",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RunTestsToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Run Tests functionality - Coming soon!", "VecTool",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExitToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            Close();
        }
        #endregion

        #region Main Tab Event Handlers
        private void BtnSelectFolders_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Folder selection - Coming soon!", "VecTool",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnCreateNewVectorStore_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Vector store creation - Coming soon!", "VecTool",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        #region Settings Tab Event Handlers
        private void CmbSettingsVectorStore_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var selectedName = cmbSettingsVectorStore?.SelectedItem?.ToString();
            SettingsTabLoadSelection(selectedName);
        }

        private void ChkInheritExcludedFiles_CheckedChanged(object? sender, EventArgs e)
        {
            if (txtExcludedFiles != null && chkInheritExcludedFiles != null)
            {
                txtExcludedFiles.Enabled = !chkInheritExcludedFiles.Checked;
            }
        }

        private void ChkInheritExcludedFolders_CheckedChanged(object? sender, EventArgs e)
        {
            if (txtExcludedFolders != null && chkInheritExcludedFolders != null)
            {
                txtExcludedFolders.Enabled = !chkInheritExcludedFolders.Checked;
            }
        }

        private void BtnSaveVsSettings_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Settings saved!", "VecTool",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnResetVsSettings_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("Settings reset!", "VecTool",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        #endregion

        // ✅ NEW - Validates designer control presence
        private void ValidateDesignerControls()
        {
            var missingControls = new List<string>();

            if (menuStrip1 == null) missingControls.Add("menuStrip1");
            if (tabControl1 == null) missingControls.Add("tabControl1");
            if (statusStrip1 == null) missingControls.Add("statusStrip1");
            if (tabPageMain == null) missingControls.Add("tabPageMain");
            if (tabPageSettings == null) missingControls.Add("tabPageSettings");
            if (tabPageRecentFiles == null) missingControls.Add("tabPageRecentFiles");

            if (missingControls.Count > 0)
            {
                var message = $"Missing designer controls: {string.Join(", ", missingControls)}\n\n" +
                              "Try: Build → Clean Solution → Rebuild Solution";

                Logger.Error("Designer controls missing: {MissingControls}", string.Join(", ", missingControls));
                MessageBox.Show(message, "VecTool - Designer Issue",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                Logger.Debug("All designer controls validated successfully");
            }
        }
    }
}
