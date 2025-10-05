// ✅ FULL FILE VERSION
// Path: OaiUI/RecentFiles/RecentFilesPanel.cs

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using oaiUI.Services;
using VecTool.Configuration;
using VecTool.RecentFiles;

namespace oaiUI.RecentFiles
{
    /// <summary>
    /// Recent Files tab user control with filter, refresh and persisted layout.
    /// Note: Designer-generated elements live in RecentFilesPanel.Designer.cs.
    /// </summary>
    public partial class RecentFilesPanel : UserControl
    {
        // Dependencies
        private IRecentFilesManager? recentFilesManager;
        private readonly string? uiStateDirectory;

        // Debounced save of column widths
        private readonly System.Windows.Forms.Timer saveDebounceTimer = new System.Windows.Forms.Timer { Interval = 300 };

        // Trick to control ListView row height
        private ImageList? rowHeightImageList;
        private const double DefaultRowHeightScale = 1.10;

        /// <summary>
        /// ✅ Parameterless constructor for Designer compatibility.
        /// </summary>
        public RecentFilesPanel()
        {
            // ✅ Designer-generated initialization
            InitializeComponent();

            // ✅ Runtime wiring
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
        /// ✅ Allows late initialization when created by Designer and composed in MainForm.
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

        // ✅ Event handlers wired by Designer
        private void RecentFilesPanelLoad(object? sender, EventArgs e)
        {
            // Ensure initial layout and content
            LoadLayout();
            RefreshList();
        }

        private void txtFilterTextChanged(object? sender, EventArgs e)
        {
            RefreshList();
        }

        private void btnRefreshClick(object? sender, EventArgs e)
        {
            RefreshList();
        }

        // --------------- Initialization helpers ---------------

        /// <summary>
        /// Wire up runtime events and configurations not handled by the Designer.
        /// </summary>
        private void WireRuntime()
        {
            SetupListView();
            ApplyRowHeightScale();
            ApplyThemeDark();
            WireDragDrop();

            // Column resize tracking (for layout persistence)
            if (lvRecentFiles != null)
            {
                lvRecentFiles.ColumnWidthChanged += OnColumnWidthChanged;
            }

            // Debounce timer for layout saves
            saveDebounceTimer.Tick += (_, _) =>
            {
                saveDebounceTimer.Stop();
                SaveLayout();
            };
        }

        private void SetupListView()
        {
            if (lvRecentFiles is null)
                return;

            lvRecentFiles.View = View.Details;
            lvRecentFiles.FullRowSelect = true;
            lvRecentFiles.GridLines = true;
            lvRecentFiles.MultiSelect = true;

            // Add columns if not already defined by Designer
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
            if (lvRecentFiles is null)
                return;

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

        private void ApplyThemeDark()
        {
            // Dark-ish theme (safe if overridden elsewhere)
            var panelBack = Color.FromArgb(32, 32, 32);
            var panelFore = Color.Gainsboro;

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
                btnRefresh.FlatStyle = FlatStyle.System;
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

        // --------------- Layout persistence ---------------

        private void OnColumnWidthChanged(object? sender, ColumnWidthChangedEventArgs e)
        {
            // Debounce: restart timer on each change
            saveDebounceTimer.Stop();
            saveDebounceTimer.Start();
        }

        /// <summary>
        /// Load layout settings (column widths) from disk.
        /// </summary>
        private void LoadLayout()
        {
            if (lvRecentFiles is null)
                return;

            var state = UiStateConfig.Load(uiStateDirectory);
            var widths = state.RecentFilesColumnWidths;

            if (widths is null || widths.Count == 0)
            {
                // First run - leave designer-defined widths
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

        /// <summary>
        /// Save layout settings (column widths) to disk.
        /// </summary>
        private void SaveLayout()
        {
            if (lvRecentFiles is null)
                return;

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

        // --------------- Data binding ---------------

        /// <summary>
        /// Refresh the list of recent files from the manager, applying the current filter.
        /// </summary>
        public void RefreshList()
        {
            if (lvRecentFiles == null)
                return;

            lvRecentFiles.BeginUpdate();
            try
            {
                lvRecentFiles.Items.Clear();

                IEnumerable<RecentFileInfo> items = recentFilesManager?.GetRecentFiles() ?? Array.Empty<RecentFileInfo>();

                // Apply filter
                var filter = txtFilter?.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    items = items.Where(f => f.FileName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                foreach (var f in items)
                {
                    var item = new ListViewItem(f.FileName) { Tag = f };

                    // Columns: File, Type, Size in KB, Generated
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

                if (lblStatus != null)
                {
                    lblStatus.Text = $"{lvRecentFiles.Items.Count} files";
                }
            }
            finally
            {
                lvRecentFiles.EndUpdate();
            }
        }

        // --------------- Drag & Drop (inbound and outbound) ---------------

        private void WireDragDrop()
        {
            if (lvRecentFiles is null)
                return;

            // Inbound
            lvRecentFiles.AllowDrop = true;
            lvRecentFiles.DragEnter += OnListViewDragEnter;
            lvRecentFiles.DragDrop += OnListViewDragDrop;

            // Outbound
            lvRecentFiles.ItemDrag += OnListViewItemDrag;
        }

        // Inbound handlers
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
            if (recentFilesManager is null)
                return;

            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true)
                return;

            try
            {
                var data = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (data is null || data.Length == 0)
                    return;

                var sourceFolders = GetCurrentVectorStoreSourceFolders();
                var added = 0;

                foreach (var path in data)
                {
                    if (string.IsNullOrWhiteSpace(path))
                        continue;

                    if (!File.Exists(path))
                        continue;

                    var type = MapExtensionToType(Path.GetExtension(path));
                    long size = 0;

                    try
                    {
                        size = new FileInfo(path).Length;
                    }
                    catch
                    {
                        size = 0;
                    }

                    // Register and associate with current vector store folders
                    recentFilesManager.RegisterGeneratedFile(
                        filePath: path,
                        fileType: type,
                        sourceFolders: sourceFolders,
                        fileSizeBytes: size,
                        generatedAtUtc: DateTime.UtcNow
                    );
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
                // Be defensive - never crash on bad drops
            }
        }

        // Outbound handler
        private void OnListViewItemDrag(object? sender, ItemDragEventArgs e)
        {
            var filePaths = GetSelectedExistingFilePaths();
            if (filePaths.Length == 0)
                return;

            var sc = new StringCollection();
            sc.AddRange(filePaths);

            var data = new DataObject();
            data.SetFileDropList(sc);

            try
            {
                data.SetData("Preferred DropEffect", DragDropEffects.Copy);
            }
            catch
            {
                // ignore
            }

            DoDragDrop(data, DragDropEffects.Copy);
        }

        private string[] GetSelectedExistingFilePaths()
        {
            if (lvRecentFiles is null)
                return Array.Empty<string>();

            var list = new List<string>();

            foreach (ListViewItem item in lvRecentFiles.SelectedItems)
            {
                if (item.Tag is RecentFileInfo info && info.Exists && !string.IsNullOrWhiteSpace(info.FilePath))
                {
                    list.Add(info.FilePath);
                }
            }

            return list.ToArray();
        }

        private static RecentFileType MapExtensionToType(string? ext)
        {
            var e = (ext ?? string.Empty).Trim().ToLowerInvariant();
            return e switch
            {
                ".docx" => RecentFileType.Docx,
                ".pdf" => RecentFileType.Pdf,
                ".md" or ".markdown" => RecentFileType.Md,
                _ => RecentFileType.Unknown
            };
        }

        /// <summary>
        /// Resolve current vector store folders using LastSelectionService (refactor-safe).
        /// </summary>
        private IReadOnlyList<string> GetCurrentVectorStoreSourceFolders()
        {
            try
            {
                var vsName = new LastSelectionService(uiStateDirectory).GetLastSelectedVectorStore();
                if (string.IsNullOrWhiteSpace(vsName))
                    return Array.Empty<string>();

                // TODO: Wire up actual vector store lookup when available
                // For now, return empty (no source folders)
                return Array.Empty<string>();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        // --------------- Disposal ---------------

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (lvRecentFiles != null)
                    {
                        lvRecentFiles.ColumnWidthChanged -= OnColumnWidthChanged;
                        lvRecentFiles.DragEnter -= OnListViewDragEnter;
                        lvRecentFiles.DragDrop -= OnListViewDragDrop;
                        lvRecentFiles.ItemDrag -= OnListViewItemDrag;
                    }

                    SaveLayout();
                    rowHeightImageList?.Dispose();
                    saveDebounceTimer.Dispose();
                    components?.Dispose();
                }
                catch
                {
                    // Defensive - never crash during Dispose
                }
            }
            base.Dispose(disposing);
        }
    }
}
