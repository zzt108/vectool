// ✅ FULL FILE VERSION
// Path: OaiUI/RecentFiles/RecentFilesPanel.Core.cs
#nullable enable

using System;
using System.Configuration;
using System.IO;
using System.Windows.Forms;
using VecTool.Configuration;
using VecTool.RecentFiles;

namespace oaiUI.RecentFiles
{
    /// <summary>
    /// RecentFilesPanel partial: Core initialization and runtime wiring.
    /// </summary>
    public sealed partial class RecentFilesPanel : UserControl
    {
        // Fields
        private IRecentFilesManager? recentFilesManager;
        private System.Windows.Forms.Timer refreshDebounceTimer = null!;
        private FileSystemWatcher? fileWatcher;
        private string? uiStateDirectory;
        private string? watchPath;
        private double DefaultRowHeightScale = 1.10;
        private double appliedRowHeightScale = 1.10;

        public RecentFilesPanel()
        {
            InitializeComponent(); // Creates all designer controls including lvRecentFiles
        }

        public RecentFilesPanel(IRecentFilesManager manager, string? uiStateDir = null, string? watchDirectory = null) :this()
        { 
            Initialize(manager, uiStateDir, watchDirectory);
        }

        // ==================== Public API ====================

        /// <summary>
        /// Initialize the panel with a RecentFilesManager instance.
        /// Call this ONCE from MainForm after the control is fully created.
        /// </summary>
        public RecentFilesPanel Initialize(IRecentFilesManager manager, string? uiStateDir = null, string? watchDirectory = null)
        {
            recentFilesManager = manager ?? throw new ArgumentNullException(nameof(manager));
            uiStateDirectory = uiStateDir;
            watchPath = watchDirectory;
            var rowHeightScaleStr = ConfigurationManager.AppSettings["recentFilesRowHeightScale"];
            if (double.TryParse(rowHeightScaleStr, out var scale) && scale > 0 && scale <= 3.0)
            {
                DefaultRowHeightScale = scale;
                appliedRowHeightScale = scale; // start with the configured value

            }

            // Setup ListView columns BEFORE wiring runtime events
            SetupListView();

            // Setup timer and watcher
            refreshDebounceTimer = new System.Windows.Forms.Timer { Interval = 500 };
            SetupFileWatcher();

            // Wire runtime events and restore UI state
            WireRuntime();
            LoadUiState();

            // Initial refresh
            RefreshList();
            ApplyThemeDark();
            return this;
        }

        // ==================== Runtime Wiring ====================

        private void WireRuntime()
        {
            if (lvRecentFiles is null) return;

            // Context menu
            InitializeContextMenu();

            // ✅ NEW - Call WireDragDrop to centralize AllowDrop and drag-drop event wiring
            WireDragDrop();

            // Other runtime handlers
            if (lvRecentFiles is not null)
            {
                lvRecentFiles.ColumnWidthChanged += OnColumnWidthChanged;
                // ❌ REMOVE - duplicated DragEnter/DragDrop/ItemDrag subscriptions
                // lvRecentFiles.DragEnter += OnListViewDragEnter;
                // lvRecentFiles.DragDrop += OnListViewDragDrop;
                // lvRecentFiles.ItemDrag += OnListViewItemDrag;
                lvRecentFiles.ColumnClick += OnColumnClick;
            }

            if (btnRefresh is not null)
                btnRefresh.Click += (s, e) => RefreshList();

            if (txtFilter is not null)
                txtFilter.TextChanged += (s, e) => RefreshList();
        }

        // ==================== File System Watching ====================

