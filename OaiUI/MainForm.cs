using LogCtxShared;
using oaiVectorStore;
using OpenAI;
using NLogShared;
using System.Text.Json;

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

        // Store the mapping between vector store and selected folders
        private Dictionary<string, List<string>> _vectorStoreFolders = new Dictionary<string, List<string>>();
        private string _vectorStoreFoldersFilePath = "vectorStoreFolders.json"; // Path to save the mapping

        public MainForm()
        {
            InitializeComponent();
            _vectorStoreManager = new VectorStoreManager();
            LoadVectorStores();
            LoadVectorStoreFolderData(); // Load saved folder data on startup
            using var log = new CtxLogger();
            log.ConfigureXml("Config/LogConfig.xml");

            comboBoxVectorStores.SelectedIndexChanged += comboBoxVectorStores_SelectedIndexChanged;
        }

        // Load the vector store folder mapping from the JSON file
        private void LoadVectorStoreFolderData()
        {
            if (File.Exists(_vectorStoreFoldersFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_vectorStoreFoldersFilePath);
                    _vectorStoreFolders = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)
                                          ?? new Dictionary<string, List<string>>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading vector store folder data: {ex.Message}", "Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _vectorStoreFolders = new Dictionary<string, List<string>>();
                }
            }
            else
            {
                _vectorStoreFolders = new Dictionary<string, List<string>>();
            }
        }

        // Save the vector store folder mapping to the JSON file
        private void SaveVectorStoreFolderData()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_vectorStoreFolders, options);
                File.WriteAllText(_vectorStoreFoldersFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving vector store folder data: {ex.Message}", "Saving Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearFolders_Click(object sender, EventArgs e)
        {
            listBoxSelectedFolders.Items.Clear();
            selectedFolders.Clear();
            // Update the stored mapping when clearing folders
            if (comboBoxVectorStores.SelectedItem != null)
            {
                string selectedVectorStoreName = comboBoxVectorStores.SelectedItem.ToString();
                if (_vectorStoreFolders.ContainsKey(selectedVectorStoreName))
                {
                    _vectorStoreFolders[selectedVectorStoreName] = new List<string>();
                    SaveVectorStoreFolderData();
                }
            }
        }

        private async void LoadVectorStores()
        {
            // Load vector store folder data first
            LoadVectorStoreFolderData();

            // Try to load vector stores from OpenAI if the API key is available
            try
            {
                using var api = new OpenAIClient();
                try
                {
                    var vectorStores = await _vectorStoreManager.GetAllVectorStoresAsync(api);
                    // Merge loaded data with existing data, prioritizing data from the file
                    var combinedStores = _vectorStoreFolders.Keys.Union(vectorStores.Values).Distinct().ToList();
                    comboBoxVectorStores.DataSource = combinedStores;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading vector stores from OpenAI: {ex.Message}. Using local data.", "OpenAI Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch
            {
                MessageBox.Show("OpenAI API key is not configured. Cannot upload files.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnSelectFolders_Click(object sender, EventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select folders containing files to upload";
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderBrowserDialog.SelectedPath;
                    if (!selectedFolders.Contains(selectedPath))
                    {
                        selectedFolders.Add(selectedPath);
                        UpdateSelectedFoldersUI();

                        // Save the selected folder for the current vector store
                        if (comboBoxVectorStores.SelectedItem != null)
                        {
                            string selectedVectorStoreName = comboBoxVectorStores.SelectedItem.ToString();
                            if (!_vectorStoreFolders.ContainsKey(selectedVectorStoreName))
                            {
                                _vectorStoreFolders[selectedVectorStoreName] = new List<string>();
                            }
                            _vectorStoreFolders[selectedVectorStoreName].Add(selectedPath);
                            SaveVectorStoreFolderData();
                        }
                    }
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
                //else
                //{
                //    MessageBox.Show("OpenAI API key is not configured. Cannot upload files.", "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //}
            }
            finally
            {
                WorkFinish();
            }
        }

        private async Task UploadFiles(string vectorStoreId)
        {
            var totalFolders = selectedFolders.Sum(folder =>
                Directory.GetDirectories(folder, "*", SearchOption.AllDirectories).Count());

            processedFolders = 0;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = totalFolders;
            progressBar1.Value = 0;

            using var api = new OpenAIClient();
            using var log = new CtxLogger();
            log.ConfigureXml("Config/LogConfig.xml");
            var p = log.Ctx.Set(new Props()
                .Add("vectorStoreId", vectorStoreId)
                .Add("totalFolders", totalFolders)
                .Add("selectedFolders", selectedFolders.AsJson())
                );

            foreach (var rootFolder in selectedFolders)
            {
                var folders = Directory.GetDirectories(rootFolder, "*", SearchOption.AllDirectories);
                foreach (var folder in folders)
                {
                    processedFolders++;
                    UpdateProgress();
                    log.Debug(folder);

                    if (folder.Contains("\\.") || folder.Contains("\\obj") || folder.Contains("\\bin") || folder.Contains("\\packages"))
                    {
                        continue;
                    }

                    toolStripStatusLabelInfo.Text = folder;

                    string relativePath = Path.GetRelativePath(rootFolder, folder).Replace('\\', '_');
                    string outputDocxPath = Path.Combine(folder, relativePath + ".docx");

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

                            if (MimeTypeProvider.IsBinary(extension)) // non text types should be uploaded separately
                            {
                                log.Info($"Uploading {file}");
                                await _vectorStoreManager.AddFileToVectorStoreFromPathAsync(api, vectorStoreId, file);
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
                // When a new vector store is created, ensure it exists in the folder mapping
                if (!_vectorStoreFolders.ContainsKey(vectorStoreName))
                {
                    _vectorStoreFolders[vectorStoreName] = new List<string>();
                    SaveVectorStoreFolderData();
                }
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

                using var log = new CtxLogger();
                log.ConfigureXml("Config/LogConfig.xml");
                var p = log.Ctx.Set(new Props()
                    .Add("vectorStoreId", vectorStoreId)
                    .Add("totalFiles", totalFiles)
                    );

                log.Info($"Deleting from VS {vectorStoreId}");
                foreach (var fileId in fileIds)
                {
                    log.Info($"Deleting file {fileId}");
                    await _vectorStoreManager.DeleteFileFromAllStoreAsync(api, vectorStoreId, fileId);
                    processedFolders++;
                    UpdateProgress();
                }
                fileIds = await _vectorStoreManager.ListAllFiles(api, vectorStoreId); // List file IDs to delete
            }
        }

        private void btnConvertToDocx_Click(object sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                MessageBox.Show("Please select at least one folder first.", "No Folders Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Word Document|*.docx";
                saveFileDialog.Title = "Save DOCX File";
                saveFileDialog.DefaultExt = "docx";
                if (txtNewVectorStoreName.Text.Trim().Length > 0)
                { saveFileDialog.FileName = txtNewVectorStoreName.Text.Trim(); }
                else
                { saveFileDialog.FileName = comboBoxVectorStores.SelectedItem?.ToString(); }

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        btnConvertToDocx.Enabled = false;
                        DocXHandler.DocXHandler.ConvertSelectedFoldersToDocx(selectedFolders, saveFileDialog.FileName);
                        MessageBox.Show("Folders successfully converted to DOCX.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error converting folders to DOCX: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        btnConvertToDocx.Enabled = true;
                    }
                }
            }
        }

        private void btnConvertToMD_Click(object sender, EventArgs e)
        {
            if (selectedFolders.Count == 0)
            {
                MessageBox.Show("Please select at least one folder first.", "No Folders Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "MarkDown Document|*.md";
                saveFileDialog.Title = "Save MD File";
                saveFileDialog.DefaultExt = "md";
                if (txtNewVectorStoreName.Text.Trim().Length > 0)
                { saveFileDialog.FileName = txtNewVectorStoreName.Text.Trim(); }
                else
                { saveFileDialog.FileName = comboBoxVectorStores.SelectedItem?.ToString(); }

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        btnConvertToMd.Enabled = false;
                        DocXHandler.MDHandler.ExportSelectedFoldersToMarkdown(selectedFolders, saveFileDialog.FileName);
                        MessageBox.Show("Folders successfully converted to MD.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error converting folders to MD: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        btnConvertToMd.Enabled = true;
                    }
                }
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
            string selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedVectorStore))
            {
                MessageBox.Show("Please select a vector store.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                WorkStart($"Delete VectorStore files from {selectedVectorStore}");
                var existingStores = await _vectorStoreManager.GetAllVectorStoresAsync(api);
                if (existingStores.ContainsKey(selectedVectorStore))
                {
                    var vectorStoreId = existingStores.First(s => s.Key == selectedVectorStore).Key;
                    await DeleteAllVSFiles(api, vectorStoreId);
                    var fileIds = await _vectorStoreManager.ListAllFiles(api, vectorStoreId);
                    toolStripStatusLabelInfo.Text = $"Files deleted successfully. Remaining:{fileIds.Count}";
                    MessageBox.Show($"Files deleted successfully. Remaining:{fileIds.Count}");
                }
                else
                {
                    MessageBox.Show($"Vector store '{selectedVectorStore}' not found on OpenAI.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting files: {ex.Message}");
            }
        }

        private void btnUploadNew_Click(object sender, EventArgs e)
        {
            // Implementation for uploading new files (if needed)
        }

        private void comboBoxVectorStores_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxVectorStores.SelectedItem != null)
            {
                string selectedVectorStoreName = comboBoxVectorStores.SelectedItem.ToString();
                LoadSelectedFoldersForVectorStore(selectedVectorStoreName);
            }
        }

        private void LoadSelectedFoldersForVectorStore(string vectorStoreName)
        {
            selectedFolders.Clear();
            listBoxSelectedFolders.Items.Clear();

            if (_vectorStoreFolders.ContainsKey(vectorStoreName))
            {
                selectedFolders.AddRange(_vectorStoreFolders[vectorStoreName]);
                UpdateSelectedFoldersUI();
            }
        }
    }
}
