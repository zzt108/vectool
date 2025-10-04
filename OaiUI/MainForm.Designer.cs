namespace Vectool.OaiUI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.actionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.convertToMdToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.getGitChangesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileSizeSummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.runTestsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageMain = new System.Windows.Forms.TabPage();
            this.lblVectorStoreManagement = new System.Windows.Forms.Label();
            this.comboBoxVectorStores = new System.Windows.Forms.ComboBox();
            this.txtNewVectorStoreName = new System.Windows.Forms.TextBox();
            this.btnCreateNewVectorStore = new System.Windows.Forms.Button();
            this.btnSelectFolders = new System.Windows.Forms.Button();
            this.listBoxSelectedFolders = new System.Windows.Forms.ListBox();
            this.tabPageSettings = new System.Windows.Forms.TabPage();
            this.btnResetVsSettings = new System.Windows.Forms.Button();
            this.btnSaveVsSettings = new System.Windows.Forms.Button();
            this.chkInheritExcludedFolders = new System.Windows.Forms.CheckBox();
            this.chkInheritExcludedFiles = new System.Windows.Forms.CheckBox();
            this.txtExcludedFolders = new System.Windows.Forms.TextBox();
            this.txtExcludedFiles = new System.Windows.Forms.TextBox();
            this.cmbSettingsVectorStore = new System.Windows.Forms.ComboBox();
            this.lblSettingsVectorStore = new System.Windows.Forms.Label();
            this.tabPageRecentFiles = new System.Windows.Forms.TabPage();
            this.recentFilesPanel = new oaiUI.RecentFiles.RecentFilesPanel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageMain.SuspendLayout();
            this.tabPageSettings.SuspendLayout();
            this.tabPageRecentFiles.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.actionsToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1200, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            // 
            // actionsToolStripMenuItem
            // 
            this.actionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.convertToMdToolStripMenuItem,
            this.toolStripSeparator1,
            this.getGitChangesToolStripMenuItem,
            this.fileSizeSummaryToolStripMenuItem,
            this.runTestsToolStripMenuItem});
            this.actionsToolStripMenuItem.Name = "actionsToolStripMenuItem";
            this.actionsToolStripMenuItem.Size = new System.Drawing.Size(59, 20);
            this.actionsToolStripMenuItem.Text = "Actions";
            // 
            // convertToMdToolStripMenuItem
            // 
            this.convertToMdToolStripMenuItem.Name = "convertToMdToolStripMenuItem";
            this.convertToMdToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.M)));
            this.convertToMdToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            this.convertToMdToolStripMenuItem.Text = "Convert to MD";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(217, 6);
            // 
            // getGitChangesToolStripMenuItem
            // 
            this.getGitChangesToolStripMenuItem.Name = "getGitChangesToolStripMenuItem";
            this.getGitChangesToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G)));
            this.getGitChangesToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            this.getGitChangesToolStripMenuItem.Text = "Get Git Changes";
            // 
            // fileSizeSummaryToolStripMenuItem
            // 
            this.fileSizeSummaryToolStripMenuItem.Name = "fileSizeSummaryToolStripMenuItem";
            this.fileSizeSummaryToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
            this.fileSizeSummaryToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            this.fileSizeSummaryToolStripMenuItem.Text = "File Size Summary";
            // 
            // runTestsToolStripMenuItem
            // 
            this.runTestsToolStripMenuItem.Name = "runTestsToolStripMenuItem";
            this.runTestsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
            this.runTestsToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            this.runTestsToolStripMenuItem.Text = "Run Tests";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPageMain);
            this.tabControl1.Controls.Add(this.tabPageSettings);
            this.tabControl1.Controls.Add(this.tabPageRecentFiles);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 24);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1200, 622);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPageMain
            // 
            this.tabPageMain.Controls.Add(this.lblVectorStoreManagement);
            this.tabPageMain.Controls.Add(this.comboBoxVectorStores);
            this.tabPageMain.Controls.Add(this.txtNewVectorStoreName);
            this.tabPageMain.Controls.Add(this.btnCreateNewVectorStore);
            this.tabPageMain.Controls.Add(this.btnSelectFolders);
            this.tabPageMain.Controls.Add(this.listBoxSelectedFolders);
            this.tabPageMain.Location = new System.Drawing.Point(4, 24);
            this.tabPageMain.Name = "tabPageMain";
            this.tabPageMain.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageMain.Size = new System.Drawing.Size(1192, 594);
            this.tabPageMain.TabIndex = 0;
            this.tabPageMain.Text = "Main";
            this.tabPageMain.UseVisualStyleBackColor = true;
            // 
            // lblVectorStoreManagement
            // 
            this.lblVectorStoreManagement.AutoSize = true;
            this.lblVectorStoreManagement.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblVectorStoreManagement.Location = new System.Drawing.Point(6, 10);
            this.lblVectorStoreManagement.Name = "lblVectorStoreManagement";
            this.lblVectorStoreManagement.Size = new System.Drawing.Size(196, 21);
            this.lblVectorStoreManagement.TabIndex = 5;
            this.lblVectorStoreManagement.Text = "Vector Store Management";
            // 
            // comboBoxVectorStores
            // 
            this.comboBoxVectorStores.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxVectorStores.FormattingEnabled = true;
            this.comboBoxVectorStores.Location = new System.Drawing.Point(10, 40);
            this.comboBoxVectorStores.Name = "comboBoxVectorStores";
            this.comboBoxVectorStores.Size = new System.Drawing.Size(300, 23);
            this.comboBoxVectorStores.TabIndex = 0;
            // 
            // txtNewVectorStoreName
            // 
            this.txtNewVectorStoreName.Location = new System.Drawing.Point(320, 40);
            this.txtNewVectorStoreName.Name = "txtNewVectorStoreName";
            this.txtNewVectorStoreName.PlaceholderText = "New vector store name";
            this.txtNewVectorStoreName.Size = new System.Drawing.Size(200, 23);
            this.txtNewVectorStoreName.TabIndex = 1;
            // 
            // btnCreateNewVectorStore
            // 
            this.btnCreateNewVectorStore.Location = new System.Drawing.Point(530, 39);
            this.btnCreateNewVectorStore.Name = "btnCreateNewVectorStore";
            this.btnCreateNewVectorStore.Size = new System.Drawing.Size(150, 25);
            this.btnCreateNewVectorStore.TabIndex = 2;
            this.btnCreateNewVectorStore.Text = "Create New";
            this.btnCreateNewVectorStore.UseVisualStyleBackColor = true;
            // 
            // btnSelectFolders
            // 
            this.btnSelectFolders.Location = new System.Drawing.Point(10, 80);
            this.btnSelectFolders.Name = "btnSelectFolders";
            this.btnSelectFolders.Size = new System.Drawing.Size(150, 30);
            this.btnSelectFolders.TabIndex = 3;
            this.btnSelectFolders.Text = "Select Folders";
            this.btnSelectFolders.UseVisualStyleBackColor = true;
            // 
            // listBoxSelectedFolders
            // 
            this.listBoxSelectedFolders.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listBoxSelectedFolders.FormattingEnabled = true;
            this.listBoxSelectedFolders.ItemHeight = 15;
            this.listBoxSelectedFolders.Location = new System.Drawing.Point(10, 120);
            this.listBoxSelectedFolders.Name = "listBoxSelectedFolders";
            this.listBoxSelectedFolders.Size = new System.Drawing.Size(1174, 454);
            this.listBoxSelectedFolders.TabIndex = 4;
            // 
            // tabPageSettings
            // 
            this.tabPageSettings.Controls.Add(this.btnResetVsSettings);
            this.tabPageSettings.Controls.Add(this.btnSaveVsSettings);
            this.tabPageSettings.Controls.Add(this.chkInheritExcludedFolders);
            this.tabPageSettings.Controls.Add(this.chkInheritExcludedFiles);
            this.tabPageSettings.Controls.Add(this.txtExcludedFolders);
            this.tabPageSettings.Controls.Add(this.txtExcludedFiles);
            this.tabPageSettings.Controls.Add(this.cmbSettingsVectorStore);
            this.tabPageSettings.Controls.Add(this.lblSettingsVectorStore);
            this.tabPageSettings.Location = new System.Drawing.Point(4, 24);
            this.tabPageSettings.Name = "tabPageSettings";
            this.tabPageSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSettings.Size = new System.Drawing.Size(1192, 594);
            this.tabPageSettings.TabIndex = 1;
            this.tabPageSettings.Text = "Settings";
            this.tabPageSettings.UseVisualStyleBackColor = true;
            // 
            // lblSettingsVectorStore
            // 
            this.lblSettingsVectorStore.AutoSize = true;
            this.lblSettingsVectorStore.Location = new System.Drawing.Point(10, 15);
            this.lblSettingsVectorStore.Name = "lblSettingsVectorStore";
            this.lblSettingsVectorStore.Size = new System.Drawing.Size(75, 15);
            this.lblSettingsVectorStore.TabIndex = 0;
            this.lblSettingsVectorStore.Text = "Vector Store:";
            // 
            // cmbSettingsVectorStore
            // 
            this.cmbSettingsVectorStore.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSettingsVectorStore.FormattingEnabled = true;
            this.cmbSettingsVectorStore.Location = new System.Drawing.Point(10, 35);
            this.cmbSettingsVectorStore.Name = "cmbSettingsVectorStore";
            this.cmbSettingsVectorStore.Size = new System.Drawing.Size(400, 23);
            this.cmbSettingsVectorStore.TabIndex = 1;
            // 
            // chkInheritExcludedFiles
            // 
            this.chkInheritExcludedFiles.AutoSize = true;
            this.chkInheritExcludedFiles.Checked = true;
            this.chkInheritExcludedFiles.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkInheritExcludedFiles.Location = new System.Drawing.Point(10, 75);
            this.chkInheritExcludedFiles.Name = "chkInheritExcludedFiles";
            this.chkInheritExcludedFiles.Size = new System.Drawing.Size(202, 19);
            this.chkInheritExcludedFiles.TabIndex = 2;
            this.chkInheritExcludedFiles.Text = "Inherit Excluded Files from Global";
            this.chkInheritExcludedFiles.UseVisualStyleBackColor = true;
            // 
            // txtExcludedFiles
            // 
            this.txtExcludedFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtExcludedFiles.Enabled = false;
            this.txtExcludedFiles.Location = new System.Drawing.Point(10, 100);
            this.txtExcludedFiles.Multiline = true;
            this.txtExcludedFiles.Name = "txtExcludedFiles";
            this.txtExcludedFiles.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtExcludedFiles.Size = new System.Drawing.Size(1174, 120);
            this.txtExcludedFiles.TabIndex = 3;
            // 
            // chkInheritExcludedFolders
            // 
            this.chkInheritExcludedFolders.AutoSize = true;
            this.chkInheritExcludedFolders.Checked = true;
            this.chkInheritExcludedFolders.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkInheritExcludedFolders.Location = new System.Drawing.Point(10, 235);
            this.chkInheritExcludedFolders.Name = "chkInheritExcludedFolders";
            this.chkInheritExcludedFolders.Size = new System.Drawing.Size(218, 19);
            this.chkInheritExcludedFolders.TabIndex = 4;
            this.chkInheritExcludedFolders.Text = "Inherit Excluded Folders from Global";
            this.chkInheritExcludedFolders.UseVisualStyleBackColor = true;
            // 
            // txtExcludedFolders
            // 
            this.txtExcludedFolders.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtExcludedFolders.Enabled = false;
            this.txtExcludedFolders.Location = new System.Drawing.Point(10, 260);
            this.txtExcludedFolders.Multiline = true;
            this.txtExcludedFolders.Name = "txtExcludedFolders";
            this.txtExcludedFolders.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtExcludedFolders.Size = new System.Drawing.Size(1174, 250);
            this.txtExcludedFolders.TabIndex = 5;
            // 
            // btnSaveVsSettings
            // 
            this.btnSaveVsSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSaveVsSettings.Location = new System.Drawing.Point(989, 530);
            this.btnSaveVsSettings.Name = "btnSaveVsSettings";
            this.btnSaveVsSettings.Size = new System.Drawing.Size(90, 30);
            this.btnSaveVsSettings.TabIndex = 6;
            this.btnSaveVsSettings.Text = "Save";
            this.btnSaveVsSettings.UseVisualStyleBackColor = true;
            // 
            // btnResetVsSettings
            // 
            this.btnResetVsSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnResetVsSettings.Location = new System.Drawing.Point(1094, 530);
            this.btnResetVsSettings.Name = "btnResetVsSettings";
            this.btnResetVsSettings.Size = new System.Drawing.Size(90, 30);
            this.btnResetVsSettings.TabIndex = 7;
            this.btnResetVsSettings.Text = "Reset";
            this.btnResetVsSettings.UseVisualStyleBackColor = true;
            // 
            // tabPageRecentFiles
            // 
            this.tabPageRecentFiles.Controls.Add(this.recentFilesPanel);
            this.tabPageRecentFiles.Location = new System.Drawing.Point(4, 24);
            this.tabPageRecentFiles.Name = "tabPageRecentFiles";
            this.tabPageRecentFiles.Size = new System.Drawing.Size(1192, 594);
            this.tabPageRecentFiles.TabIndex = 2;
            this.tabPageRecentFiles.Text = "Recent Files";
            this.tabPageRecentFiles.UseVisualStyleBackColor = true;
            // 
            // recentFilesPanel
            // 
            this.recentFilesPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.recentFilesPanel.Location = new System.Drawing.Point(0, 0);
            this.recentFilesPanel.Name = "recentFilesPanel";
            this.recentFilesPanel.Size = new System.Drawing.Size(1192, 594);
            this.recentFilesPanel.TabIndex = 0;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel,
            this.progressBar});
            this.statusStrip1.Location = new System.Drawing.Point(0, 646);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1200, 26);
            this.statusStrip1.TabIndex = 2;
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(39, 21);
            this.statusLabel.Text = "Ready";
            // 
            // progressBar
            // 
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(200, 20);
            this.progressBar.Visible = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 672);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "VecTool";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPageMain.ResumeLayout(false);
            this.tabPageMain.PerformLayout();
            this.tabPageSettings.ResumeLayout(false);
            this.tabPageSettings.PerformLayout();
            this.tabPageRecentFiles.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem actionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem convertToMdToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem getGitChangesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileSizeSummaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem runTestsToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageMain;
        private System.Windows.Forms.Label lblVectorStoreManagement;
        private System.Windows.Forms.ComboBox comboBoxVectorStores;
        private System.Windows.Forms.TextBox txtNewVectorStoreName;
        private System.Windows.Forms.Button btnCreateNewVectorStore;
        private System.Windows.Forms.Button btnSelectFolders;
        private System.Windows.Forms.ListBox listBoxSelectedFolders;
        private System.Windows.Forms.TabPage tabPageSettings;
        private System.Windows.Forms.Button btnResetVsSettings;
        private System.Windows.Forms.Button btnSaveVsSettings;
        private System.Windows.Forms.CheckBox chkInheritExcludedFolders;
        private System.Windows.Forms.CheckBox chkInheritExcludedFiles;
        private System.Windows.Forms.TextBox txtExcludedFolders;
        private System.Windows.Forms.TextBox txtExcludedFiles;
        private System.Windows.Forms.ComboBox cmbSettingsVectorStore;
        private System.Windows.Forms.Label lblSettingsVectorStore;
        private System.Windows.Forms.TabPage tabPageRecentFiles;
        private oaiUI.RecentFiles.RecentFilesPanel recentFilesPanel;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
    }
}
