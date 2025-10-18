// Path: OaiUI/MainForm.Designer.cs

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Vectool.OaiUI
{
    partial class MainForm
    {
        // ✅ Designer field declarations - ALL controls must be here
        private System.ComponentModel.IContainer components = null;

        // Top menu
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem actionsToolStripMenuItem;
        private ToolStripMenuItem convertToMdToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem getGitChangesToolStripMenuItem;
        private ToolStripMenuItem fileSizeSummaryToolStripMenuItem;
        private ToolStripMenuItem runTestsToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;

        // Tabs
        private TabControl tabControl1;
        private TabPage tabPageMain;
        private TabPage tabPageSettings;
        private TabPage tabPageRecentFiles;

        // Main tab controls
        private Label lblVectorStoreManagement;
        private ComboBox comboBoxVectorStores;
        private TextBox txtNewVectorStoreName;
        private Button btnCreateNewVectorStore;
        private Button btnSelectFolders;
        private ListBox listBoxSelectedFolders;

        // Settings tab controls
        private Button btnResetVsSettings;
        private Button btnSaveVsSettings;
        private CheckBox chkInheritExcludedFolders;
        private CheckBox chkInheritExcludedFiles;
        private TextBox txtExcludedFolders;
        private TextBox txtExcludedFiles;
        private ComboBox cmbSettingsVectorStore;
        private Label lblSettingsVectorStore;

        // ✅ Recent Files tab - CRITICAL: This field MUST be declared here
        private oaiUI.RecentFiles.RecentFilesPanel recentFilesPanel;

        // Status
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel statusLabel;
        private ToolStripProgressBar progressBar;

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
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            actionsToolStripMenuItem = new ToolStripMenuItem();
            convertToMdToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            getGitChangesToolStripMenuItem = new ToolStripMenuItem();
            fileSizeSummaryToolStripMenuItem = new ToolStripMenuItem();
            runTestsToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            tabControl1 = new TabControl();
            tabPageMain = new TabPage();
            lblVectorStoreManagement = new Label();
            comboBoxVectorStores = new ComboBox();
            txtNewVectorStoreName = new TextBox();
            btnCreateNewVectorStore = new Button();
            btnSelectFolders = new Button();
            listBoxSelectedFolders = new ListBox();
            tabPageSettings = new TabPage();
            btnResetVsSettings = new Button();
            btnSaveVsSettings = new Button();
            chkInheritExcludedFolders = new CheckBox();
            chkInheritExcludedFiles = new CheckBox();
            txtExcludedFolders = new TextBox();
            txtExcludedFiles = new TextBox();
            cmbSettingsVectorStore = new ComboBox();
            lblSettingsVectorStore = new Label();
            tabPageRecentFiles = new TabPage();
            recentFilesPanel = new oaiUI.RecentFiles.RecentFilesPanel();
            statusStrip1 = new StatusStrip();
            statusLabel = new ToolStripStatusLabel();
            progressBar = new ToolStripProgressBar();
            menuStrip1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPageMain.SuspendLayout();
            tabPageSettings.SuspendLayout();
            tabPageRecentFiles.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, actionsToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1200, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "&File";
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
            exitToolStripMenuItem.Size = new Size(134, 22);
            exitToolStripMenuItem.Text = "E&xit";
            // 
            // actionsToolStripMenuItem
            // 
            actionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { convertToMdToolStripMenuItem, toolStripSeparator1, getGitChangesToolStripMenuItem, fileSizeSummaryToolStripMenuItem, runTestsToolStripMenuItem });
            actionsToolStripMenuItem.Name = "actionsToolStripMenuItem";
            actionsToolStripMenuItem.Size = new Size(59, 20);
            actionsToolStripMenuItem.Text = "&Actions";
            // 
            // convertToMdToolStripMenuItem
            // 
            convertToMdToolStripMenuItem.Name = "convertToMdToolStripMenuItem";
            convertToMdToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.M;
            convertToMdToolStripMenuItem.Size = new Size(209, 22);
            convertToMdToolStripMenuItem.Text = "Convert to &MD";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(206, 6);
            // 
            // getGitChangesToolStripMenuItem
            // 
            getGitChangesToolStripMenuItem.Name = "getGitChangesToolStripMenuItem";
            getGitChangesToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.G;
            getGitChangesToolStripMenuItem.Size = new Size(209, 22);
            getGitChangesToolStripMenuItem.Text = "Get &Git Changes";
            // 
            // fileSizeSummaryToolStripMenuItem
            // 
            fileSizeSummaryToolStripMenuItem.Name = "fileSizeSummaryToolStripMenuItem";
            fileSizeSummaryToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F;
            fileSizeSummaryToolStripMenuItem.Size = new Size(209, 22);
            fileSizeSummaryToolStripMenuItem.Text = "&File Size Summary";
            // 
            // runTestsToolStripMenuItem
            // 
            runTestsToolStripMenuItem.Name = "runTestsToolStripMenuItem";
            runTestsToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.T;
            runTestsToolStripMenuItem.Size = new Size(209, 22);
            runTestsToolStripMenuItem.Text = "Run &Tests";
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(180, 22);
            aboutToolStripMenuItem.Text = "&About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItemClick;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPageMain);
            tabControl1.Controls.Add(tabPageSettings);
            tabControl1.Controls.Add(tabPageRecentFiles);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 24);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1200, 626);
            tabControl1.TabIndex = 1;
            // 
            // tabPageMain
            // 
            tabPageMain.Controls.Add(lblVectorStoreManagement);
            tabPageMain.Controls.Add(comboBoxVectorStores);
            tabPageMain.Controls.Add(txtNewVectorStoreName);
            tabPageMain.Controls.Add(btnCreateNewVectorStore);
            tabPageMain.Controls.Add(btnSelectFolders);
            tabPageMain.Controls.Add(listBoxSelectedFolders);
            tabPageMain.Location = new Point(4, 24);
            tabPageMain.Name = "tabPageMain";
            tabPageMain.Padding = new Padding(8);
            tabPageMain.Size = new Size(1192, 598);
            tabPageMain.TabIndex = 0;
            tabPageMain.Text = "Main";
            tabPageMain.UseVisualStyleBackColor = true;
            // 
            // lblVectorStoreManagement
            // 
            lblVectorStoreManagement.AutoSize = true;
            lblVectorStoreManagement.Location = new Point(16, 16);
            lblVectorStoreManagement.Name = "lblVectorStoreManagement";
            lblVectorStoreManagement.Size = new Size(144, 15);
            lblVectorStoreManagement.TabIndex = 0;
            lblVectorStoreManagement.Text = "Vector Store Management";
            // 
            // comboBoxVectorStores
            // 
            comboBoxVectorStores.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxVectorStores.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxVectorStores.FormattingEnabled = true;
            comboBoxVectorStores.Location = new Point(16, 40);
            comboBoxVectorStores.Name = "comboBoxVectorStores";
            comboBoxVectorStores.Size = new Size(700, 23);
            comboBoxVectorStores.TabIndex = 1;
            // 
            // txtNewVectorStoreName
            // 
            txtNewVectorStoreName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtNewVectorStoreName.Location = new Point(16, 72);
            txtNewVectorStoreName.Name = "txtNewVectorStoreName";
            txtNewVectorStoreName.PlaceholderText = "New vector store name...";
            txtNewVectorStoreName.Size = new Size(580, 23);
            txtNewVectorStoreName.TabIndex = 2;
            // 
            // btnCreateNewVectorStore
            // 
            btnCreateNewVectorStore.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCreateNewVectorStore.Location = new Point(604, 72);
            btnCreateNewVectorStore.Name = "btnCreateNewVectorStore";
            btnCreateNewVectorStore.Size = new Size(112, 23);
            btnCreateNewVectorStore.TabIndex = 3;
            btnCreateNewVectorStore.Text = "Create";
            btnCreateNewVectorStore.UseVisualStyleBackColor = true;
            // 
            // btnSelectFolders
            // 
            btnSelectFolders.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSelectFolders.Location = new Point(732, 40);
            btnSelectFolders.Name = "btnSelectFolders";
            btnSelectFolders.Size = new Size(128, 23);
            btnSelectFolders.TabIndex = 4;
            btnSelectFolders.Text = "Select Folders...";
            btnSelectFolders.UseVisualStyleBackColor = true;
            // 
            // listBoxSelectedFolders
            // 
            listBoxSelectedFolders.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listBoxSelectedFolders.FormattingEnabled = true;
            listBoxSelectedFolders.IntegralHeight = false;
            listBoxSelectedFolders.ItemHeight = 15;
            listBoxSelectedFolders.Location = new Point(16, 112);
            listBoxSelectedFolders.Name = "listBoxSelectedFolders";
            listBoxSelectedFolders.Size = new Size(1148, 464);
            listBoxSelectedFolders.TabIndex = 5;
            // 
            // tabPageSettings
            // 
            tabPageSettings.Controls.Add(btnResetVsSettings);
            tabPageSettings.Controls.Add(btnSaveVsSettings);
            tabPageSettings.Controls.Add(chkInheritExcludedFolders);
            tabPageSettings.Controls.Add(chkInheritExcludedFiles);
            tabPageSettings.Controls.Add(txtExcludedFolders);
            tabPageSettings.Controls.Add(txtExcludedFiles);
            tabPageSettings.Controls.Add(cmbSettingsVectorStore);
            tabPageSettings.Controls.Add(lblSettingsVectorStore);
            tabPageSettings.Location = new Point(4, 24);
            tabPageSettings.Name = "tabPageSettings";
            tabPageSettings.Padding = new Padding(8);
            tabPageSettings.Size = new Size(192, 72);
            tabPageSettings.TabIndex = 1;
            tabPageSettings.Text = "Settings";
            tabPageSettings.UseVisualStyleBackColor = true;
            // 
            // btnResetVsSettings
            // 
            btnResetVsSettings.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnResetVsSettings.Location = new Point(88, 2);
            btnResetVsSettings.Name = "btnResetVsSettings";
            btnResetVsSettings.Size = new Size(88, 28);
            btnResetVsSettings.TabIndex = 7;
            btnResetVsSettings.Text = "Reset";
            btnResetVsSettings.UseVisualStyleBackColor = true;
            // 
            // btnSaveVsSettings
            // 
            btnSaveVsSettings.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSaveVsSettings.Location = new Point(-12, 2);
            btnSaveVsSettings.Name = "btnSaveVsSettings";
            btnSaveVsSettings.Size = new Size(88, 28);
            btnSaveVsSettings.TabIndex = 6;
            btnSaveVsSettings.Text = "Save";
            btnSaveVsSettings.UseVisualStyleBackColor = true;
            // 
            // chkInheritExcludedFolders
            // 
            chkInheritExcludedFolders.AutoSize = true;
            chkInheritExcludedFolders.Location = new Point(200, 80);
            chkInheritExcludedFolders.Name = "chkInheritExcludedFolders";
            chkInheritExcludedFolders.Size = new Size(151, 19);
            chkInheritExcludedFolders.TabIndex = 3;
            chkInheritExcludedFolders.Text = "Inherit Excluded Folders";
            chkInheritExcludedFolders.UseVisualStyleBackColor = true;
            // 
            // chkInheritExcludedFiles
            // 
            chkInheritExcludedFiles.AutoSize = true;
            chkInheritExcludedFiles.Location = new Point(16, 80);
            chkInheritExcludedFiles.Name = "chkInheritExcludedFiles";
            chkInheritExcludedFiles.Size = new Size(136, 19);
            chkInheritExcludedFiles.TabIndex = 2;
            chkInheritExcludedFiles.Text = "Inherit Excluded Files";
            chkInheritExcludedFiles.UseVisualStyleBackColor = true;
            // 
            // txtExcludedFolders
            // 
            txtExcludedFolders.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtExcludedFolders.Location = new Point(16, 288);
            txtExcludedFolders.Multiline = true;
            txtExcludedFolders.Name = "txtExcludedFolders";
            txtExcludedFolders.PlaceholderText = "One folder name per line";
            txtExcludedFolders.ScrollBars = ScrollBars.Vertical;
            txtExcludedFolders.Size = new Size(160, 0);
            txtExcludedFolders.TabIndex = 5;
            // 
            // txtExcludedFiles
            // 
            txtExcludedFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtExcludedFiles.Location = new Point(16, 112);
            txtExcludedFiles.Multiline = true;
            txtExcludedFiles.Name = "txtExcludedFiles";
            txtExcludedFiles.PlaceholderText = "One pattern per line (e.g., *.tmp)";
            txtExcludedFiles.ScrollBars = ScrollBars.Vertical;
            txtExcludedFiles.Size = new Size(160, 160);
            txtExcludedFiles.TabIndex = 4;
            // 
            // cmbSettingsVectorStore
            // 
            cmbSettingsVectorStore.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbSettingsVectorStore.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSettingsVectorStore.FormattingEnabled = true;
            cmbSettingsVectorStore.Location = new Point(16, 40);
            cmbSettingsVectorStore.Name = "cmbSettingsVectorStore";
            cmbSettingsVectorStore.Size = new Size(0, 23);
            cmbSettingsVectorStore.TabIndex = 1;
            // 
            // lblSettingsVectorStore
            // 
            lblSettingsVectorStore.AutoSize = true;
            lblSettingsVectorStore.Location = new Point(16, 16);
            lblSettingsVectorStore.Name = "lblSettingsVectorStore";
            lblSettingsVectorStore.Size = new Size(73, 15);
            lblSettingsVectorStore.TabIndex = 0;
            lblSettingsVectorStore.Text = "Vector Store:";
            // 
            // tabPageRecentFiles
            // 
            tabPageRecentFiles.Controls.Add(recentFilesPanel);
            tabPageRecentFiles.Location = new Point(4, 24);
            tabPageRecentFiles.Name = "tabPageRecentFiles";
            tabPageRecentFiles.Padding = new Padding(8);
            tabPageRecentFiles.Size = new Size(192, 72);
            tabPageRecentFiles.TabIndex = 2;
            tabPageRecentFiles.Text = "Recent Files";
            tabPageRecentFiles.UseVisualStyleBackColor = true;
            // 
            // recentFilesPanel
            // 
            recentFilesPanel.BackColor = Color.FromArgb(32, 32, 32);
            recentFilesPanel.Dock = DockStyle.Fill;
            recentFilesPanel.ForeColor = Color.Gainsboro;
            recentFilesPanel.Location = new Point(8, 8);
            recentFilesPanel.Margin = new Padding(6);
            recentFilesPanel.Name = "recentFilesPanel";
            recentFilesPanel.Size = new Size(176, 56);
            recentFilesPanel.TabIndex = 0;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { statusLabel, progressBar });
            statusStrip1.Location = new Point(0, 650);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1200, 22);
            statusStrip1.TabIndex = 2;
            statusStrip1.Text = "statusStrip1";
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new Size(1083, 17);
            statusLabel.Spring = true;
            statusLabel.Text = "Ready";
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // progressBar
            // 
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(100, 16);
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1200, 672);
            Controls.Add(tabControl1);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "VecTool";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            tabControl1.ResumeLayout(false);
            tabPageMain.ResumeLayout(false);
            tabPageMain.PerformLayout();
            tabPageSettings.ResumeLayout(false);
            tabPageSettings.PerformLayout();
            tabPageRecentFiles.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
