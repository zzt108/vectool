// Path: OaiUI/RecentFiles/RecentFilesPanel.cs
#nullable enable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VecTool.Configuration;
using VecTool.RecentFiles;
// Explicit alias to avoid ambiguity with System.Threading.Timer
using WinFormsTimer = System.Windows.Forms.Timer;

namespace oaiUI.RecentFiles
{
    /// <summary>
    /// Recent Files tab user control with filter, refresh and persisted column widths.
    /// Also increases row height by 10% for readability and applies a dark theme.
    /// </summary>
    public partial class RecentFilesPanel : UserControl
    {
        private const double DefaultRowHeightScale = 1.10;

        // Not readonly: designer creates the control, MainForm initializes it later if needed.
        private IRecentFilesManager? recentFilesManager;
        private readonly string? uiStateDirectory;

        // Debounce saving column widths during resize operations.
        private readonly WinFormsTimer saveDebounceTimer = new WinFormsTimer { Interval = 300 };

        private ImageList? rowHeightImageList;

        // Parameterless constructor required by Designer and used by MainForm Designer loader.
        public RecentFilesPanel()
        {
            InitializeComponent();
            SetupListView();
            ApplyRowHeightScale();
            ApplyThemeDark(); // Dark theme by default
            WireDragDrop();   // Enable drag-and-drop
            LoadLayout();

            if (lvRecentFiles != null)
            {
                lvRecentFiles.ColumnWidthChanged += OnColumnWidthChanged;
            }

            saveDebounceTimer.Tick += (_, __) =>
            {
                saveDebounceTimer.Stop();
                SaveLayout();
            };
        }

        // Preferred constructor for DI/tests with optional UI state directory.
        public RecentFilesPanel(IRecentFilesManager recentFilesManager, string? uiStateDirectory = null)
        {
            this.recentFilesManager = recentFilesManager ?? throw new ArgumentNullException(nameof(recentFilesManager));
            this.uiStateDirectory = uiStateDirectory;

            InitializeComponent();
            SetupListView();
            ApplyRowHeightScale();
            ApplyThemeDark(); // Dark theme by default
            WireDragDrop();   // Enable drag-and-drop
            LoadLayout();

            if (lvRecentFiles != null)
            {
                lvRecentFiles.ColumnWidthChanged += OnColumnWidthChanged;
            }

            saveDebounceTimer.Tick += (_, __) =>
            {
                saveDebounceTimer.Stop();
                SaveLayout();
            };
        }

        // Public API used by MainForm when the panel is created by the Designer.
        public void Initialize(IRecentFilesManager manager)
        {
            recentFilesManager = manager ?? throw new ArgumentNullException(nameof(manager));
            LoadLayout();
            RefreshList();
        }

        // Public API used by MainForm and tests to reload items.
        public void RefreshList()
        {
            if (lvRecentFiles is null)
            {
                return;
            }

            lvRecentFiles.BeginUpdate();
            try
            {
                lvRecentFiles.Items.Clear();

                IEnumerable<RecentFileInfo> items = recentFilesManager?.GetRecentFiles() ?? Array.Empty<RecentFileInfo>();
                var filter = txtFilter?.Text?.Trim();

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    items = items
                        .Where(f => f.FileName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                foreach (var f in items)
                {
                    var item = new ListViewItem(f.FileName)
                    {
                        Tag = f
                    };

                    // Columns: File, Type, Size (in KB with one decimal), Generated
                    item.SubItems.Add(f.FileType.ToString());
                    item.SubItems.Add($"{(f.FileSizeBytes / 1024.0):0.0} KB");
                    item.SubItems.Add(f.GeneratedAt.ToString("yyyy-MM-dd HH:mm"));

                    if (!f.Exists)
                    {
                        item.ForeColor = Color.Gray;
                        item.Font = new Font(lvRecentFiles.Font, FontStyle.Italic);
                    }

                    lvRecentFiles.Items.Add(item);
                }

                lblStatus.Text = $"{lvRecentFiles.Items.Count} files";
            }
            finally
            {
                lvRecentFiles.EndUpdate();
            }
        }

        // Designer-wired events
        private void txtFilterTextChanged(object? sender, EventArgs e)
        {
            RefreshList();
        }

        private void btnRefreshClick(object? sender, EventArgs e)
        {
            RefreshList();
        }

        private void RecentFilesPanelLoad(object? sender, EventArgs e)
        {
            RefreshList();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    // Dispose Designer container declared in the partial Designer file.
                    components?.Dispose();

                    if (lvRecentFiles != null)
                    {
                        lvRecentFiles.ColumnWidthChanged -= OnColumnWidthChanged;
                        lvRecentFiles.DragEnter -= OnListViewDragEnter;
                        lvRecentFiles.DragDrop -= OnListViewDragDrop;
                        SaveLayout();
                    }

                    rowHeightImageList?.Dispose();
                    saveDebounceTimer.Dispose();
                }
                catch
                {
                    // Defensive: never crash UI during Dispose
                }
            }

            base.Dispose(disposing);
        }

        private void SetupListView()
        {
            if (lvRecentFiles is null) return;

            lvRecentFiles.View = View.Details;
            lvRecentFiles.FullRowSelect = true;
            lvRecentFiles.GridLines = true;
            lvRecentFiles.MultiSelect = true;

            // Create columns once if not defined by designer.
            if (lvRecentFiles.Columns.Count == 0)
            {
                lvRecentFiles.Columns.Add(new ColumnHeader { Text = "File", Width = 400, Name = "colFile" });
                lvRecentFiles.Columns.Add(new ColumnHeader { Text = "Type", Width = 120, Name = "colType", TextAlign = HorizontalAlignment.Left });
                lvRecentFiles.Columns.Add(new ColumnHeader { Text = "Size", Width = 120, Name = "colSize", TextAlign = HorizontalAlignment.Right });
                lvRecentFiles.Columns.Add(new ColumnHeader { Text = "Generated", Width = 220, Name = "colGenerated", TextAlign = HorizontalAlignment.Left });
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
                ImageSize = new Size(1, Math.Max(desired, 18))
            };
            lvRecentFiles.SmallImageList = rowHeightImageList;
        }

