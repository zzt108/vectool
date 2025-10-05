// Path: OaiUI/MainForm.LastSelection.cs
// Description: Partial MainForm class focusing on "Recent Files" last-selection/filter state.

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Vectool.UI.RecentFiles;
using VecTool.Configuration;
using VecTool.Core.RecentFiles;

namespace Vectool.OaiUI
{
    /// <summary>
    /// Partial MainForm implementation managing the Recent Files tab state:
    /// - Loads persisted filter (All/Linked/Unlinked/Specific) and specific store id.
    /// - Binds filtered items via IRecentFilesService.
    /// - Persists the last selected recent file path.
    /// </summary>
    public partial class MainForm : Form
    {
        // Backing fields for UI state and data source.
        private readonly UiStateConfig uiState;
        private readonly IRecentFilesService recentFilesService;

        // Lightweight UI control for the Recent Files tab; event-driven binding.
        private readonly RecentFilesControl recentFilesControl;

        /// <summary>
        /// Overloaded constructor allowing DI in tests or composition roots.
        /// The parameterless constructor remains in other partial definitions.
        /// </summary>
        /// <param name="uiState">Persisted UI state provider.</param>
        /// <param name="recentFilesService">Service providing filtered recent files.</param>
        public MainForm(UiStateConfig uiState, IRecentFilesService recentFilesService)
        {
            this.uiState = uiState ?? throw new ArgumentNullException(nameof(uiState));
            this.recentFilesService = recentFilesService ?? throw new ArgumentNullException(nameof(recentFilesService));

            // Initialize control and wire events prior to InitializeComponent so handlers exist.
            recentFilesControl = new RecentFilesControl();
            recentFilesControl.VectorFilterChanged += OnVectorFilterChanged;
            recentFilesControl.FileSelected += OnRecentFileSelected;

            // Initialize the WinForms designer components for this Form.
            InitializeComponent();

            // Replace the designer-supplied surface with our control, docked.
            // If a designer-added panel exists, it's cleared here to avoid overlap.
            if (TryGetRecentFilesTab(out var tab))
            {
                tab.Controls.Clear();
                recentFilesControl.Dock = DockStyle.Fill;
                tab.Controls.Add(recentFilesControl);
            }

            // Apply persisted filter and bind initial data.
            InitializeRecentFilesTab();
        }

        /// <summary>
        /// Initializes the Recent Files tab from persisted UiState and binds data.
        /// </summary>
        private void InitializeRecentFilesTab()
        {
            var filter = uiState.GetRecentFilesFilter();
            var storeId = uiState.GetRecentFilesSpecificStoreId();

            recentFilesControl.SetFilter(filter, storeId);

            var items = recentFilesService.GetRecentFiles(filter, storeId);
            recentFilesControl.Bind(items);
        }

        /// <summary>
        /// Handles user filter changes, persists them, and refreshes the grid.
        /// </summary>
        private void OnVectorFilterChanged(VectorStoreLinkFilter filter, string? storeId)
        {
            uiState.SetRecentFilesFilter(filter);
            uiState.SetRecentFilesSpecificStoreId(storeId);

            var items = recentFilesService.GetRecentFiles(filter, storeId);
            recentFilesControl.Bind(items);
        }

        /// <summary>
        /// Persists the last selected recent file path for UX continuity.
        /// </summary>
        private void OnRecentFileSelected(string path)
        {
            // Null/empty handled in UiStateConfig; no throw on whitespace.
            uiState.SetLastSelectedRecentFilePath(path);

            // Optional: open or navigate to the file could be implemented here.
            // e.g., Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        /// <summary>
        /// Attempts to locate the "Recent Files" tab page by its known field name or text.
        /// </summary>
        private bool TryGetRecentFilesTab(out TabPage tabPage)
        {
            // Designer-named field is typically "tabPageRecentFiles".
            // If not available, try by Text caption fallback.
            tabPage = null!;

            try
            {
                // Access via generated field if present on this partial form instance.
                var field = GetType().GetField("tabPageRecentFiles",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (field?.GetValue(this) is TabPage fieldTab)
                {
                    tabPage = fieldTab;
                    return true;
                }
            }
            catch
            {
                // Fall through to caption-based lookup.
            }

            // Fallback by caption search on a known TabControl, if present.
            try
            {
                var tabControlField = GetType().GetField("tabControl1",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (tabControlField?.GetValue(this) is TabControl tc)
                {
                    foreach (TabPage page in tc.TabPages)
                    {
                        if (string.Equals(page.Text, "Recent Files", StringComparison.OrdinalIgnoreCase))
                        {
                            tabPage = page;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Ignore; return false if not found.
            }

            return false;
        }
    }
}
