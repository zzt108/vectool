﻿namespace oaiUI
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
            listBoxSelectedFolders = new ListBox();
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
            btnConvertToDocx = new Button();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // comboBoxVectorStores
            // 
            comboBoxVectorStores.Location = new Point(12, 61);
            comboBoxVectorStores.Name = "comboBoxVectorStores";
            comboBoxVectorStores.Size = new Size(200, 23);
            comboBoxVectorStores.TabIndex = 0;
            // 
            // txtNewVectorStoreName
            // 
            txtNewVectorStoreName.Location = new Point(232, 61);
            txtNewVectorStoreName.Name = "txtNewVectorStoreName";
            txtNewVectorStoreName.Size = new Size(150, 23);
            txtNewVectorStoreName.TabIndex = 1;
            // 
            // btnSelectFolders
            // 
            btnSelectFolders.Location = new Point(12, 133);
            btnSelectFolders.Name = "btnSelectFolders";
            btnSelectFolders.Size = new Size(100, 30);
            btnSelectFolders.TabIndex = 2;
            btnSelectFolders.Text = "Select Folders";
            btnSelectFolders.Click += btnSelectFolders_Click;
            // 
            // listBoxSelectedFolders
            // 
            listBoxSelectedFolders.ItemHeight = 15;
            listBoxSelectedFolders.Location = new Point(12, 173);
            listBoxSelectedFolders.Name = "listBoxSelectedFolders";
            listBoxSelectedFolders.Size = new Size(379, 94);
            listBoxSelectedFolders.TabIndex = 3;
            // 
            // btnUploadFiles
            // 
            btnUploadFiles.Location = new Point(12, 293);
            btnUploadFiles.Name = "btnUploadFiles";
            btnUploadFiles.Size = new Size(100, 30);
            btnUploadFiles.TabIndex = 4;
            btnUploadFiles.Text = "Upload Replace";
            btnUploadFiles.Click += btnUploadFiles_Click;
            // 
            // labelSelectVectorStore
            // 
            labelSelectVectorStore.Location = new Point(12, 41);
            labelSelectVectorStore.Name = "labelSelectVectorStore";
            labelSelectVectorStore.Size = new Size(100, 23);
            labelSelectVectorStore.TabIndex = 5;
            labelSelectVectorStore.Text = "Select Existing Vector Store:";
            // 
            // labelNewVectorStore
            // 
            labelNewVectorStore.Location = new Point(232, 41);
            labelNewVectorStore.Name = "labelNewVectorStore";
            labelNewVectorStore.Size = new Size(100, 23);
            labelNewVectorStore.TabIndex = 6;
            labelNewVectorStore.Text = "Or Enter New Name:";
            // 
            // progressBar1
            // 
            progressBar1.Dock = DockStyle.Top;
            progressBar1.Location = new Point(0, 0);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(800, 23);
            progressBar1.TabIndex = 7;
            // 
            // btnDeleteAllVSFiles
            // 
            btnDeleteAllVSFiles.BackColor = Color.Firebrick;
            btnDeleteAllVSFiles.Location = new Point(12, 90);
            btnDeleteAllVSFiles.Name = "btnDeleteAllVSFiles";
            btnDeleteAllVSFiles.Size = new Size(200, 30);
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
            btnUploadNew.Location = new Point(232, 293);
            btnUploadNew.Name = "btnUploadNew";
            btnUploadNew.Size = new Size(100, 30);
            btnUploadNew.TabIndex = 10;
            btnUploadNew.Text = "Upload New";
            // 
            // btnClearFolders
            // 
            btnClearFolders.Location = new Point(118, 133);
            btnClearFolders.Name = "btnClearFolders";
            btnClearFolders.Size = new Size(108, 30);
            btnClearFolders.TabIndex = 0;
            btnClearFolders.Text = "Empty Selected";
            btnClearFolders.Click += btnClearFolders_Click;
            // 
            // btnConvertToDocx
            // 
            btnConvertToDocx.Location = new Point(232, 133);
            btnConvertToDocx.Name = "btnConvertToDocx";
            btnConvertToDocx.Size = new Size(127, 30);
            btnConvertToDocx.TabIndex = 11;
            btnConvertToDocx.Text = "Convert to 1 DOCX";
            btnConvertToDocx.Click += btnConvertToDocx_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnConvertToDocx);
            Controls.Add(btnClearFolders);
            Controls.Add(btnUploadNew);
            Controls.Add(statusStrip1);
            Controls.Add(btnDeleteAllVSFiles);
            Controls.Add(progressBar1);
            Controls.Add(comboBoxVectorStores);
            Controls.Add(txtNewVectorStoreName);
            Controls.Add(btnSelectFolders);
            Controls.Add(listBoxSelectedFolders);
            Controls.Add(btnUploadFiles);
            Controls.Add(labelSelectVectorStore);
            Controls.Add(labelNewVectorStore);
            Name = "MainForm";
            Text = "Main Form";
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

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
    }
}
