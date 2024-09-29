using oaiVectorStore;
using OpenAI_API;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace oaiUI
{
    public partial class MainForm : Form
    {
        private VectorStoreManager _vectorStoreManager;
        private List<string> selectedFolders = new List<string>();
        private ComboBox comboBoxVectorStores;
        private TextBox txtNewVectorStoreName;
        private Button btnSelectFolders;
        private ListBox listBoxSelectedFolders;
        private Button btnUploadFiles;
        private int processedFiles;

        public MainForm()
        {
            InitializeComponent();
            // Initialize your OpenAIClient with appropriate API key and base URL
            _vectorStoreManager = new VectorStoreManager();
            LoadVectorStores(); // Load existing vector stores into ComboBox
        }

        private void InitializeControls()
        {
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

        private async void LoadVectorStores()
        {
            try
            {
                var vectorStores = await _vectorStoreManager.GetAllVectorStoresAsync();

                comboBoxVectorStores.DataSource = vectorStores.Values.ToArray();
                //comboBoxVectorStores.DataSource = new BindingSource(vectorStores, null);
                //comboBoxVectorStores.DisplayMember = "Value";
                //comboBoxVectorStores.ValueMember = "Key";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading vector stores: {ex.Message}");
            }
        }

        private void btnSelectFolders_Click(object sender, EventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select folders containing files to upload";
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFolders.Add(folderBrowserDialog.SelectedPath);
                    // Update UI to show selected folders
                    UpdateSelectedFoldersUI();
                }
            }
        }

        private void UpdateSelectedFoldersUI()
        {
            listBoxSelectedFolders.Items.Clear();
            foreach (var folder in selectedFolders)
            {
                listBoxSelectedFolders.Items.Add(folder);
            }
        }

        private async void btnUploadFiles_Click(object sender, EventArgs e)
        {
            string newVectorStoreName = txtNewVectorStoreName.Text.Trim();
            string selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
            string vectorStoreName = string.IsNullOrEmpty(newVectorStoreName) ? selectedVectorStore : newVectorStoreName;


            try
            {
                string vectorStoreId = await RecreateVectorStore(vectorStoreName);

                // Upload files from all selected folders
                await UploadFiles(vectorStoreId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uploading files: {ex.Message}");
            }
        }

        private async Task UploadFiles(string vectorStoreId)
        {
            var totalFiles = selectedFolders.Sum(folder =>
                Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Count());

            processedFiles = 0;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = totalFiles;
            progressBar1.Value = 0;

            foreach (var folder in selectedFolders)
            {

                var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories).ToList();
                foreach (var file in files)
                {
                    processedFiles++;
                    UpdateProgress();

                    if (file.Contains("\\.") || file.Contains("\\obj") || file.Contains("\\bin"))
                    {
                        continue;
                    }

                    // Check MIME type and upload
                    string extension = Path.GetExtension(file);
                    if (MimeTypeProvider.GetMimeType(extension) != "application/octet-stream") // Skip unknown types
                    {
                        string? newExtension = MimeTypeProvider.GetNewExtension(extension);
                        if (newExtension is not null)
                        {
                            // Create a copy of the file with the new extension
                            string newFilePath = Path.ChangeExtension(file, newExtension);
                            File.Copy(file, newFilePath, true);

                            try
                            {
                                var mdTag = MimeTypeProvider.GetMdTag(extension);
                                if (mdTag != null)
                                {
                                    // Add start and end language tags to the file content
                                    string content = File.ReadAllText(newFilePath);
                                    content = $"```{mdTag}\n{content}\n```";
                                    File.WriteAllText(newFilePath, content);                                // Upload the new copy
                                }
                                await _vectorStoreManager.AddFileToVectorStoreFromPathAsync(vectorStoreId, newFilePath);
                            }
                            finally
                            {
                                // Delete the temporary new extension file
                                File.Delete(newFilePath);
                            }
                        }
                        else
                        {
                            // For files that do not have a new extension, upload as usual
                            await _vectorStoreManager.AddFileToVectorStoreFromPathAsync(vectorStoreId, file);
                        }
                    }
                }
            }

            MessageBox.Show("Files uploaded successfully.");
        }

        private async Task<string> RecreateVectorStore(string vectorStoreName)
        {
            var existingStores = await _vectorStoreManager.GetAllVectorStoresAsync();
            string vectorStoreId;

            if (existingStores.Values.Contains(vectorStoreName))
            {
                // If it exists, delete all files
                vectorStoreId = existingStores.First(s => s.Value == vectorStoreName).Key;
                await DeleteAllVSFiles(vectorStoreId);
            }
            else
            {
                // Create the vector store
                vectorStoreId = await _vectorStoreManager.CreateVectorStoreAsync(vectorStoreName, new List<string>());
            }

            return vectorStoreId;
        }

        private async Task DeleteAllVSFiles(string vectorStoreId)
        {
            var fileIds = await _vectorStoreManager.ListAllFiles(vectorStoreId); // List file IDs to delete
            var totalFiles = fileIds.Count;

            processedFiles = 0;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = totalFiles;
            progressBar1.Value = 0;
            foreach (var fileId in fileIds)
            {
                await _vectorStoreManager.DeleteFileFromAllStoreAsync(vectorStoreId, fileId);
                processedFiles++;
                UpdateProgress();
            }
        }

        private void UpdateProgress()
        {
            if (progressBar1.InvokeRequired)
            {
                progressBar1.Invoke(new Action(UpdateProgress));
            }
            else
            {
                progressBar1.Value = processedFiles;
                progressBar1.Update();
                Application.DoEvents();
            }
        }

        private async void btnDeleteAllVSFiles_ClickAsync(object sender, EventArgs e)
        {
            try
            {
                string selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();

                var existingStores = await _vectorStoreManager.GetAllVectorStoresAsync();


                var vectorStoreId = existingStores.First(s => s.Value == selectedVectorStore).Key;
                await DeleteAllVSFiles(vectorStoreId);


                // Upload files from all selected folders
                foreach (var folder in selectedFolders)
                {
                    if (folder.StartsWith('.'))
                    {
                        continue;
                    }

                    var files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories).ToList();
                    foreach (var file in files)
                    {
                        // Check MIME type and upload
                        string extension = Path.GetExtension(file);
                        if (MimeTypeProvider.GetMimeType(extension) != "application/octet-stream") // Skip unknown types
                        {
                            if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                            {
                                // Create a copy of the file with .txt extension
                                string txtFilePath = Path.ChangeExtension(file, ".cs.txt");
                                File.Copy(file, txtFilePath, true);

                                try
                                {
                                    // Upload the .txt copy
                                    await _vectorStoreManager.AddFileToVectorStoreFromPathAsync(vectorStoreId, txtFilePath);
                                }
                                finally
                                {
                                    // Delete the temporary .txt file
                                    File.Delete(txtFilePath);
                                }
                            }
                            else
                            {
                                // For non-.cs files, upload as usual
                                await _vectorStoreManager.AddFileToVectorStoreFromPathAsync(vectorStoreId, file);
                            }
                        }

                        // Update progress
                        processedFiles++;
                        UpdateProgress();

                    }
                }

                MessageBox.Show("Files deleted successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting files: {ex.Message}");
            }

        }
    }
}
