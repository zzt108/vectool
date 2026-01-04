#nullable enable
namespace oaiUI.RecentFiles
{
    /// <summary>
    /// RecentFilesPanel partial: Event handlers for UI controls.
    /// </summary>
    public sealed partial class RecentFilesPanel : UserControl
    {
        // ==================== Event Handlers (Wired by Designer) ====================

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
    }
}