        private void OnColumnWidthChanged(object? sender, ColumnWidthChangedEventArgs e)
        {
            // Debounce disk writes while resizing.
            saveDebounceTimer.Stop();
            saveDebounceTimer.Start();
        }

        private void LoadLayout()
        {
            if (lvRecentFiles is null) return;

            var state = UiStateConfig.Load(uiStateDirectory);
            var widths = state.RecentFilesColumnWidths;

            if (widths is null || widths.Count == 0)
            {
                // No prior state: autosize as a reasonable default for existing content.
                TryAutoSizeColumns();
                return;
            }

            foreach (ColumnHeader col in lvRecentFiles.Columns)
            {
                if (widths.TryGetValue(col.Text, out var w) && w > 0)
                {
                    col.Width = w;
                }
            }
        }

        private void SaveLayout()
        {
            if (lvRecentFiles is null) return;

            var current = UiStateConfig.Load(uiStateDirectory);
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (ColumnHeader col in lvRecentFiles.Columns)
            {
                map[col.Text] = col.Width;
            }

            current.RecentFilesColumnWidths = map;
            current.RecentFilesRowHeightScale = DefaultRowHeightScale;
            UiStateConfig.Save(current, uiStateDirectory);
        }

        private void TryAutoSizeColumns()
        {
            if (lvRecentFiles is null) return;
            try
            {
                foreach (ColumnHeader col in lvRecentFiles.Columns)
                {
                    col.Width = -2; // auto-size to header/content
                }
            }
            catch
            {
                // Nothing fatal, sizing will be adjusted by the user anyway
            }
        }

        // Exposed for tests
        internal void SaveLayoutForTesting() => SaveLayout();

        // Theming: apply a dark palette to the panel and the ListView without changing logic.
        private void ApplyThemeDark()
        {
            var panelBack = Color.FromArgb(32, 32, 32);
            var panelFore = Color.Gainsboro;

            this.BackColor = panelBack;
            this.ForeColor = panelFore;

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
                lblStatus.ForeColor = Color.Silver;
            }

            if (txtFilter != null)
            {
                txtFilter.BackColor = Color.FromArgb(45, 45, 45);
                txtFilter.ForeColor = panelFore;
                txtFilter.BorderStyle = BorderStyle.FixedSingle;
            }

            if (btnRefresh != null)
            {
                btnRefresh.FlatStyle = FlatStyle.System; // keeps native theming
                btnRefresh.BackColor = panelBack;
                btnRefresh.ForeColor = panelFore;
            }

            if (lvRecentFiles != null)
            {
                lvRecentFiles.BackColor = Color.FromArgb(24, 24, 24);
                lvRecentFiles.ForeColor = Color.Gainsboro;
                lvRecentFiles.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        // --------------- Drag & Drop wiring and handlers ---------------

        private void WireDragDrop()
        {
            if (lvRecentFiles is null) return;
            lvRecentFiles.AllowDrop = true;
            lvRecentFiles.DragEnter += OnListViewDragEnter;
            lvRecentFiles.DragDrop += OnListViewDragDrop;
        }

        private void OnListViewDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void OnListViewDragDrop(object? sender, DragEventArgs e)
        {
            if (recentFilesManager is null) return;
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true) return;

            try
            {
                var data = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (data is null || data.Length == 0) return;

                var sourceFolders = GetCurrentVectorStoreSourceFolders();
                var added = 0;

                foreach (var path in data)
                {
                    if (string.IsNullOrWhiteSpace(path)) continue;
                    if (!File.Exists(path)) continue;

                    var type = MapExtensionToType(Path.GetExtension(path));
                    long size = 0;
                    try { size = new FileInfo(path).Length; } catch { size = 0; }

                    // Register and associate with current vector store folders
                    recentFilesManager.RegisterGeneratedFile(
                        filePath: path,
                        fileType: type,
                        sourceFolders: sourceFolders,
                        fileSizeBytes: size,
                        generatedAtUtc: DateTime.UtcNow);

                    added++;
                }

                if (added > 0)
                {
                    recentFilesManager.Save();
                    RefreshList();
                }
            }
            catch
            {
                // Be defensive: never crash on bad drops
            }
        }

        private static RecentFileType MapExtensionToType(string? ext)
        {
            var e = (ext ?? string.Empty).Trim().ToLowerInvariant();
            return e switch
            {
                ".docx" => RecentFileType.Docx,
                ".pdf" => RecentFileType.Pdf,
                ".md" => RecentFileType.Md,
                ".markdown" => RecentFileType.Md,
                _ => RecentFileType.Unknown
            };
        }

        private IReadOnlyList<string> GetCurrentVectorStoreSourceFolders()
        {
            try
            {
                var state = UiStateConfig.Load(uiStateDirectory);
                var vsName = state.LastSelectedVectorStore;
                if (string.IsNullOrWhiteSpace(vsName))
                    return Array.Empty<string>();

                var all = VectorStoreConfig.LoadAll();
                if (all.TryGetValue(vsName, out var cfg) && cfg.FolderPaths is { Count: > 0 })
                {
                    // Use configured folders of the active vector store to bind the dropped files
                    return cfg.FolderPaths.ToList();
                }
            }
            catch
            {
                // Ignore and fall back
            }
            return Array.Empty<string>();
        }
    }
}
