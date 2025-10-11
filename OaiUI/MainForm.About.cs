using System;
using System.Windows.Forms;

namespace VecTool.OaiUI
{
    public partial class MainForm : Form
    {
        private void aboutToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            using var dlg = new AboutForm();
            dlg.ShowDialog(this);
        }
    }
}
