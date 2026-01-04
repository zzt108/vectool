// File: OaiUI/MainForm.Core.cs

using Microsoft.Extensions.Logging;
using oaiUI;
using oaiUI.Services;
using Vectool.UI.Versioning;
using VecTool.Configuration;
using VecTool.Configuration.Helpers;
using VecTool.Core.Configuration;
using VecTool.Core.Helpers;
using VecTool.Core.Models;
using VecTool.Handlers;
using VecTool.RecentFiles;
using VecTool.UI.Panels;

namespace Vectool.OaiUI
{
    /// <summary>
    /// MainForm partial: Core initialization, fields, constructor, and event wiring.
    /// </summary>
    public partial class MainForm : Form
    {
        // ✅ UI services and data
        private WinFormsUserInterface userInterface;

        private IRecentFilesManager recentFilesManager;

        // ✅ Selected folders bound to the listbox
        private readonly List<string> selectedFolders = new();

        // ✅ All known vector stores
        private Dictionary<string, VectorStoreConfig> allVectorStoreConfigs = new();

        // ✅ Persist last vector store selection
        private readonly ILastSelectionService lastSelection = new LastSelectionService();

        private readonly IVersionProvider versionProvider;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<MainForm> logger;

        /// <summary>
        /// Constructor: Initializes MainForm with version provider.
        /// </summary>
        public MainForm(IVersionProvider versionProvider, ILoggerFactory loggerFactory)
        {
            this.versionProvider = versionProvider.ThrowIfNull(nameof(versionProvider));

            this.loggerFactory = loggerFactory.ThrowIfNull(nameof(loggerFactory));
            this.logger = this.loggerFactory.CreateLogger<MainForm>();

            InitializeComponent();

            // ✅ Initialize UI service wrappers
            userInterface = new WinFormsUserInterface(statusLabel, progressBar);

            // ✅ Initialize Recent Files
            var recentFilesConfig = RecentFilesConfig.FromAppConfig();
            Directory.CreateDirectory(recentFilesConfig.OutputPath);
            var recentFilesStore = new FileRecentFilesStore(recentFilesConfig);
            recentFilesManager = new RecentFilesManager(recentFilesConfig, recentFilesStore);
            recentFilesManager.Load();

            // ✅ Recent files panel created by designer; initialize once controls exist
            recentFilesPanel.Initialize(recentFilesManager);

            var promptsConfig = PromptsConfig.FromAppConfig();
            var searchEngine = new PromptSearchEngine(promptsConfig);
            var favoritesManager = new FavoritesManager();

            if (promptsConfig is not null)
            {
                promptsBrowserPanel = new PromptsBrowserPanel();
                tabPagePrompts.Controls.Add(promptsBrowserPanel);
                promptsBrowserPanel.Dock = DockStyle.Fill;
                promptsBrowserPanel.Initialize(searchEngine, favoritesManager, promptsConfig?.RepositoryPath);
            }

            WireUpEvents();
            LoadVectorStoresIntoComboBox();
            UpdateFormTitle();
        }

        /// <summary>
        /// Wires up all event handlers for menu items and form controls.
        /// </summary>
        private void WireUpEvents()
        {
            // ✅ Menu items
            convertToMdToolStripMenuItem.Click += convertToMdToolStripMenuItem_Click;
            getGitChangesToolStripMenuItem.Click += getGitChangesToolStripMenuItem_Click;
            fileSizeSummaryToolStripMenuItem.Click += fileSizeSummaryToolStripMenuItem_Click;
            runTestsToolStripMenuItem.Click += runTestsToolStripMenuItem_Click;
            exportToRepomixToolStripMenuItem.Click += exportToRepomixToolStripMenuItemClick;
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;

            // ✅ Form controls
            btnSelectFolders.Click += btnSelectFoldersClick;
            comboBoxVectorStores.SelectedIndexChanged += comboBoxVectorStoresSelectedIndexChanged;
            btnCreateNewVectorStore.Click += btnCreateNewVectorStoreClick;

            // ✅ Tab change refresh – ensure recent files grid stays fresh
            tabControl1.SelectedIndexChanged += (sender, args) =>
            {
                if (tabControl1.SelectedTab == tabPageRecentFiles)
                {
                    recentFilesPanel.RefreshList();
                }
            };
        }

        /// <summary>
        /// Updates the form title with vector store name and version.
        /// </summary>
        private void UpdateFormTitle()
        {
            var selectedName = comboBoxVectorStores.SelectedItem?.ToString();
            var version = versionProvider.FileVersion;
            var baseName = $"VecTool v{version}";

            this.Text = string.IsNullOrEmpty(selectedName)
                ? baseName
                : $"{baseName} - {selectedName}";
        }
    }
}