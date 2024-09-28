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
            listBoxSelectedFolders = new ListBox();
            btnUploadFiles = new Button();
            labelSelectVectorStore = new Label();
            labelNewVectorStore = new Label();
            progressBar1 = new ProgressBar();
            btnDeleteAllVSFiles = new Button();
            SuspendLayout();
            // 
            // comboBoxVectorStores
            // 
            comboBoxVectorStores.Location = new Point(20, 20);
            comboBoxVectorStores.Name = "comboBoxVectorStores";
            comboBoxVectorStores.Size = new Size(200, 23);
            comboBoxVectorStores.TabIndex = 0;
            // 
            // txtNewVectorStoreName
            // 
            txtNewVectorStoreName.Location = new Point(240, 20);
            txtNewVectorStoreName.Name = "txtNewVectorStoreName";
            txtNewVectorStoreName.Size = new Size(150, 23);
            txtNewVectorStoreName.TabIndex = 1;
            // 
            // btnSelectFolders
            // 
            btnSelectFolders.Location = new Point(20, 60);
            btnSelectFolders.Name = "btnSelectFolders";
            btnSelectFolders.Size = new Size(100, 30);
            btnSelectFolders.TabIndex = 2;
            btnSelectFolders.Text = "Select Folders";
            btnSelectFolders.Click += btnSelectFolders_Click;
            // 
            // listBoxSelectedFolders
            // 
            listBoxSelectedFolders.ItemHeight = 15;
            listBoxSelectedFolders.Location = new Point(20, 100);
            listBoxSelectedFolders.Name = "listBoxSelectedFolders";
            listBoxSelectedFolders.Size = new Size(379, 94);
            listBoxSelectedFolders.TabIndex = 3;
            // 
            // btnUploadFiles
            // 
            btnUploadFiles.Location = new Point(20, 220);
            btnUploadFiles.Name = "btnUploadFiles";
            btnUploadFiles.Size = new Size(100, 30);
            btnUploadFiles.TabIndex = 4;
            btnUploadFiles.Text = "Upload Files";
            btnUploadFiles.Click += btnUploadFiles_Click;
            // 
            // labelSelectVectorStore
            // 
            labelSelectVectorStore.Location = new Point(20, 0);
            labelSelectVectorStore.Name = "labelSelectVectorStore";
            labelSelectVectorStore.Size = new Size(100, 23);
            labelSelectVectorStore.TabIndex = 5;
            labelSelectVectorStore.Text = "Select Existing Vector Store:";
            // 
            // labelNewVectorStore
            // 
            labelNewVectorStore.Location = new Point(240, 0);
            labelNewVectorStore.Name = "labelNewVectorStore";
            labelNewVectorStore.Size = new Size(100, 23);
            labelNewVectorStore.TabIndex = 6;
            labelNewVectorStore.Text = "Or Enter New Name:";
            // 
            // progressBar1
            // 
            progressBar1.Dock = DockStyle.Bottom;
            progressBar1.Location = new Point(0, 427);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(800, 23);
            progressBar1.TabIndex = 7;
            // 
            // btnDeleteAllVSFiles
            // 
            btnDeleteAllVSFiles.BackColor = Color.Firebrick;
            btnDeleteAllVSFiles.Location = new Point(240, 60);
            btnDeleteAllVSFiles.Name = "btnDeleteAllVSFiles";
            btnDeleteAllVSFiles.Size = new Size(150, 30);
            btnDeleteAllVSFiles.TabIndex = 8;
            btnDeleteAllVSFiles.Text = "Delete All VS files";
            btnDeleteAllVSFiles.UseVisualStyleBackColor = false;
            btnDeleteAllVSFiles.Click += btnDeleteAllVSFiles_ClickAsync;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
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
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label labelSelectVectorStore;
        private Label labelNewVectorStore;
        private ProgressBar progressBar1;
        private Button btnDeleteAllVSFiles;
    }
}