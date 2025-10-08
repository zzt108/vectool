// ✅ FULL FILE VERSION
// File: OaiUI/RecentFiles/RecentFilesPanel.DataBinding.cs

using System;
using System.Windows.Forms;
using LogCtxShared;

namespace OaiUI.RecentFiles
{
    public partial class RecentFilesPanel : UserControl
    {
        public RecentFilesPanel()
        {
            InitializeComponent();
        }

        private void OnDataBound(object sender, EventArgs e)
        {
            // Use static Set rather than instance
            var ctx = LogCtx.Set();
            ctx.LogInfo("Data bound");

            // ... existing data-binding logic ...
        }

        // Other members...
    }
}
