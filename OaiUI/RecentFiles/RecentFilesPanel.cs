#nullable enable

using NLogShared;
using System;
using System.Windows.Forms;
using VecTool.RecentFiles;

namespace oaiUI.RecentFiles
{
    // Main partial: fields, constructors, runtime wiring, and disposal-safe cleanup.
    public partial class RecentFilesPanel : UserControl
    {
        // Defaults and fields kept in main partial to avoid duplication across partials
        private const double DefaultRowHeightScale = 1.0;

        private IRecentFilesManager? recentFilesManager;
        private readonly string? uiStateDirectory;
        private readonly System.Windows.Forms.Timer saveDebounceTimer = new ();
        private ImageList? rowHeightImageList;

        // Parameterless constructor for Designer support
        public RecentFilesPanel()
        {
            InitializeComponent();
            InitializeRuntime();
        }

        // DI-friendly constructor for tests and production
        public RecentFilesPanel(IRecentFilesManager recentFilesManager, string? uiStateDirectory = null)
        {
            this.recentFilesManager = recentFilesManager ?? throw new ArgumentNullException(nameof(recentFilesManager));
            this.uiStateDirectory = uiStateDirectory;

            InitializeComponent();
            InitializeRuntime();
        }

        // Allow late initialization when created by Designer and composed in MainForm
        public void Initialize(IRecentFilesManager manager)
        {
            recentFilesManager = manager ?? throw new ArgumentNullException(nameof(manager));
            LoadLayout();
            RefreshList();
        }

        // Exposed for tests
        internal void SaveLayoutForTesting() => SaveLayout();

        // Designer wires: this.Load += RecentFilesPanelLoad
        //private void RecentFilesPanelLoad(object? sender, EventArgs e)
        //{
        //    // Ensure initial layout and content
        //    LoadLayout();
        //    RefreshList();
        //}

        // Optional setter for late binding of the manager in integration contexts
        public void SetRecentFilesManager(IRecentFilesManager manager)
        {
            recentFilesManager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        // Centralized runtime wiring that is safe with Designer-generated subscriptions
        private void InitializeRuntime()
        {
            // Debounce column width saves; partial handles OnColumnWidthChanged -> timer restart
            saveDebounceTimer.Interval = 300;
            saveDebounceTimer.Tick += (_, __) =>
            {
                try
                {
                    SaveLayout();
                }
                catch (Exception ex)
                {
                    //LogCtx.Log.With("component", "RecentFilesPanel")
                    //          .With("area", "Init")
                    //          .With("err", ex)
                    //          .Error("Error during debounced SaveLayout.");
                }
                finally
                {
                    saveDebounceTimer.Stop();
                }
            };

            // Improve perceived UI smoothness
            try
            {
                // This reduces flicker for ListView on resize/refresh in many WinForms setups
                typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                               ?.SetValue(lvRecentFiles, true, null);
            }
            catch
            {
                // Non-fatal if reflection fails
            }

            // Setup ListView scaffolding and DnD once at runtime start
            SetupListView();
            WireDragDrop();
            ApplyThemeDark();

            // Ensure cleanup on control disposal
            Disposed += (_, __) =>
            {
                try
                {
                    // Unhook events we own; Designer wiring (Load) remains in the .Designer.cs
                    if (lvRecentFiles != null)
                    {
                        lvRecentFiles.ItemDrag -= OnListViewItemDrag;
                        lvRecentFiles.DragEnter -= OnListViewDragEnter;
                        lvRecentFiles.DragDrop -= OnListViewDragDrop;
                        lvRecentFiles.ColumnWidthChanged -= OnColumnWidthChanged;
                        lvRecentFiles.SmallImageList = null;
                    }

                    rowHeightImageList?.Dispose();
                    rowHeightImageList = null;

                    saveDebounceTimer.Stop();
                    saveDebounceTimer.Dispose();
                }
                catch (Exception ex)
                {
                    //LogCtx.Log.With("component", "RecentFilesPanel")
                    //          .With("area", "Dispose")
                    //          .With("err", ex)
                    //          .Warn("Non-fatal error during RecentFilesPanel disposal.");
                }
            };

            //LogCtx.Log.With("component", "RecentFilesPanel")
            //          .With("area", "Init")
            //          .Info("Runtime initialization completed.");
        }
    }
}
