// ✅ FULL FILE VERSION
#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VecTool.Configuration;
using VecTool.RecentFiles;

namespace oaiUI.RecentFiles
{
    /// <summary>
    /// RecentFilesPanel partial: Core - initialization, fields, constructors, DI, sorting.
    /// </summary>
    public sealed partial class RecentFilesPanel : UserControl
    {
        // ✅ NEW - Sort state fields
        private int sortColumn = 0;
        private SortOrder sortOrder = SortOrder.Ascending;

        // Dependencies
        private IRecentFilesManager? recentFilesManager;
        private readonly string? uiStateDirectory;

        // Internal State
        private readonly System.Windows.Forms.Timer saveDebounceTimer = new() { Interval = 300 };
        private ImageList? rowHeightImageList;

        // 🔄 MODIFY - FileSystemWatcher for auto-refresh
        private FileSystemWatcher? fileWatcher;
        private readonly System.Windows.Forms.Timer refreshDebounceTimer = new() { Interval = 500 };

        private const double DefaultRowHeightScale = 1.10;

        // Constructors

        /// <summary>
        /// Parameterless constructor for Designer compatibility.
        /// </summary>
        public RecentFilesPanel()
        {
            InitializeComponent(); // Designer-generated
            WireRuntime();
        }

        /// <summary>
        /// Preferred constructor for DI/tests with explicit manager injection.
        /// </summary>
        public RecentFilesPanel(IRecentFilesManager recentFilesManager, string? uiStateDirectory = null)
        {
            this.recentFilesManager = recentFilesManager ?? throw new ArgumentNullException(nameof(recentFilesManager));
            this.uiStateDirectory = uiStateDirectory;
            InitializeComponent();
            WireRuntime();
        }

        /// <summary>
        /// Allows late initialization when created by Designer and composed in MainForm.
        /// </summary>
        public void Initialize(IRecentFilesManager manager)
        {
            recentFilesManager = manager ?? throw new ArgumentNullException(nameof(manager));
            LoadLayout();
            RefreshList();
        }

        /// <summary>
        /// Exposed for tests to force immediate save.
        /// </summary>
        internal void SaveLayoutForTesting()
        {
            SaveLayout();
        }

        // Initialization Helpers

        /// <summary>
        /// Wire up runtime events and configurations not handled by the Designer.
        /// </summary>
        /// <summary>
        /// 🔄 MODIFY - Wire up all runtime event handlers after InitializeComponent.
        /// </summary>
        private void WireRuntime()
        {
            if (lvRecentFiles is null) return;

            // ✅ NEW - Initialize context menu
            InitializeContextMenu();

            // Existing event handlers
            Load += RecentFilesPanelLoad;
            if (txtFilter is not null)
                txtFilter.TextChanged += txtFilterTextChanged;
            if (btnRefresh is not null)
                btnRefresh.Click += btnRefreshClick;
            if (lvRecentFiles is not null)
            {
                lvRecentFiles.ColumnWidthChanged += OnColumnWidthChanged;
                lvRecentFiles.DragEnter += OnListViewDragEnter;
                lvRecentFiles.DragDrop += OnListViewDragDrop;
                lvRecentFiles.ItemDrag += OnListViewItemDrag;
                lvRecentFiles.ColumnClick += OnColumnClick;
            }

            refreshDebounceTimer.Tick += (s, e) =>
            {
                refreshDebounceTimer.Stop();
                RefreshList();
            };

            saveDebounceTimer.Tick += (s, e) =>
            {
                saveDebounceTimer.Stop();
                SaveLayout();
            };

            SetupListView();
            ApplyRowHeightScale();
            ApplyFontSizeFromConfig();
            ApplyThemeDark();
            WireFileSystemWatcher();
        }

