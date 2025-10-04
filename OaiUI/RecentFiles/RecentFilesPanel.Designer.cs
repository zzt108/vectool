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
            tableLayoutPanel = new TableLayoutPanel();
            lblFilter = new Label();
            txtFilter = new TextBox();
            btnRefresh = new Button();
            lvRecentFiles = new ListView();
            lblStatus = new Label();
            tableLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.ColumnCount = 3;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel.Controls.Add(lblFilter, 0, 0);
            tableLayoutPanel.Controls.Add(txtFilter, 1, 0);
            tableLayoutPanel.Controls.Add(btnRefresh, 2, 0);
            tableLayoutPanel.Controls.Add(lvRecentFiles, 0, 1);
            tableLayoutPanel.Controls.Add(lblStatus, 0, 2);
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Location = new Point(0, 0);
            tableLayoutPanel.Margin = new Padding(6, 6, 6, 6);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.Padding = new Padding(15, 17, 15, 17);
            tableLayoutPanel.RowCount = 3;
            tableLayoutPanel.RowStyles.Add(new RowStyle());
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new RowStyle());
            tableLayoutPanel.Size = new Size(1486, 1280);
            tableLayoutPanel.TabIndex = 0;
            // 
            // lblFilter
            // 
            lblFilter.AutoSize = true;
            lblFilter.Dock = DockStyle.Fill;
            lblFilter.Location = new Point(21, 23);
            lblFilter.Margin = new Padding(6, 6, 15, 6);
            lblFilter.Name = "lblFilter";
            lblFilter.Size = new Size(72, 83);
            lblFilter.TabIndex = 0;
            lblFilter.Text = "Filter:";
            lblFilter.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // txtFilter
            // 
            txtFilter.Dock = DockStyle.Fill;
            txtFilter.Location = new Point(114, 23);
            txtFilter.Margin = new Padding(6, 6, 6, 6);
            txtFilter.Name = "txtFilter";
            txtFilter.Size = new Size(1135, 39);
            txtFilter.TabIndex = 1;
            txtFilter.TextChanged += txtFilter_TextChanged;
            // 
            // btnRefresh
            // 
            btnRefresh.Dock = DockStyle.Fill;
            btnRefresh.Location = new Point(1261, 23);
            btnRefresh.Margin = new Padding(6, 6, 6, 6);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(204, 83);
            btnRefresh.TabIndex = 2;
            btnRefresh.Text = "Refresh";
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // lvRecentFiles
            // 
            tableLayoutPanel.SetColumnSpan(lvRecentFiles, 3);
            lvRecentFiles.Dock = DockStyle.Fill;
            lvRecentFiles.Location = new Point(21, 118);
            lvRecentFiles.Margin = new Padding(6, 6, 6, 6);
            lvRecentFiles.Name = "lvRecentFiles";
            lvRecentFiles.Size = new Size(1444, 1088);
            lvRecentFiles.TabIndex = 3;
            lvRecentFiles.UseCompatibleStateImageBehavior = false;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            tableLayoutPanel.SetColumnSpan(lblStatus, 3);
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Location = new Point(21, 1225);
            lblStatus.Margin = new Padding(6, 13, 6, 6);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(1444, 32);
            lblStatus.TabIndex = 4;
            lblStatus.Text = "0 file(s)";
            // 
            // RecentFilesPanel
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tableLayoutPanel);
            Margin = new Padding(6, 6, 6, 6);
            Name = "RecentFilesPanel";
            Size = new Size(1486, 1280);
            Load += RecentFilesPanel_Load;
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel.PerformLayout();
            ResumeLayout(false);
        }
    }
}
