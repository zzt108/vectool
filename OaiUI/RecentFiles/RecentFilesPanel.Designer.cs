namespace oaiUI.RecentFiles
{
    partial class RecentFilesPanel
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListView lvRecentFiles;
        private System.Windows.Forms.TextBox txtFilter;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Label lblFilter;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lvRecentFiles = new System.Windows.Forms.ListView();
            this.lblFilter = new System.Windows.Forms.Label();
            this.txtFilter = new System.Windows.Forms.TextBox();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.tableLayoutPanel.SuspendLayout();
            this.SuspendLayout();

            // tableLayoutPanel
            this.tableLayoutPanel.ColumnCount = 3;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel.Controls.Add(this.lblFilter, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.txtFilter, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.btnRefresh, 2, 0);
            this.tableLayoutPanel.Controls.Add(this.lvRecentFiles, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.lblStatus, 0, 2);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.Padding = new System.Windows.Forms.Padding(8);
            this.tableLayoutPanel.RowCount = 3;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            this.tableLayoutPanel.Size = new System.Drawing.Size(800, 600);
            this.tableLayoutPanel.TabIndex = 0;

            // lblFilter
            this.lblFilter.AutoSize = true;
            this.lblFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFilter.Location = new System.Drawing.Point(11, 11);
            this.lblFilter.Margin = new System.Windows.Forms.Padding(3, 3, 8, 3);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Size = new System.Drawing.Size(50, 26);
            this.lblFilter.TabIndex = 0;
            this.lblFilter.Text = "Filter:";
            this.lblFilter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // txtFilter
            this.txtFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtFilter.Location = new System.Drawing.Point(72, 11);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new System.Drawing.Size(600, 23);
            this.txtFilter.TabIndex = 1;
            this.txtFilter.TextChanged += new System.EventHandler(this.txtFilter_TextChanged);

            // btnRefresh
            this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefresh.Location = new System.Drawing.Point(680, 11);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(110, 26);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            // lvRecentFiles
            this.tableLayoutPanel.SetColumnSpan(this.lvRecentFiles, 3);
            this.lvRecentFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvRecentFiles.Location = new System.Drawing.Point(11, 43);
            this.lvRecentFiles.Name = "lvRecentFiles";
            this.lvRecentFiles.Size = new System.Drawing.Size(778, 512);
            this.lvRecentFiles.TabIndex = 3;
            this.lvRecentFiles.UseCompatibleStateImageBehavior = false;

            // lblStatus
            this.tableLayoutPanel.SetColumnSpan(this.lblStatus, 3);
            this.lblStatus.AutoSize = true;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Location = new System.Drawing.Point(11, 561);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(778, 28);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "0 file(s)";

            // RecentFilesPanel
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel);
            this.Name = "RecentFilesPanel";
            this.Size = new System.Drawing.Size(800, 600);
            this.Load += new System.EventHandler(this.RecentFilesPanel_Load);
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