        /// <summary>
        /// ✅ NEW - Applies font size from UiState or App.config.
        /// </summary>
        private void ApplyFontSizeFromConfig()
        {
            if (lvRecentFiles is null) return;

            try
            {
                // Load from UiState first
                var state = UiStateConfig.Load(uiStateDirectory);
                double? fontSize = state.RecentFilesFontSize;

                // Fallback to App.config if not in UiState
                if (!fontSize.HasValue)
                {
                    var configValue = System.Configuration.ConfigurationManager.AppSettings["recentFilesFontSize"];
                    if (!string.IsNullOrWhiteSpace(configValue) && double.TryParse(configValue, out var parsed))
                    {
                        fontSize = parsed;

                        // Save to UiState for next time
                        state.RecentFilesFontSize = fontSize;
                        UiStateConfig.Save(state, uiStateDirectory);
                    }
                }

                // Apply font size if valid
                if (fontSize.HasValue && fontSize.Value > 0)
                {
                    var currentFont = lvRecentFiles.Font;
                    lvRecentFiles.Font = new Font(currentFont.FontFamily, (float)fontSize.Value, currentFont.Style);
                }
            }
            catch
            {
                // Defensive - don't crash if config reading fails
            }
        }

        /// <summary>
        /// ✅ NEW - Handles ListView column click for sorting.
        /// </summary>
        private void OnColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (lvRecentFiles is null) return;

            // Toggle sort order if same column, otherwise reset to ascending
            if (e.Column == sortColumn)
            {
                sortOrder = sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                sortColumn = e.Column;
                sortOrder = SortOrder.Ascending;
            }

            // Apply sorting
            lvRecentFiles.ListViewItemSorter = new RecentFilesListViewComparer(sortColumn, sortOrder);
            lvRecentFiles.Sort();

            // Update column headers with sort indicators
            UpdateColumnHeaders();
        }

        /// <summary>
        /// ✅ NEW - Updates column headers to show sort indicators (▲/▼).
        /// </summary>
        private void UpdateColumnHeaders()
        {
            if (lvRecentFiles is null) return;

            for (int i = 0; i < lvRecentFiles.Columns.Count; i++)
            {
                var col = lvRecentFiles.Columns[i];
                var baseText = (col.Tag as string) ?? col.Text.Replace(" ▲", "").Replace(" ▼", "");

                if (i == sortColumn)
                {
                    col.Text = baseText + (sortOrder == SortOrder.Ascending ? " ▲" : " ▼");
                }
                else
                {
                    col.Text = baseText;
                }
            }
        }

        /// <summary>
        /// 🔄 MODIFY - Wires FileSystemWatcher to monitor the output directory for new files.
        /// </summary>
        private void WireFileSystemWatcher()
        {
            try
            {
                var config = RecentFilesConfig.FromAppConfig();
                var watchPath = config.OutputPath;

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
                refreshDebounceTimer.Tick += (_, _) =>
                {
                    refreshDebounceTimer.Stop();
                    RefreshList();
                };
            }
            catch
            {
                // Defensive - don't crash if watcher fails
            }
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

            // Add columns if not already defined by Designer
            if (lvRecentFiles.Columns.Count == 0)
            {
                // 🔄 MODIFY - Store base header text in Tag for stable persistence key
                var colFile = new ColumnHeader { Text = "File", Width = 400, Name = "colFile", Tag = "File" };
                var colType = new ColumnHeader { Text = "Type", Width = 120, Name = "colType", TextAlign = HorizontalAlignment.Left, Tag = "Type" };
                var colSize = new ColumnHeader { Text = "Size", Width = 120, Name = "colSize", TextAlign = HorizontalAlignment.Right, Tag = "Size" };
                var colGenerated = new ColumnHeader { Text = "Generated", Width = 220, Name = "colGenerated", TextAlign = HorizontalAlignment.Left, Tag = "Generated" };

                lvRecentFiles.Columns.Add(colFile);
                lvRecentFiles.Columns.Add(colType);
                lvRecentFiles.Columns.Add(colSize);
                lvRecentFiles.Columns.Add(colGenerated);
            }
            else
            {
                // Ensure existing columns have Tag set for base header
                foreach (ColumnHeader col in lvRecentFiles.Columns)
                {
                    if (col.Tag is null)
                    {
                        col.Tag = col.Text.Replace(" ▲", "").Replace(" ▼", "");
                    }
                }
            }
        }

