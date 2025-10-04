// Path: OaiUI/RecentFiles/RecentFilesPanel.Designer.cs
#nullable enable

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace oaiUI.RecentFiles
{
    [DesignerCategory("Code")]
    partial class RecentFilesPanel
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer? components = null;

        private TableLayoutPanel tableLayoutPanel = null!;
        private Label lblFilter = null!;
        private TextBox txtFilter = null!;
        private Button btnRefresh = null!;
        private ListView lvRecentFiles = null!;
        private Label lblStatus = null!;

        #region Windows Form Designer generated code

        /// <summary>
        /// Method required for Designer support.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.lblFilter = new System.Windows.Forms.Label();
            this.txtFilter = new System.Windows.Forms.TextBox();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.lvRecentFiles = new System.Windows.Forms.ListView();
            this.lblStatus = new System.Windows.Forms.Label();
            this.tableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.ColumnCount = 3;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.tableLayoutPanel.Controls.Add(this.lblFilter, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.txtFilter, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.btnRefresh, 2, 0);
            this.tableLayoutPanel.Controls.Add(this.lvRecentFiles, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.lblStatus, 0, 2);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel.Margin = new System.Windows.Forms.Padding(6);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 3;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(900, 600);
            this.tableLayoutPanel.TabIndex = 0;
            // 
            // lblFilter
            // 
            this.lblFilter.AutoSize = true;
            this.lblFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFilter.Location = new System.Drawing.Point(6, 6);
            this.lblFilter.Margin = new System.Windows.Forms.Padding(6, 6, 0, 6);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Size = new System.Drawing.Size(42, 23);
            this.lblFilter.TabIndex = 0;
            this.lblFilter.Text = "Filter";
            this.lblFilter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtFilter
            // 
            this.txtFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtFilter.Location = new System.Drawing.Point(54, 6);
            this.txtFilter.Margin = new System.Windows.Forms.Padding(6);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.PlaceholderText = "Type to filter recent files...";
            this.txtFilter.Size = new System.Drawing.Size(744, 23);
            this.txtFilter.TabIndex = 1;
            this.txtFilter.TextChanged += new System.EventHandler(this.txtFilterTextChanged);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnRefresh.Location = new System.Drawing.Point(804, 6);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(6);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(90, 23);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefreshClick);
            // 
            // lvRecentFiles
            // 
            this.tableLayoutPanel.SetColumnSpan(this.lvRecentFiles, 3);
            this.lvRecentFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvRecentFiles.Location = new System.Drawing.Point(6, 41);
            this.lvRecentFiles.Margin = new System.Windows.Forms.Padding(6);
            this.lvRecentFiles.Name = "lvRecentFiles";
            this.lvRecentFiles.Size = new System.Drawing.Size(888, 523);
            this.lvRecentFiles.TabIndex = 3;
            this.lvRecentFiles.UseCompatibleStateImageBehavior = false;
            this.lvRecentFiles.View = System.Windows.Forms.View.Details;
            this.lvRecentFiles.FullRowSelect = true;
            this.lvRecentFiles.GridLines = true;
            this.lvRecentFiles.MultiSelect = true;
            // 
            // lblStatus
            // 
            this.tableLayoutPanel.SetColumnSpan(this.lblStatus, 3);
            this.lblStatus.AutoSize = true;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Location = new System.Drawing.Point(6, 576);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(6);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(888, 18);
            this.lblStatus.TabIndex = 4;
            this.lblStatus.Text = "0 files";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // RecentFilesPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel);
            this.Margin = new System.Windows.Forms.Padding(6);
            this.Name = "RecentFilesPanel";
            this.Size = new System.Drawing.Size(900, 600);
            this.Load += new System.EventHandler(this.RecentFilesPanelLoad);
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
