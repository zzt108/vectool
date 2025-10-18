using System;
using System.Linq;
using System.Windows.Forms;
using oaiUI.RecentFiles;

namespace Vectool.OaiUI
{
    /// <summary>
    /// Partial MainForm slice: provides a handler to refresh the Recent Files panel after a removal request.
    /// This variant does not assume designer fields exist in this partial; it locates controls at runtime.
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// Handles a removal request coming from Recent Files UI by refreshing the panel.
        /// If a domain removal API is wired in a different partial, it can be invoked there.
        /// </summary>
        /// <param name="path">Absolute path to remove from the recent list.</param>
        private void OnRecentFileRemoveRequested(string path)
        {
            // Optional: call into removal domain service from another partial if available.
            RefreshRecentFilesPanel();
        }

        /// <summary>
        /// Finds the "Recent Files" tab by caption and refreshes the embedded panel if present.
        /// </summary>
        private void RefreshRecentFilesPanel()
        {
            var tabs = Controls.OfType<TabControl>().FirstOrDefault();
            if (tabs == null) return;

            foreach (TabPage tp in tabs.TabPages)
            {
                if (string.Equals(tp.Text, "Recent Files", StringComparison.OrdinalIgnoreCase))
                {
                    var panel = tp.Controls.OfType<RecentFilesPanel>().FirstOrDefault();
                    panel?.RefreshList();
                    break;
                }
            }
        }
    }
}
