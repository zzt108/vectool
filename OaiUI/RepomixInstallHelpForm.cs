using System;
using System.Drawing;
using System.Windows.Forms;

namespace Vectool.OaiUI
{
    /// <summary>
    /// Displays installation instructions for Repomix when it's not found on the system.
    /// </summary>
    public sealed class RepomixInstallHelpForm : Form
    {
        private readonly TextBox txtInstructions;
        private readonly Button btnClose;
        private readonly Button btnOpenDocs;

        public RepomixInstallHelpForm()
        {
            // Form properties
            Text = "Repomix Not Found - Installation Help";
            Size = new Size(650, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Instructions text box
            txtInstructions = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10F),
                Dock = DockStyle.Fill,
                Text = GetInstructionsText()
            };

            // Button panel
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10)
            };

            btnClose = new Button
            {
                Text = "Close",
                Width = 100,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            btnClose.Click += (s, e) => Close();

            btnOpenDocs = new Button
            {
                Text = "Open Documentation",
                Width = 160,
                Height = 30
            };
            btnOpenDocs.Click += BtnOpenDocs_Click;

            btnPanel.Controls.Add(btnClose);
            btnPanel.Controls.Add(btnOpenDocs);

            // Layout
            Controls.Add(txtInstructions);
            Controls.Add(btnPanel);

            AcceptButton = btnClose;
        }

        private string GetInstructionsText()
        {
            return @"╔══════════════════════════════════════════════════════════════════════╗
║                 REPOMIX NOT FOUND                                    ║
╚══════════════════════════════════════════════════════════════════════╝

Repomix is required to export your codebase to AI-friendly XML format.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📦 INSTALLATION OPTIONS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Option 1: Use NPX (Recommended - No Installation Required)
----------------------------------------------------------
No installation needed! NPX runs Repomix on-demand.

Prerequisites:
  • Node.js 18+ installed (includes NPX)
  • Download from: https://nodejs.org/

Verify NPX is available:
  > npx --version


Option 2: Global Installation via NPM
--------------------------------------
Install Repomix globally for faster execution:

  > npm install -g repomix

Verify installation:
  > repomix --version


Option 3: Homebrew (macOS/Linux)
---------------------------------
  > brew install repomix


━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔍 TROUBLESHOOTING
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

If npx or repomix commands are not found:

1. Ensure Node.js 18+ is installed:
   > node --version

2. Restart your terminal/command prompt after installation

3. On Windows, ensure Node.js is in your PATH:
   - Search for ""Environment Variables""
   - Add Node.js install directory to PATH

4. Try running VecTool as Administrator (Windows)


━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📚 MORE INFORMATION
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Official Documentation: https://repomix.com/guide
GitHub Repository:      https://github.com/yamadashy/repomix

After installation, restart VecTool and try the export again.
";
        }

        private void BtnOpenDocs_Click(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://repomix.com/guide",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open browser:\n{ex.Message}",
                    "LogError",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.LogError);
            }
        }
    }
}
