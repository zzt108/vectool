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
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Text = "Main Form";
            // ComboBox for existing vector stores
            comboBoxVectorStores = new ComboBox
            {
                Name = "comboBoxVectorStores",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(200, 20)
            };
            Controls.Add(comboBoxVectorStores);

            // TextBox for new vector store name
            txtNewVectorStoreName = new TextBox
            {
                Name = "txtNewVectorStoreName",
                Location = new System.Drawing.Point(240, 20),
                Size = new System.Drawing.Size(150, 20)
            };
            Controls.Add(txtNewVectorStoreName);

            // Button to select folders
            btnSelectFolders = new Button
            {
                Name = "btnSelectFolders",
                Text = "Select Folders",
                Location = new System.Drawing.Point(20, 60),
                Size = new System.Drawing.Size(100, 30)
            };
            btnSelectFolders.Click += btnSelectFolders_Click;
            Controls.Add(btnSelectFolders);

            // ListBox for displaying selected folders
            listBoxSelectedFolders = new ListBox
            {
                Name = "listBoxSelectedFolders",
                Location = new System.Drawing.Point(20, 100),
                Size = new System.Drawing.Size(370, 100)
            };
            Controls.Add(listBoxSelectedFolders);

            // Button to upload files
            btnUploadFiles = new Button
            {
                Name = "btnUploadFiles",
                Text = "Upload Files",
                Location = new System.Drawing.Point(20, 220),
                Size = new System.Drawing.Size(100, 30)
            };
            btnUploadFiles.Click += btnUploadFiles_Click;
            Controls.Add(btnUploadFiles);

            // Label for the ComboBox
            Label labelSelectVectorStore = new Label
            {
                Text = "Select Existing Vector Store:",
                Location = new System.Drawing.Point(20, 0)
            };
            Controls.Add(labelSelectVectorStore);

            // Label for the TextBox
            Label labelNewVectorStore = new Label
            {
                Text = "Or Enter New Name:",
                Location = new System.Drawing.Point(240, 0)
            };
            Controls.Add(labelNewVectorStore);
        }

        #endregion
    }
}