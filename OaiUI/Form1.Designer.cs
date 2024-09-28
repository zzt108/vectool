namespace oai
{
    partial class Form1
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
            txtFolderPath = new TextBox();
            btnSelectFolder = new Button();
            btnUpload = new Button();
            progressBar = new ProgressBar();
            txtQuestion = new TextBox();
            btnAskQuestion = new Button();
            txtAnswer = new TextBox();
            SuspendLayout();
            // 
            // txtFolderPath
            // 
            txtFolderPath.Location = new Point(14, 14);
            txtFolderPath.Margin = new Padding(4, 3, 4, 3);
            txtFolderPath.Name = "txtFolderPath";
            txtFolderPath.Size = new Size(442, 23);
            txtFolderPath.TabIndex = 0;
            // 
            // btnSelectFolder
            // 
            btnSelectFolder.Location = new Point(463, 12);
            btnSelectFolder.Margin = new Padding(4, 3, 4, 3);
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(106, 27);
            btnSelectFolder.TabIndex = 1;
            btnSelectFolder.Text = "Select Folder";
            btnSelectFolder.UseVisualStyleBackColor = true;
            btnSelectFolder.Click += btnSelectFolder_Click;
            // 
            // btnUpload
            // 
            btnUpload.Location = new Point(14, 44);
            btnUpload.Margin = new Padding(4, 3, 4, 3);
            btnUpload.Name = "btnUpload";
            btnUpload.Size = new Size(88, 27);
            btnUpload.TabIndex = 2;
            btnUpload.Text = "Upload";
            btnUpload.UseVisualStyleBackColor = true;
            btnUpload.Click += btnUpload_Click;
            // 
            // progressBar
            // 
            progressBar.Location = new Point(108, 44);
            progressBar.Margin = new Padding(4, 3, 4, 3);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(461, 27);
            progressBar.TabIndex = 3;
            // 
            // txtQuestion
            // 
            txtQuestion.Location = new Point(14, 77);
            txtQuestion.Margin = new Padding(4, 3, 4, 3);
            txtQuestion.Name = "txtQuestion";
            txtQuestion.Size = new Size(442, 23);
            txtQuestion.TabIndex = 4;
            // 
            // btnAskQuestion
            // 
            btnAskQuestion.Location = new Point(463, 75);
            btnAskQuestion.Margin = new Padding(4, 3, 4, 3);
            btnAskQuestion.Name = "btnAskQuestion";
            btnAskQuestion.Size = new Size(106, 27);
            btnAskQuestion.TabIndex = 5;
            btnAskQuestion.Text = "Ask Question";
            btnAskQuestion.UseVisualStyleBackColor = true;
            btnAskQuestion.Click += btnAskQuestion_Click;
            // 
            // txtAnswer
            // 
            txtAnswer.Location = new Point(14, 107);
            txtAnswer.Margin = new Padding(4, 3, 4, 3);
            txtAnswer.Multiline = true;
            txtAnswer.Name = "txtAnswer";
            txtAnswer.Size = new Size(555, 179);
            txtAnswer.TabIndex = 6;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(583, 301);
            Controls.Add(txtAnswer);
            Controls.Add(btnAskQuestion);
            Controls.Add(txtQuestion);
            Controls.Add(progressBar);
            Controls.Add(btnUpload);
            Controls.Add(btnSelectFolder);
            Controls.Add(txtFolderPath);
            Margin = new Padding(4, 3, 4, 3);
            Name = "Form1";
            Text = "Code Analyzer";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox txtFolderPath;
        private System.Windows.Forms.Button btnSelectFolder;
        private System.Windows.Forms.Button btnUpload;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.TextBox txtQuestion;
        private System.Windows.Forms.Button btnAskQuestion;
        private System.Windows.Forms.TextBox txtAnswer;
    }
}