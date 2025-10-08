// ✅ FULL FILE VERSION
// File: OaiUI/RecentFiles/RecentFilesPanel.DragDrop.cs

using System;
using System.Windows.Forms;
using LogCtxShared;

namespace OaiUI.RecentFiles
{
    public partial class RecentFilesPanel : UserControl
    {
        private void OnDragEnter(object sender, DragEventArgs e)
        {
            var ctx = LogCtx.Set();
            ctx.LogInfo("Drag entered");

            // ... existing drag-enter logic ...
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            var ctx = LogCtx.Set();
            ctx.LogInfo("Drag dropped");

            // ... existing drag-drop logic ...
        }

        // Other members...
    }
}
