// Path: VecTool.UI/OaiUI/MainForm.Designer.cs
// NOTE: Designer-generated layout for MainForm. Control names match existing partials.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace VecTool.OaiUI
{
    partial class MainForm
    {
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

        // New: Help → About
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

        // Recent Files tab
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

        // Windows Form Designer generated code
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // Instantiate controls
            this.menuStrip1 = new MenuStrip();
            this.fileToolStripMenuItem = new ToolStripMenuItem();
            this.exitToolStripMenuItem = new ToolStripMenuItem();
            this.actionsToolStripMenuItem = new ToolStripMenuItem();
            this.convertToMdToolStripMenuItem = new ToolStripMenuItem();
            this.toolStripSeparator1 = new ToolStripSeparator();
            this.getGitChangesToolStripMenuItem = new ToolStripMenuItem();
            this.fileSizeSummaryToolStripMenuItem = new ToolStripMenuItem();
            this.runTestsToolStripMenuItem = new ToolStripMenuItem();
            this.helpToolStripMenuItem = new ToolStripMenuItem();
            this.aboutToolStripMenuItem = new ToolStripMenuItem();

            this.tabControl1 = new TabControl();
            this.tabPageMain = new TabPage();
            this.tabPageSettings = new TabPage();
            this.tabPageRecentFiles = new TabPage();

            this.lblVectorStoreManagement = new Label();
            this.comboBoxVectorStores = new ComboBox();
            this.txtNewVectorStoreName = new TextBox();
            this.btnCreateNewVectorStore = new Button();
            this.btnSelectFolders = new Button();
            this.listBoxSelectedFolders = new ListBox();

            this.btnResetVsSettings = new Button();
            this.btnSaveVsSettings = new Button();
            this.chkInheritExcludedFolders = new CheckBox();
            this.chkInheritExcludedFiles = new CheckBox();
            this.txtExcludedFolders = new TextBox();
            this.txtExcludedFiles = new TextBox();
            this.cmbSettingsVectorStore = new ComboBox();
            this.lblSettingsVectorStore = new Label();

            this.recentFilesPanel = new oaiUI.RecentFiles.RecentFilesPanel();

            this.statusStrip1 = new StatusStrip();
            this.statusLabel = new ToolStripStatusLabel();
            this.progressBar = new ToolStripProgressBar();

            // Suspend layout for containers
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageMain.SuspendLayout();
            this.tabPageSettings.SuspendLayout();
            this.tabPageRecentFiles.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();

            // menuStrip1
            this.menuStrip1.Items.AddRange(new ToolStripItem[] {
                this.fileToolStripMenuItem,
                this.actionsToolStripMenuItem,
                this.helpToolStripMenuItem
            });
            this.menuStrip1.Location = new Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new Size(1200, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";

            // File menu
            this.fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                this.exitToolStripMenuItem
            });
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";

            // Exit
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.ShortcutKeys = ((Keys)((Keys.Alt | Keys.F4)));
            this.exitToolStripMenuItem.Size = new Size(180, 22);
            this.exitToolStripMenuItem.Text = "Exit";

            // Actions menu
            this.actionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                this.convertToMdToolStripMenuItem,
                this.toolStripSeparator1,
                this.getGitChangesToolStripMenuItem,
                this.fileSizeSummaryToolStripMenuItem,
                this.runTestsToolStripMenuItem
            });
            this.actionsToolStripMenuItem.Name = "actionsToolStripMenuItem";
            this.actionsToolStripMenuItem.Size = new Size(59, 20);
            this.actionsToolStripMenuItem.Text = "Actions";

            // Convert to MD
            this.convertToMdToolStripMenuItem.Name = "convertToMdToolStripMenuItem";
            this.convertToMdToolStripMenuItem.ShortcutKeys = ((Keys)((Keys.Control | Keys.M)));
            this.convertToMdToolStripMenuItem.Size = new Size(220, 22);
            this.convertToMdToolStripMenuItem.Text = "Convert to MD";

            // Separator
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new Size(217, 6);

            // Get Git Changes
            this.getGitChangesToolStripMenuItem.Name = "getGitChangesToolStripMenuItem";
            this.getGitChangesToolStripMenuItem.Size = new Size(220, 22);
            this.getGitChangesToolStripMenuItem.Text = "Get Git Changes";

            // File Size Summary
            this.fileSizeSummaryToolStripMenuItem.Name = "fileSizeSummaryToolStripMenuItem";
            this.fileSizeSummaryToolStripMenuItem.Size = new Size(220, 22);
            this.fileSizeSummaryToolStripMenuItem.Text = "File Size Summary";

            // Run Tests
            this.runTestsToolStripMenuItem.Name = "runTestsToolStripMenuItem";
            this.runTestsToolStripMenuItem.ShortcutKeys = ((Keys)((Keys.Control | Keys.T)));
            this.runTestsToolStripMenuItem.Size = new Size(220, 22);
            this.runTestsToolStripMenuItem.Text = "Run Tests";

            // Help menu
            this.helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
                this.aboutToolStripMenuItem
            });
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";

            // About
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new Size(180, 22);
            this.aboutToolStripMenuItem.Text = "About VecTool...";
            // Note: other menu handlers are wired in code-behind; About is wired here for convenience
            this.aboutToolStripMenuItem.Click += new EventHandler(this.aboutToolStripMenuItem_Click);

            // tabControl1
            this.tabControl1.Controls.Add(this.tabPageMain);
            this.tabControl1.Controls.Add(this.tabPageSettings);
            this.tabControl1.Controls.Add(this.tabPageRecentFiles);
            this.tabControl1.Dock = DockStyle.Fill;
            this.tabControl1.Location = new Point(0, 24);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new Size(1200, 626);
            this.tabControl1.TabIndex = 1;

            // tabPageMain
            this.tabPageMain.Controls.Add(this.lblVectorStoreManagement);
            this.tabPageMain.Controls.Add(this.comboBoxVectorStores);
            this.tabPageMain.Controls.Add(this.txtNewVectorStoreName);
            this.tabPageMain.Controls.Add(this.btnCreateNewVectorStore);
            this.tabPageMain.Controls.Add(this.btnSelectFolders);
            this.tabPageMain.Controls.Add(this.listBoxSelectedFolders);
            this.tabPageMain.Location = new Point(4, 24);
            this.tabPageMain.Name = "tabPageMain";
            this.tabPageMain.Padding = new Padding(8);
            this.tabPageMain.Size = new Size(1192, 598);
            this.tabPageMain.TabIndex = 0;
            this.tabPageMain.Text = "Main";
            this.tabPageMain.UseVisualStyleBackColor = true;

            // lblVectorStoreManagement
            this.lblVectorStoreManagement.AutoSize = true;
            this.lblVectorStoreManagement.Location = new Point(16, 16);
            this.lblVectorStoreManagement.Name = "lblVectorStoreManagement";
            this.lblVectorStoreManagement.Size = new Size(155, 15);
            this.lblVectorStoreManagement.TabIndex = 0;
            this.lblVectorStoreManagement.Text = "Vector Store Management";

            // comboBoxVectorStores
            this.comboBoxVectorStores.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.comboBoxVectorStores.DropDownStyle = ComboBoxStyle.DropDownList;
            this.comboBoxVectorStores.FormattingEnabled = true;
            this.comboBoxVectorStores.Location = new Point(16, 40);
            this.comboBoxVectorStores.Name = "comboBoxVectorStores";
            this.comboBoxVectorStores.Size = new Size(700, 23);
            this.comboBoxVectorStores.TabIndex = 1;

            // txtNewVectorStoreName
            this.txtNewVectorStoreName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtNewVectorStoreName.Location = new Point(16, 72);
            this.txtNewVectorStoreName.Name = "txtNewVectorStoreName";
            this.txtNewVectorStoreName.PlaceholderText = "New vector store name...";
            this.txtNewVectorStoreName.Size = new Size(580, 23);
            this.txtNewVectorStoreName.TabIndex = 2;

            // btnCreateNewVectorStore
            this.btnCreateNewVectorStore.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnCreateNewVectorStore.Location = new Point(604, 72);
            this.btnCreateNewVectorStore.Name = "btnCreateNewVectorStore";
            this.btnCreateNewVectorStore.Size = new Size(112, 23);
            this.btnCreateNewVectorStore.TabIndex = 3;
            this.btnCreateNewVectorStore.Text = "Create";
            this.btnCreateNewVectorStore.UseVisualStyleBackColor = true;

            // btnSelectFolders
            this.btnSelectFolders.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnSelectFolders.Location = new Point(732, 40);
            this.btnSelectFolders.Name = "btnSelectFolders";
            this.btnSelectFolders.Size = new Size(128, 23);
            this.btnSelectFolders.TabIndex = 4;
            this.btnSelectFolders.Text = "Select Folders...";
            this.btnSelectFolders.UseVisualStyleBackColor = true;

            // listBoxSelectedFolders
            this.listBoxSelectedFolders.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.listBoxSelectedFolders.FormattingEnabled = true;
            this.listBoxSelectedFolders.IntegralHeight = false;
            this.listBoxSelectedFolders.Location = new Point(16, 112);
            this.listBoxSelectedFolders.Name = "listBoxSelectedFolders";
            this.listBoxSelectedFolders.Size = new Size(1148, 464);
            this.listBoxSelectedFolders.TabIndex = 5;

            // tabPageSettings
            this.tabPageSettings.Controls.Add(this.btnResetVsSettings);
            this.tabPageSettings.Controls.Add(this.btnSaveVsSettings);
            this.tabPageSettings.Controls.Add(this.chkInheritExcludedFolders);
            this.tabPageSettings.Controls.Add(this.chkInheritExcludedFiles);
            this.tabPageSettings.Controls.Add(this.txtExcludedFolders);
            this.tabPageSettings.Controls.Add(this.txtExcludedFiles);
            this.tabPageSettings.Controls.Add(this.cmbSettingsVectorStore);
            this.tabPageSettings.Controls.Add(this.lblSettingsVectorStore);
            this.tabPageSettings.Location = new Point(4, 24);
            this.tabPageSettings.Name = "tabPageSettings";
            this.tabPageSettings.Padding = new Padding(8);
            this.tabPageSettings.Size = new Size(1192, 598);
            this.tabPageSettings.TabIndex = 1;
            this.tabPageSettings.Text = "Settings";
            this.tabPageSettings.UseVisualStyleBackColor = true;

            // lblSettingsVectorStore
            this.lblSettingsVectorStore.AutoSize = true;
            this.lblSettingsVectorStore.Location = new Point(16, 16);
            this.lblSettingsVectorStore.Name = "lblSettingsVectorStore";
            this.lblSettingsVectorStore.Size = new Size(76, 15);
            this.lblSettingsVectorStore.TabIndex = 0;
            this.lblSettingsVectorStore.Text = "Vector Store";

            // cmbSettingsVectorStore
            this.cmbSettingsVectorStore.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.cmbSettingsVectorStore.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbSettingsVectorStore.FormattingEnabled = true;
            this.cmbSettingsVectorStore.Location = new Point(16, 40);
            this.cmbSettingsVectorStore.Name = "cmbSettingsVectorStore";
            this.cmbSettingsVectorStore.Size = new Size(600, 23);
            this.cmbSettingsVectorStore.TabIndex = 1;

            // chkInheritExcludedFiles
            this.chkInheritExcludedFiles.AutoSize = true;
            this.chkInheritExcludedFiles.Location = new Point(16, 80);
            this.chkInheritExcludedFiles.Name = "chkInheritExcludedFiles";
            this.chkInheritExcludedFiles.Size = new Size(146, 19);
            this.chkInheritExcludedFiles.TabIndex = 2;
            this.chkInheritExcludedFiles.Text = "Inherit Excluded Files";
            this.chkInheritExcludedFiles.UseVisualStyleBackColor = true;

            // chkInheritExcludedFolders
            this.chkInheritExcludedFolders.AutoSize = true;
            this.chkInheritExcludedFolders.Location = new Point(200, 80);
            this.chkInheritExcludedFolders.Name = "chkInheritExcludedFolders";
            this.chkInheritExcludedFolders.Size = new Size(161, 19);
            this.chkInheritExcludedFolders.TabIndex = 3;
            this.chkInheritExcludedFolders.Text = "Inherit Excluded Folders";
            this.chkInheritExcludedFolders.UseVisualStyleBackColor = true;

            // txtExcludedFiles
            this.txtExcludedFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.txtExcludedFiles.Location = new Point(16, 112);
            this.txtExcludedFiles.Multiline = true;
            this.txtExcludedFiles.ScrollBars = ScrollBars.Vertical;
            this.txtExcludedFiles.Size = new Size(1160, 160);
            this.txtExcludedFiles.Name = "txtExcludedFiles";
            this.txtExcludedFiles.TabIndex = 4;
            this.txtExcludedFiles.PlaceholderText = "One pattern per line (e.g., *.tmp)";

            // txtExcludedFolders
            this.txtExcludedFolders.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            this.txtExcludedFolders.Location = new Point(16, 288);
            this.txtExcludedFolders.Multiline = true;
            this.txtExcludedFolders.ScrollBars = ScrollBars.Vertical;
            this.txtExcludedFolders.Size = new Size(1160, 224);
            this.txtExcludedFolders.Name = "txtExcludedFolders";
            this.txtExcludedFolders.TabIndex = 5;
            this.txtExcludedFolders.PlaceholderText = "One folder name per line";

            // btnSaveVsSettings
            this.btnSaveVsSettings.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnSaveVsSettings.Location = new Point(988, 528);
            this.btnSaveVsSettings.Name = "btnSaveVsSettings";
            this.btnSaveVsSettings.Size = new Size(88, 28);
            this.btnSaveVsSettings.TabIndex = 6;
            this.btnSaveVsSettings.Text = "Save";
            this.btnSaveVsSettings.UseVisualStyleBackColor = true;

            // btnResetVsSettings
            this.btnResetVsSettings.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnResetVsSettings.Location = new Point(1088, 528);
            this.btnResetVsSettings.Name = "btnResetVsSettings";
            this.btnResetVsSettings.Size = new Size(88, 28);
            this.btnResetVsSettings.TabIndex = 7;
            this.btnResetVsSettings.Text = "Reset";
            this.btnResetVsSettings.UseVisualStyleBackColor = true;

            // tabPageRecentFiles
            this.tabPageRecentFiles.Controls.Add(this.recentFilesPanel);
            this.tabPageRecentFiles.Location = new Point(4, 24);
            this.tabPageRecentFiles.Name = "tabPageRecentFiles";
            this.tabPageRecentFiles.Padding = new Padding(8);
            this.tabPageRecentFiles.Size = new Size(1192, 598);
            this.tabPageRecentFiles.TabIndex = 2;
            this.tabPageRecentFiles.Text = "Recent Files";
            this.tabPageRecentFiles.UseVisualStyleBackColor = true;

            // recentFilesPanel
            this.recentFilesPanel.Dock = DockStyle.Fill;
            this.recentFilesPanel.Location = new Point(8, 8);
            this.recentFilesPanel.Name = "recentFilesPanel";
            this.recentFilesPanel.Size = new Size(1176, 582);
            this.recentFilesPanel.TabIndex = 0;

            // statusStrip1
            this.statusStrip1.Items.AddRange(new ToolStripItem[] {
                this.statusLabel,
                this.progressBar
            });
            this.statusStrip1.Location = new Point(0, 650);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new Size(1200, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";

            // statusLabel
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new Size(39, 17);
            this.statusLabel.Text = "Ready";

            // progressBar
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(200, 16);
            this.progressBar.Style = ProgressBarStyle.Continuous;

            // MainForm
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1200, 672);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "VecTool";

            // Resume layouts
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
    }
}