        private void SetupFileWatcher()
        {
            if (recentFilesManager is null) return;

            if (string.IsNullOrWhiteSpace(watchPath) || !Directory.Exists(watchPath))
                return;

            fileWatcher = new FileSystemWatcher(watchPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                Filter = "*.*",
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            fileWatcher.Created += OnFileSystemChanged;
            fileWatcher.Changed += OnFileSystemChanged;
            fileWatcher.Renamed += OnFileSystemChanged;

            // Debounce timer to avoid excessive refreshes
            refreshDebounceTimer.Tick += (_, __) =>
            {
                refreshDebounceTimer.Stop();
                RefreshList();
            };
        }

        /// <summary>
        /// ✅ NEW - Handles FileSystemWatcher events with debouncing.
        /// </summary>
        private void OnFileSystemChanged(object? sender, FileSystemEventArgs e)
        {
            // Restart debounce timer on each event
            refreshDebounceTimer.Stop();
            refreshDebounceTimer.Start();
        }

        private void SetupListView()
        {
            if (lvRecentFiles is null) return;

            lvRecentFiles.View = View.Details;
            lvRecentFiles.FullRowSelect = true;
            lvRecentFiles.GridLines = true;
            lvRecentFiles.MultiSelect = true;

            // Always ensure columns exist (Designer doesn't define them)
            if (lvRecentFiles.Columns.Count == 0)
            {
                lvRecentFiles.Columns.Add("File", 300);
                lvRecentFiles.Columns.Add("Type", 100);
                lvRecentFiles.Columns.Add("Size", 80);
                lvRecentFiles.Columns.Add("Generated", 140);
            }
            // If columns exist but have no width, reset them
            else
            {
                for (int i = 0; i < lvRecentFiles.Columns.Count; i++)
                {
                    if (lvRecentFiles.Columns[i].Width == 0)
                    {
                        lvRecentFiles.Columns[i].Width = i switch
                        {
                            0 => 300, // File
                            1 => 100, // Type
                            2 => 80,  // Size
                            3 => 140, // Generated
                            _ => 100
                        };
                    }
                }
            }
        }

        // ==================== UI State Persistence ====================

        private void LoadUiState()
        {
            try
            {
                var state = UiStateConfig.Load(uiStateDirectory);

                // Restore column widths
                if (lvRecentFiles is not null && state.RecentFilesColumnWidths.Count > 0)
                {
                    foreach (ColumnHeader col in lvRecentFiles.Columns)
                    {
                        if (state.RecentFilesColumnWidths.TryGetValue(col.Text, out int width))
                        {
                            col.Width = width;
                        }
                    }
                }

                // Restore font size if configured
                if (state.RecentFilesFontSize.HasValue && lvRecentFiles is not null)
                {
                    lvRecentFiles.Font = new System.Drawing.Font(
                        lvRecentFiles.Font.FontFamily,
                        (float)state.RecentFilesFontSize.Value,
                        lvRecentFiles.Font.Style);
                }
            }
            catch
            {
                // Defensive: never crash UI on load errors
            }
        }

        private void SaveUiState()
        {
            try
            {
                var state = new UiStateConfig.UiState();

                // Save column widths
                if (lvRecentFiles is not null)
                {
                    foreach (ColumnHeader col in lvRecentFiles.Columns)
                    {
                        state.RecentFilesColumnWidths[col.Text] = col.Width;
                    }

                    // Save font size
                    state.RecentFilesFontSize = lvRecentFiles.Font.Size;
                }

                UiStateConfig.Save(state, uiStateDirectory);
            }
            catch
            {
                // Defensive: never crash UI on save errors
            }
        }

        private void OnColumnWidthChanged(object? sender, ColumnWidthChangedEventArgs e)
        {
            SaveUiState();
        }

        // ==================== Sorting ====================

        private int sortColumn = -1;
        private SortOrder sortOrder = SortOrder.None;

        private void OnColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (lvRecentFiles is null) return;

            // Toggle sort order
            if (e.Column == sortColumn)
            {
                sortOrder = sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                sortColumn = e.Column;
                sortOrder = SortOrder.Ascending;
            }

            lvRecentFiles.ListViewItemSorter = new ListViewItemComparer(e.Column, sortOrder);
            lvRecentFiles.Sort();
        }

        private void ApplyThemeDark()
        {
            // Dark-ish theme; safe if overridden elsewhere
            var panelBack = System.Drawing.Color.FromArgb(32, 32, 32);
            var panelFore = System.Drawing.Color.Gainsboro;

            BackColor = panelBack;
            ForeColor = panelFore;

            if (tableLayoutPanel != null)
            {
                tableLayoutPanel.BackColor = panelBack;
                tableLayoutPanel.ForeColor = panelFore;
            }

            if (lblFilter != null)
            {
                lblFilter.BackColor = panelBack;
                lblFilter.ForeColor = panelFore;
            }

            if (lblStatus != null)
            {
                lblStatus.BackColor = panelBack;
                lblStatus.ForeColor = System.Drawing.Color.Silver;
            }

            if (txtFilter != null)
            {
                txtFilter.BackColor = System.Drawing.Color.FromArgb(45, 45, 45);
                txtFilter.ForeColor = panelFore;
                txtFilter.BorderStyle = BorderStyle.FixedSingle;
            }

            if (btnRefresh != null)
            {
                btnRefresh.FlatStyle = FlatStyle.System;
                btnRefresh.BackColor = panelBack;
                btnRefresh.ForeColor = panelFore;
            }

            if (lvRecentFiles != null)
            {
                lvRecentFiles.BackColor = System.Drawing.Color.FromArgb(24, 24, 24);
                lvRecentFiles.ForeColor = System.Drawing.Color.Gainsboro;
                lvRecentFiles.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        private class ListViewItemComparer : System.Collections.IComparer
        {
            private readonly int column;
            private readonly SortOrder order;

            public ListViewItemComparer(int column, SortOrder order)
            {
                this.column = column;
                this.order = order;
            }

            public int Compare(object? x, object? y)
            {
                if (x is not ListViewItem itemX || y is not ListViewItem itemY)
                    return 0;

                var textX = itemX.SubItems.Count > column ? itemX.SubItems[column].Text : string.Empty;
                var textY = itemY.SubItems.Count > column ? itemY.SubItems[column].Text : string.Empty;

                int result = string.Compare(textX, textY, StringComparison.OrdinalIgnoreCase);
                return order == SortOrder.Descending ? -result : result;
            }
        }

        // ==================== Dispose ====================

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fileWatcher?.Dispose();
                refreshDebounceTimer?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
