// ✅ NEW FILE
#nullable enable
using System;
using System.Windows.Forms;
using VecTool.Configuration;
using VecTool.RecentFiles;

namespace oaiUI.RecentFiles
{
    /// <summary>
    /// RecentFilesPanel partial: Core initialization, fields, constructors, DI.
    /// </summary>
    public sealed partial class RecentFilesPanel : UserControl
    {
        // ==================== Dependencies ====================
        private IRecentFilesManager? recentFilesManager;
        private readonly string? uiStateDirectory;

        // ==================== Internal State ====================
        private readonly System.Windows.Forms.Timer saveDebounceTimer = new() { Interval = 300 };
        private ImageList? rowHeightImageList;
        private const double DefaultRowHeightScale = 1.10;

        // ==================== Constructors ====================

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

        // ==================== Initialization Helpers ====================

        /// <summary>
        /// Wire up runtime events and configurations not handled by the Designer.
        /// </summary>
        private void WireRuntime()
        {
            SetupListView();
            ApplyRowHeightScale();
            ApplyThemeDark();
            WireDragDrop();

            // Column resize tracking for layout persistence
            if (lvRecentFiles != null)
                lvRecentFiles.ColumnWidthChanged += OnColumnWidthChanged;

            // Debounce timer for layout saves
            saveDebounceTimer.Tick += (_, _) => { saveDebounceTimer.Stop(); SaveLayout(); };
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
            rowHeightImageList = new ImageList { ImageSize = new System.Drawing.Size(1, Math.Max(desired, 18)) };
            lvRecentFiles.SmallImageList = rowHeightImageList;
        }

        private void ApplyThemeDark()
        {
            // Dark-ish theme (safe if overridden elsewhere)
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

        // ==================== Disposal ====================

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
