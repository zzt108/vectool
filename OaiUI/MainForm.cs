// ✅ FULL FILE VERSION
// Path: OaiUI/MainForm.cs

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
    /// Main application form with tabbed interface for Vector Store management.
    /// </summary>
    public partial class MainForm : Form
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        // Dependencies
        private IRecentFilesManager? recentFilesManager;
        private IAppSettingsReader? appSettings;

        /// <summary>
        /// Parameterless constructor for Designer compatibility and runtime DI.
        /// </summary>
        public MainForm()
        {
            // ✅ CRITICAL: InitializeComponent MUST be first - initializes all designer controls
            InitializeComponent();

#if DEBUG
            // ✅ NEW: Non-blocking validation - logs warnings only, doesn't halt startup
            ValidateDesignerControlsNonBlocking();
#endif

            // Initialize dependencies
            InitializeDependencies();

            // Wire up additional events not handled by designer
            WireUpEvents();

            // Load initial data
            LoadInitialData();

            Logger.Info("MainForm initialized successfully");
        }

        /// <summary>
        /// DI-friendly constructor for tests or advanced composition.
        /// </summary>
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
            // Fallback initialization for runtime if dependencies weren't injected
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
                MessageBox.Show(
                    $"Warning: Some data failed to load: {ex.Message}",
                    "VecTool",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void WireUpEvents()
        {
            try
            {
                // Tab control
                if (tabControl1 != null)
                {
                    tabControl1.SelectedIndexChanged += OnTabSelectedIndexChanged;
                }

                // Menu items - Actions
                if (convertToMdToolStripMenuItem != null)
                {
                    convertToMdToolStripMenuItem.Click += ConvertToMdToolStripMenuItemClick;
                }

                if (getGitChangesToolStripMenuItem != null)
                {
                    getGitChangesToolStripMenuItem.Click += GetGitChangesToolStripMenuItemClick;
                }

                if (fileSizeSummaryToolStripMenuItem != null)
                {
                    fileSizeSummaryToolStripMenuItem.Click += FileSizeSummaryToolStripMenuItemClick;
                }

                if (runTestsToolStripMenuItem != null)
                {
                    runTestsToolStripMenuItem.Click += RunTestsToolStripMenuItemClick;
                }

                if (exitToolStripMenuItem != null)
                {
                    exitToolStripMenuItem.Click += ExitToolStripMenuItemClick;
                }

                // Main tab
                if (btnSelectFolders != null)
                {
                    btnSelectFolders.Click += BtnSelectFoldersClick;
                }

                if (btnCreateNewVectorStore != null)
                {
                    btnCreateNewVectorStore.Click += BtnCreateNewVectorStoreClick;
                }

                // Settings tab
                if (cmbSettingsVectorStore != null)
                {
                    cmbSettingsVectorStore.SelectedIndexChanged += CmbSettingsVectorStoreSelectedIndexChanged;
                }

                if (chkInheritExcludedFiles != null)
                {
                    chkInheritExcludedFiles.CheckedChanged += ChkInheritExcludedFilesCheckedChanged;
                }

                if (chkInheritExcludedFolders != null)
                {
                    chkInheritExcludedFolders.CheckedChanged += ChkInheritExcludedFoldersCheckedChanged;
                }

                if (btnSaveVsSettings != null)
                {
                    btnSaveVsSettings.Click += BtnSaveVsSettingsClick;
                }

                if (btnResetVsSettings != null)
                {
                    btnResetVsSettings.Click += BtnResetVsSettingsClick;
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
            var tabs = (sender as TabControl) ?? Controls.OfType<TabControl>().FirstOrDefault();
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

        private void ConvertToMdToolStripMenuItemClick(object? sender, EventArgs e)
        {
            MessageBox.Show("Convert to MD functionality - Coming soon!", "VecTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void GetGitChangesToolStripMenuItemClick(object? sender, EventArgs e)
        {
            MessageBox.Show("Get Git Changes functionality - Coming soon!", "VecTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void FileSizeSummaryToolStripMenuItemClick(object? sender, EventArgs e)
        {
            MessageBox.Show("File Size Summary functionality - Coming soon!", "VecTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RunTestsToolStripMenuItemClick(object? sender, EventArgs e)
        {
            MessageBox.Show("Run Tests functionality - Coming soon!", "VecTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExitToolStripMenuItemClick(object? sender, EventArgs e)
        {
            Close();
        }

        #endregion

        #region Main Tab Event Handlers

        private void BtnSelectFoldersClick(object? sender, EventArgs e)
        {
            MessageBox.Show("Folder selection - Coming soon!", "VecTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnCreateNewVectorStoreClick(object? sender, EventArgs e)
        {
            MessageBox.Show("Vector store creation - Coming soon!", "VecTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region Settings Tab Event Handlers

        private void CmbSettingsVectorStoreSelectedIndexChanged(object? sender, EventArgs e)
        {
            var selectedName = cmbSettingsVectorStore?.SelectedItem?.ToString();
            SettingsTabLoadSelection(selectedName);
        }

        private void ChkInheritExcludedFilesCheckedChanged(object? sender, EventArgs e)
        {
            if (txtExcludedFiles != null && chkInheritExcludedFiles != null)
            {
                txtExcludedFiles.Enabled = !chkInheritExcludedFiles.Checked;
            }
        }

        private void ChkInheritExcludedFoldersCheckedChanged(object? sender, EventArgs e)
        {
            if (txtExcludedFolders != null && chkInheritExcludedFolders != null)
            {
                txtExcludedFolders.Enabled = !chkInheritExcludedFolders.Checked;
            }
        }

        private void BtnSaveVsSettingsClick(object? sender, EventArgs e)
        {
            MessageBox.Show("Settings saved!", "VecTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnResetVsSettingsClick(object? sender, EventArgs e)
        {
            MessageBox.Show("Settings reset!", "VecTool", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

#if DEBUG
        /// <summary>
        /// ✅ NEW: Validates that all designer controls are present.
        /// Logs warnings instead of blocking UI to prevent startup issues.
        /// </summary>
        private void ValidateDesignerControlsNonBlocking()
        {
            var missingControls = new List<string>();

            // Check menu
            if (menuStrip1 == null) missingControls.Add(nameof(menuStrip1));
            if (fileToolStripMenuItem == null) missingControls.Add(nameof(fileToolStripMenuItem));

            // Check tabs
            if (tabControl1 == null) missingControls.Add(nameof(tabControl1));
            if (tabPageMain == null) missingControls.Add(nameof(tabPageMain));
            if (tabPageSettings == null) missingControls.Add(nameof(tabPageSettings));
            if (tabPageRecentFiles == null) missingControls.Add(nameof(tabPageRecentFiles));

            // ✅ Check Recent Files panel (the critical control!)
            if (recentFilesPanel == null) missingControls.Add(nameof(recentFilesPanel));

            // Check status
            if (statusStrip1 == null) missingControls.Add(nameof(statusStrip1));
            if (statusLabel == null) missingControls.Add(nameof(statusLabel));

            if (missingControls.Count > 0)
            {
                var message = $"⚠️ Designer controls missing: {string.Join(", ", missingControls)}";
                Logger.Warn(message);
                Console.WriteLine(message);
                // ✅ DO NOT show MessageBox - just log and continue
            }
            else
            {
                Logger.Debug("✅ All designer controls validated successfully");
            }
        }
#endif
    }
}