        private void ApplyRowHeightScale()
        {
            if (lvRecentFiles is null) return;

            var state = UiStateConfig.Load(uiStateDirectory);
            var scale = state.RecentFilesRowHeightScale ?? DefaultRowHeightScale;

            var baseHeight = lvRecentFiles.Font.Height + 6;
            var desired = (int)Math.Ceiling(baseHeight * scale);

            rowHeightImageList?.Dispose();
            rowHeightImageList = new ImageList
            {
                ImageSize = new System.Drawing.Size(1, Math.Max(desired, 18))
            };

            lvRecentFiles.SmallImageList = rowHeightImageList;
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

        // Disposal
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (lvRecentFiles is not null)
                    {
                        lvRecentFiles.ColumnWidthChanged -= OnColumnWidthChanged;
                        lvRecentFiles.DragEnter -= OnListViewDragEnter;
                        lvRecentFiles.DragDrop -= OnListViewDragDrop;
                        lvRecentFiles.ItemDrag -= OnListViewItemDrag;
                        lvRecentFiles.ColumnClick -= OnColumnClick;
                    }

                    SaveLayout();
                    rowHeightImageList?.Dispose();
                    saveDebounceTimer.Dispose();

                    // 🔄 MODIFY - Cleanup FileSystemWatcher
                    if (fileWatcher is not null)
                    {
                        fileWatcher.EnableRaisingEvents = false;
                        fileWatcher.Created -= OnFileSystemChanged;
                        fileWatcher.Changed -= OnFileSystemChanged;
                        fileWatcher.Dispose();
                        fileWatcher = null;
                    }

                    refreshDebounceTimer.Dispose();

                    // ✅ NEW - Cleanup context menu
                    DisposeContextMenu();

                    components?.Dispose();
                }
                catch
                {
                    // Defensive - swallow disposal errors
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// ✅ NEW - Comparer for ListView item sorting.
        /// </summary>
        private sealed class RecentFilesListViewComparer : System.Collections.IComparer
        {
            private readonly int column;
            private readonly SortOrder order;

            public RecentFilesListViewComparer(int column, SortOrder order)
            {
                this.column = column;
                this.order = order;
            }

            public int Compare(object? x, object? y)
            {
                if (x is not ListViewItem itemX || y is not ListViewItem itemY)
                    return 0;

                if (column < 0 || column >= itemX.SubItems.Count || column >= itemY.SubItems.Count)
                    return 0;

                var textX = itemX.SubItems[column].Text;
                var textY = itemY.SubItems[column].Text;

                int result;

                // Column-specific comparison
                switch (column)
                {
                    case 0: // File - string comparison
                    case 1: // Type - string comparison
                        result = string.Compare(textX, textY, StringComparison.OrdinalIgnoreCase);
                        break;

                    case 2: // Size - numeric comparison (parse "123.45 KB")
                        result = CompareSize(textX, textY);
                        break;

                    case 3: // Generated - DateTime comparison
                        result = CompareDateTime(textX, textY);
                        break;

                    default:
                        result = string.Compare(textX, textY, StringComparison.OrdinalIgnoreCase);
                        break;
                }

                // Apply sort order
                return order == SortOrder.Descending ? -result : result;
            }

            private static int CompareSize(string textX, string textY)
            {
                var sizeX = ParseSize(textX);
                var sizeY = ParseSize(textY);
                return sizeX.CompareTo(sizeY);
            }

            private static double ParseSize(string text)
            {
                // Parse "123.45 KB" format
                var parts = text.Split(' ');
                if (parts.Length > 0 && double.TryParse(parts[0], out var size))
                {
                    return size;
                }
                return 0.0;
            }

            private static int CompareDateTime(string textX, string textY)
            {
                var dateX = ParseDateTime(textX);
                var dateY = ParseDateTime(textY);
                return dateX.CompareTo(dateY);
            }

            private static DateTime ParseDateTime(string text)
            {
                // Parse "yyyy-MM-dd HH:mm" format
                if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var date))
                {
                    return date;
                }
                return DateTime.MinValue;
            }
        }
    }
}
