// ✅ FULL FILE VERSION
// Path: src/VecTool.UI/OaiUI/MainForm.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VecTool.Configuration;
using VecTool.Core;
using VecTool.Handlers;
using VecTool.RecentFiles;
using oaiUI;
using oaiUI.Services;

namespace VecTool.OaiUI
{
    // Orchestration partial: keeps fields, constructors, InitializeComponent, wiring, and high-level flow.
    public partial class MainForm : Form
    {
        // UI services and data
        private WinFormsUserInterface userInterface;
        private IRecentFilesManager recentFilesManager;

        // Selected folders bound to the listbox
        private readonly List<string> selectedFolders = new();

        // All known vector stores
        private Dictionary<string, VectorStoreConfig> allVectorStoreConfigs = new();

        // Persist last vector store selection
        private readonly ILastSelectionService lastSelection = new LastSelectionService();

        public MainForm()
        {
            InitializeComponent();

            // Initialize UI service wrappers
            userInterface = new WinFormsUserInterface(statusLabel, progressBar);

            // Initialize Recent Files
            var recentFilesConfig = RecentFilesConfig.FromAppConfig();
            Directory.CreateDirectory(recentFilesConfig.OutputPath);
            var recentFilesStore = new FileRecentFilesStore(recentFilesConfig);
            recentFilesManager = new RecentFilesManager(recentFilesConfig, recentFilesStore);
            recentFilesManager.Load();

            // Recent files panel created by designer - initialize once controls exist
            recentFilesPanel.Initialize(recentFilesManager);

            WireUpEvents();

            // Load vector stores into combo box (implementation in MainForm.VectorStoreManagement.cs)
            LoadVectorStoresIntoComboBox();
        }

        private void WireUpEvents()
        {
            // Menu items
            convertToMdToolStripMenuItem.Click += convertToMdToolStripMenuItemClick;
            getGitChangesToolStripMenuItem.Click += getGitChangesToolStripMenuItemClick;
            fileSizeSummaryToolStripMenuItem.Click += fileSizeSummaryToolStripMenuItemClick;
            runTestsToolStripMenuItem.Click += runTestsToolStripMenuItemClick;
            exitToolStripMenuItem.Click += exitToolStripMenuItemClick;

            // Form controls
            btnSelectFolders.Click += btnSelectFoldersClick;
            comboBoxVectorStores.SelectedIndexChanged += comboBoxVectorStoresSelectedIndexChanged;
            btnCreateNewVectorStore.Click += btnCreateNewVectorStoreClick;

            // Tab change refresh (ensure recent files grid stays fresh)
            tabControl1.SelectedIndexChanged += (sender, args) =>
            {
                if (tabControl1.SelectedTab == tabPageRecentFiles)
                {
                    recentFilesPanel.RefreshList();
                }
            };
        }

        // Note: UpdateFormTitle implementation moved to MainForm.VectorStoreManagement.cs.
        // Note: SanitizeFileName implementation moved to MainForm.FileOperations.cs.
        // Note: All file operation handlers moved to MainForm.FileOperations.cs.
        // Note: Vector store handlers and helpers moved to MainForm.VectorStoreManagement.cs.
        // Note: Folder selection handlers moved to MainForm.FolderSelection.cs.
    }
}
