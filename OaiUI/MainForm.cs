using oaiVectorStore;
using DocXHandler;
using OpenAI;
using SeriLogShared;
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
        private int processedFolders;

        public MainForm()
        {
            InitializeComponent();
            // Initialize your OpenAIClient with appropriate API key and base URL
            _vectorStoreManager = new VectorStoreManager();
            LoadVectorStores(); // Load existing vector stores into ComboBox
        }

        private void btnClearFolders_Click(object sender, EventArgs e)
        {
            listBoxSelectedFolders.Items.Clear();
            selectedFolders.Clear(); // Assuming you have a list to store the selected folders
        }

        private async void LoadVectorStores()
        {
            using var api = new OpenAIClient();

            try
            {
                var vectorStores = await _vectorStoreManager.GetAllVectorStoresAsync(api);

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

        void WorkStart(string str)
        {
            toolStripStatusLabelState.Text = str;
            btnDeleteAllVSFiles.Enabled = false;
            btnUploadFiles.Enabled = false;
            btnUploadNew.Enabled = false;
        }

        void WorkFinish()
        {
            toolStripStatusLabelState.Text = "Finished " + toolStripStatusLabelState.Text;
            btnDeleteAllVSFiles.Enabled = true;
            btnUploadFiles.Enabled = true;
            btnUploadNew.Enabled = true;
        }

        private async void btnUploadFiles_Click(object sender, EventArgs e)
        {

            try
            {
                WorkStart("Upload/Replace files");
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
            finally
            {
                WorkFinish();
            }
        }


        private async Task UploadFiles(string vectorStoreId)
        {
            using var log = new SeriLogCtx();
            var totalFolders = selectedFolders.Sum(folder =>
                Directory.GetDirectories(folder, "*", SearchOption.AllDirectories).Count());

            processedFolders = 0;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = totalFolders;
            progressBar1.Value = 0;

            using var api = new OpenAIClient();

            foreach (var rootFolder in selectedFolders)
            {
                var folders = Directory.GetDirectories(rootFolder, "*", SearchOption.AllDirectories);
                foreach (var folder in folders)
                {
                    processedFolders++;
                    UpdateProgress();

                    if (folder.Contains("\\.") || folder.Contains("\\obj") || folder.Contains("\\bin") || folder.Contains("\\packages"))
                    {
                        continue;
                    }

                    toolStripStatusLabelInfo.Text = folder;

                    // TODO create here the outputDocxPath by concatenating the difference between rootFolder and folder
                    // TODO replace \ with _ and append .docx

                    // Calculate the relative path from rootFolder to folder
                    string relativePath = Path.GetRelativePath(rootFolder, folder).Replace('\\', '_');

                    // Create the outputDocxPath by appending ".docx" to the relative path
                    string outputDocxPath = Path.Combine(folder, relativePath + ".docx");

                    // string outputDocxPath = Path.Combine(folder, "xxx.docx");
                    try
                    {
                        DocXHandler.DocXHandler.ConvertFilesToDocx(folder, outputDocxPath);
                        string[] files = Directory.GetFiles(folder);

                        foreach (string file in files)
                        {
                            // Check MIME type and upload
                            string extension = Path.GetExtension(file);
                            if (MimeTypeProvider.GetMimeType(extension) == "application/octet-stream") // Skip unknown types
                            {
                                continue;
                            }

                            if (MimeTypeProvider.GetMimeType(extension).StartsWith("application")) // non text types should be uploaded separately
                            {
                                await _vectorStoreManager.AddFileToVectorStoreFromPathAsync(api, vectorStoreId, outputDocxPath);
                            }
                        }
                    }
                    finally
                    {
                        new FileInfo(outputDocxPath).Delete();
                    }

                }

            }

            MessageBox.Show("Files uploaded successfully.");
        }

        private async Task StoreIndividualFiles(string vectorStoreId, OpenAIClient api, string folder)
        {
            var files = Directory.GetFiles(folder).ToList();
            foreach (var file in files)
            {


                // Check MIME type and upload
                string extension = Path.GetExtension(file);
                if (MimeTypeProvider.GetMimeType(extension) == "application/octet-stream") // Skip unknown types
                {
                    continue;
                }

                // Check if the file content is not empty
                if (new FileInfo(file).Length == 0)
                {
                    continue; // Skip empty files
                }

                toolStripStatusLabelInfo.Text = file;

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
                        try
                        {
                            await _vectorStoreManager.AddFileToVectorStoreFromPathAsync(api, vectorStoreId, newFilePath);
                        }
                        catch (Exception ex)
                        {
                            var response = MessageBox.Show(ex.Message, newFilePath, MessageBoxButtons.CancelTryContinue, MessageBoxIcon.Error);
                            if (response == DialogResult.Cancel)
                            {
                                throw;
                            }
                        }
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
                    try
                    {
                        await _vectorStoreManager.AddFileToVectorStoreFromPathAsync(api, vectorStoreId, file);
                    }
                    catch (Exception ex)
                    {
                        var response = MessageBox.Show(ex.Message, file, MessageBoxButtons.CancelTryContinue, MessageBoxIcon.Error);
                        if (response == DialogResult.Cancel)
                        {
                            throw;
                        }
                    }
                }

            }
        }

        private async Task<string> RecreateVectorStore(string vectorStoreName)
        {
            using var api = new OpenAIClient();

            var existingStores = await _vectorStoreManager.GetAllVectorStoresAsync(api);
            string vectorStoreId;

            if (existingStores.Values.Contains(vectorStoreName))
            {
                // If it exists, delete all files
                vectorStoreId = existingStores.First(s => s.Value == vectorStoreName).Key;
                await DeleteAllVSFiles(api, vectorStoreId);
            }
            else
            {
                // Create the vector store
                vectorStoreId = await _vectorStoreManager.CreateVectorStoreAsync(api, vectorStoreName, new List<string>());
            }

            return vectorStoreId;
        }

        private async Task DeleteAllVSFiles(OpenAIClient api, string vectorStoreId)
        {
            var fileIds = await _vectorStoreManager.ListAllFiles(api, vectorStoreId); // List file IDs to delete
            while (fileIds.Count > 0)
            {
                var totalFiles = fileIds.Count;

                processedFolders = 0;
                progressBar1.Minimum = 0;
                progressBar1.Maximum = totalFiles;
                progressBar1.Value = 0;
                foreach (var fileId in fileIds)
                {
                    await _vectorStoreManager.DeleteFileFromAllStoreAsync(api, vectorStoreId, fileId);
                    processedFolders++;
                    UpdateProgress();
                }
                fileIds = await _vectorStoreManager.ListAllFiles(api, vectorStoreId); // List file IDs to delete
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
                progressBar1.Value = processedFolders;
                progressBar1.Update();
                Application.DoEvents();
            }
            toolStripStatusLabelMax.Text = progressBar1.Maximum.ToString();
            toolStripStatusLabelCurrent.Text = progressBar1.Value.ToString();
        }

        private async void btnDeleteAllVSFiles_ClickAsync(object sender, EventArgs e)
        {
            using var api = new OpenAIClient();

            try
            {
                WorkStart("Delete VectorStore files");

                string selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();

                var existingStores = await _vectorStoreManager.GetAllVectorStoresAsync(api);

                var vectorStoreId = existingStores.First(s => s.Value == selectedVectorStore).Key;
                await DeleteAllVSFiles(api, vectorStoreId);

                var fileIds = await _vectorStoreManager.ListAllFiles(api, vectorStoreId); // List file IDs to delete
                toolStripStatusLabelInfo.Text = $"Files deleted successfully. Remaining:{fileIds.Count}";

                MessageBox.Show($"Files deleted successfully. Remaining:{fileIds.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting files: {ex.Message}");
            }
            finally
            {
                WorkFinish();
            }

        }
    }
}
