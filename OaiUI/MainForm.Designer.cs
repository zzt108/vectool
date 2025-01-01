using System.Reflection;

namespace oaiUI
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            comboBoxVectorStores = new ComboBox();
            txtNewVectorStoreName = new TextBox();
            btnSelectFolders = new Button();
            btnUploadFiles = new Button();
            labelSelectVectorStore = new Label();
            labelNewVectorStore = new Label();
            progressBar1 = new ProgressBar();
            btnDeleteAllVSFiles = new Button();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabelState = new ToolStripStatusLabel();
            toolStripStatusLabelCurrent = new ToolStripStatusLabel();
            toolStripStatusLabelMax = new ToolStripStatusLabel();
            toolStripStatusLabelInfo = new ToolStripStatusLabel();
            btnUploadNew = new Button();
            btnClearFolders = new Button();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            panel1 = new Panel();
            splitContainer1 = new SplitContainer();
            panel2 = new Panel();
            label1 = new Label();
            btnConvertToDocx = new Button();
            btnConvertToMd = new Button();
            btnDeleteVectorStoreAssoc = new Button();
            tabPage2 = new TabPage();
            richTextBox1 = new RichTextBox();
            statusStrip1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // comboBoxVectorStores
            // 
            comboBoxVectorStores.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxVectorStores.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxVectorStores.FormattingEnabled = true;
            comboBoxVectorStores.Location = new Point(12, 42);
            comboBoxVectorStores.Margin = new Padding(3, 2, 3, 2);
            comboBoxVectorStores.Name = "comboBoxVectorStores";
            comboBoxVectorStores.Size = new Size(242, 23);
            comboBoxVectorStores.TabIndex = 0;
            // 
            // txtNewVectorStoreName
            // 
            txtNewVectorStoreName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtNewVectorStoreName.Location = new Point(261, 42);
            txtNewVectorStoreName.Margin = new Padding(3, 2, 3, 2);
            txtNewVectorStoreName.Name = "txtNewVectorStoreName";
            txtNewVectorStoreName.Size = new Size(242, 23);
            txtNewVectorStoreName.TabIndex = 1;
            // 
            // btnSelectFolders
            // 
            btnSelectFolders.Location = new Point(12, 82);
            btnSelectFolders.Margin = new Padding(3, 2, 3, 2);
            btnSelectFolders.Name = "btnSelectFolders";
            btnSelectFolders.Size = new Size(113, 28);
            btnSelectFolders.TabIndex = 2;
            btnSelectFolders.Text = "Select Folders";
            btnSelectFolders.UseVisualStyleBackColor = true;
            btnSelectFolders.Click += btnSelectFolders_Click;
            // 
            // btnUploadFiles
            // 
            btnUploadFiles.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnUploadFiles.Location = new Point(12, 231);
            btnUploadFiles.Margin = new Padding(3, 2, 3, 2);
            btnUploadFiles.Name = "btnUploadFiles";
            btnUploadFiles.Size = new Size(113, 28);
            btnUploadFiles.TabIndex = 4;
            btnUploadFiles.Text = "Upload / Replace";
            btnUploadFiles.UseVisualStyleBackColor = true;
            btnUploadFiles.Click += btnUploadFiles_Click;
            // 
            // labelSelectVectorStore
            // 
            labelSelectVectorStore.AutoSize = true;
            labelSelectVectorStore.Location = new Point(12, 25);
            labelSelectVectorStore.Name = "labelSelectVectorStore";
            labelSelectVectorStore.Size = new Size(151, 15);
            labelSelectVectorStore.TabIndex = 5;
            labelSelectVectorStore.Text = "Select Existing Vector Store:";
            // 
            // labelNewVectorStore
            // 
            labelNewVectorStore.AutoSize = true;
            labelNewVectorStore.Location = new Point(261, 25);
            labelNewVectorStore.Name = "labelNewVectorStore";
            labelNewVectorStore.Size = new Size(115, 15);
            labelNewVectorStore.TabIndex = 6;
            labelNewVectorStore.Text = "Or Enter New Name:";
            // 
            // progressBar1
            // 
            progressBar1.Dock = DockStyle.Top;
            progressBar1.Location = new Point(0, 0);
            progressBar1.Margin = new Padding(3, 2, 3, 2);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(800, 23);
            progressBar1.TabIndex = 7;
            // 
            // btnDeleteAllVSFiles
            // 
            btnDeleteAllVSFiles.BackColor = Color.Red;
            btnDeleteAllVSFiles.Location = new Point(12, 114);
            btnDeleteAllVSFiles.Margin = new Padding(3, 2, 3, 2);
            btnDeleteAllVSFiles.Name = "btnDeleteAllVSFiles";
            btnDeleteAllVSFiles.Size = new Size(232, 28);
            btnDeleteAllVSFiles.TabIndex = 8;
            btnDeleteAllVSFiles.Text = "Delete All VS files";
            btnDeleteAllVSFiles.UseVisualStyleBackColor = false;
            btnDeleteAllVSFiles.Click += btnDeleteAllVSFiles_ClickAsync;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabelState, toolStripStatusLabelCurrent, toolStripStatusLabelMax, toolStripStatusLabelInfo });
            statusStrip1.Location = new Point(0, 428);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 10, 0);
            statusStrip1.Size = new Size(800, 22);
            statusStrip1.TabIndex = 9;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabelState
            // 
            toolStripStatusLabelState.Name = "toolStripStatusLabelState";
            toolStripStatusLabelState.Size = new Size(39, 17);
            toolStripStatusLabelState.Text = "Status";
            // 
            // toolStripStatusLabelCurrent
            // 
            toolStripStatusLabelCurrent.Name = "toolStripStatusLabelCurrent";
            toolStripStatusLabelCurrent.Size = new Size(47, 17);
            toolStripStatusLabelCurrent.Text = "Current";
            // 
            // toolStripStatusLabelMax
            // 
            toolStripStatusLabelMax.Name = "toolStripStatusLabelMax";
            toolStripStatusLabelMax.Size = new Size(30, 17);
            toolStripStatusLabelMax.Text = "Max";
            // 
            // toolStripStatusLabelInfo
            // 
            toolStripStatusLabelInfo.Name = "toolStripStatusLabelInfo";
            toolStripStatusLabelInfo.Size = new Size(28, 17);
            toolStripStatusLabelInfo.Text = "Info";
            // 
            // btnUploadNew
            // 
            btnUploadNew.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnUploadNew.Location = new Point(131, 231);
            btnUploadNew.Margin = new Padding(3, 2, 3, 2);
            btnUploadNew.Name = "btnUploadNew";
            btnUploadNew.Size = new Size(113, 28);
            btnUploadNew.TabIndex = 10;
            btnUploadNew.Text = "Upload New";
            btnUploadNew.UseVisualStyleBackColor = true;
            btnUploadNew.Visible = false;
            btnUploadNew.Click += btnUploadNew_Click;
            // 
            // btnClearFolders
            // 
            btnClearFolders.Location = new Point(131, 82);
            btnClearFolders.Margin = new Padding(3, 2, 3, 2);
            btnClearFolders.Name = "btnClearFolders";
            btnClearFolders.Size = new Size(113, 28);
            btnClearFolders.TabIndex = 0;
            btnClearFolders.Text = "Empty Selected";
            btnClearFolders.UseVisualStyleBackColor = true;
            btnClearFolders.Click += btnClearFolders_Click;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 23);
            tabControl1.Margin = new Padding(3, 2, 3, 2);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(800, 405);
            tabControl1.TabIndex = 11;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(panel1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Margin = new Padding(3, 2, 3, 2);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3, 2, 3, 2);
            tabPage1.Size = new Size(792, 377);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Main";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            panel1.Controls.Add(splitContainer1);
            panel1.Controls.Add(btnDeleteAllVSFiles);
            panel1.Controls.Add(btnDeleteVectorStoreAssoc);
            panel1.Controls.Add(btnClearFolders);
            panel1.Controls.Add(btnSelectFolders);
            panel1.Controls.Add(labelSelectVectorStore);
            panel1.Controls.Add(btnUploadNew);
            panel1.Controls.Add(labelNewVectorStore);
            panel1.Controls.Add(btnUploadFiles);
            panel1.Controls.Add(comboBoxVectorStores);
            panel1.Controls.Add(txtNewVectorStoreName);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(3, 2);
            panel1.Name = "panel1";
            panel1.Size = new Size(786, 373);
            panel1.TabIndex = 12;
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.Location = new Point(12, 147);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(richTextBox1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(panel2);
            splitContainer1.Size = new Size(771, 78);
            splitContainer1.SplitterDistance = 556;
            splitContainer1.TabIndex = 11;
            // 
            // panel2
            // 
            panel2.Controls.Add(label1);
            panel2.Controls.Add(btnConvertToDocx);
            panel2.Controls.Add(btnConvertToMd);
            panel2.Dock = DockStyle.Fill;
            panel2.Location = new Point(0, 0);
            panel2.Name = "panel2";
            panel2.Size = new Size(211, 78);
            panel2.TabIndex = 12;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 0);
            label1.Name = "label1";
            label1.Size = new Size(116, 15);
            label1.TabIndex = 13;
            label1.Text = "Convert to single file";
            // 
            // btnConvertToDocx
            // 
            btnConvertToDocx.Location = new Point(0, 17);
            btnConvertToDocx.Margin = new Padding(3, 2, 3, 2);
            btnConvertToDocx.Name = "btnConvertToDocx";
            btnConvertToDocx.Size = new Size(100, 28);
            btnConvertToDocx.TabIndex = 11;
            btnConvertToDocx.Text = "DOCX";
            btnConvertToDocx.UseVisualStyleBackColor = true;
            btnConvertToDocx.Click += btnConvertToDocx_Click;
            // 
            // btnConvertToMd
            // 
            btnConvertToMd.Location = new Point(106, 17);
            btnConvertToMd.Margin = new Padding(3, 2, 3, 2);
            btnConvertToMd.Name = "btnConvertToMd";
            btnConvertToMd.Size = new Size(100, 28);
            btnConvertToMd.TabIndex = 12;
            btnConvertToMd.Text = "MD";
            btnConvertToMd.UseVisualStyleBackColor = true;
            btnConvertToMd.Click += btnConvertToMD_Click;
            // 
            // btnDeleteVectorStoreAssoc
            // 
            btnDeleteVectorStoreAssoc.BackColor = Color.Red;
            btnDeleteVectorStoreAssoc.Location = new Point(261, 114);
            btnDeleteVectorStoreAssoc.Margin = new Padding(3, 2, 3, 2);
            btnDeleteVectorStoreAssoc.Name = "btnDeleteVectorStoreAssoc";
            btnDeleteVectorStoreAssoc.Size = new Size(242, 28);
            btnDeleteVectorStoreAssoc.TabIndex = 9;
            btnDeleteVectorStoreAssoc.Text = "Delete VS Association";
            btnDeleteVectorStoreAssoc.UseVisualStyleBackColor = false;
            btnDeleteVectorStoreAssoc.Click += btnDeleteVectorStoreAssoc_Click;
            // 
            // tabPage2
            // 
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(792, 377);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Settings";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Location = new Point(0, 0);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(556, 78);
            richTextBox1.TabIndex = 13;
            richTextBox1.Text = "";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(tabControl1);
            Controls.Add(statusStrip1);
            Controls.Add(progressBar1);
            Margin = new Padding(3, 2, 3, 2);
            Name = "MainForm";
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ComboBox comboBoxVectorStores = null!;
        private TextBox txtNewVectorStoreName;
        private Button btnSelectFolders;
        private Button btnUploadFiles;
        private Button btnDeleteVectorStoreAssoc = null!; // Declaration for the new button

        private Label labelSelectVectorStore;
        private Label labelNewVectorStore;
        private ProgressBar progressBar1;
        private Button btnDeleteAllVSFiles;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabelState;
        private ToolStripStatusLabel toolStripStatusLabelCurrent;
        private ToolStripStatusLabel toolStripStatusLabelMax;
        private Button btnUploadNew;
        private ToolStripStatusLabel toolStripStatusLabelInfo;
        private Button btnClearFolders;
        private Button btnConvertToDocx;
        private Button btnConvertToMd;

        // private ComboBox comboBoxVectorStores;
        // private TextBox txtNewVectorStoreName;
        // private Button btnSelectFolders;
        // private Button btnUploadFiles;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private Panel panel1;
        private SplitContainer splitContainer1;
        private Panel panel2;
        private Label label1;
        private RichTextBox richTextBox1;
    }
}
