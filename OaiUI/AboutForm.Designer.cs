using System.Windows.Forms;

namespace Vectool.UI
{
    partial class AboutForm
    {
        private System.ComponentModel.IContainer components = null!;
        private Label lblTitle = null!;
        private Label lblInformational = null!;
        private Label lblFileVersion = null!;
        private Label lblAssemblyVersion = null!;
        private Label lblBuild = null!;
        private Label lblCommit = null!;
        private Button btnClose = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.lblTitle = new Label();
            this.lblInformational = new Label();
            this.lblFileVersion = new Label();
            this.lblAssemblyVersion = new Label();
            this.lblBuild = new Label();
            this.lblCommit = new Label();
            this.btnClose = new Button();

            this.SuspendLayout();

            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(12, 9);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(72, 21);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "VecTool";

            // lblInformational
            this.lblInformational.AutoSize = true;
            this.lblInformational.Location = new System.Drawing.Point(14, 45);
            this.lblInformational.Name = "lblInformational";
            this.lblInformational.Size = new System.Drawing.Size(121, 15);
            this.lblInformational.TabIndex = 1;
            this.lblInformational.Text = "Display: <informational>";

            // lblFileVersion
            this.lblFileVersion.AutoSize = true;
            this.lblFileVersion.Location = new System.Drawing.Point(14, 70);
            this.lblFileVersion.Name = "lblFileVersion";
            this.lblFileVersion.Size = new System.Drawing.Size(94, 15);
            this.lblFileVersion.TabIndex = 2;
            this.lblFileVersion.Text = "File: <fileversion>";

            // lblAssemblyVersion
            this.lblAssemblyVersion.AutoSize = true;
            this.lblAssemblyVersion.Location = new System.Drawing.Point(14, 95);
            this.lblAssemblyVersion.Name = "lblAssemblyVersion";
            this.lblAssemblyVersion.Size = new System.Drawing.Size(131, 15);
            this.lblAssemblyVersion.TabIndex = 3;
            this.lblAssemblyVersion.Text = "Assembly: <asmversion>";

            // lblBuild
            this.lblBuild.AutoSize = true;
            this.lblBuild.Location = new System.Drawing.Point(14, 120);
            this.lblBuild.Name = "lblBuild";
            this.lblBuild.Size = new System.Drawing.Size(102, 15);
            this.lblBuild.TabIndex = 4;
            this.lblBuild.Text = "Build: <timestamp>";

            // lblCommit
            this.lblCommit.AutoSize = true;
            this.lblCommit.Location = new System.Drawing.Point(14, 145);
            this.lblCommit.Name = "lblCommit";
            this.lblCommit.Size = new System.Drawing.Size(96, 15);
            this.lblCommit.TabIndex = 5;
            this.lblCommit.Text = "Commit: <short>";

            // btnClose
            this.btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnClose.Text = "Close";
            this.btnClose.Location = new System.Drawing.Point(355, 175);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(90, 28);
            this.btnClose.TabIndex = 6;
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += (_, __) => this.Close();

            // AboutForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(460, 215);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblInformational);
            this.Controls.Add(this.lblFileVersion);
            this.Controls.Add(this.lblAssemblyVersion);
            this.Controls.Add(this.lblBuild);
            this.Controls.Add(this.lblCommit);
            this.Controls.Add(this.btnClose);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "About VecTool";
            this.ShowInTaskbar = false;

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
