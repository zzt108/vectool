﻿using oaiVectorStore;
using OpenAI;
using NLogShared;
using System.Configuration;

namespace oaiUI
{
    public partial class MainForm : Form
    {
        private VectorStoreManager _vectorStoreManager;
        private List<string> selectedFolders = new List<string>();
        private ComboBox comboBoxVectorStores = null!;
        private TextBox txtNewVectorStoreName;
        private Button btnSelectFolders;
        private ListBox listBoxSelectedFolders;
        private Button btnUploadFiles;
        private Button btnDeleteVectorStoreAssoc = null!; // Declaration for the new button

        private void btnDeleteVectorStoreAssoc_Click(object sender, EventArgs e)
        {
            string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedVectorStore))
            {
                MessageBox.Show("Please select a vector store.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Remove the association from the dictionary
            if (_vectorStoreFolders.ContainsKey(selectedVectorStore))
            {
                _vectorStoreFolders.Remove(selectedVectorStore);

                // Update the UI (remove from combobox and clear selected folders)
                var currentDataSource = comboBoxVectorStores.DataSource as List<string>;
                if (currentDataSource != null)
                {
                    currentDataSource.Remove(selectedVectorStore);
                    comboBoxVectorStores.DataSource = null; // Temporarily detach
                    comboBoxVectorStores.DataSource = currentDataSource; // Reattach
                }
                comboBoxVectorStores.SelectedItem = null;
                selectedFolders.Clear();
                listBoxSelectedFolders.Items.Clear();

                // Save the updated data to the JSON file
                SaveVectorStoreFolderData();

                MessageBox.Show($"Folder associations for vector store '{selectedVectorStore}' deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"No folder associations found for vector store '{selectedVectorStore}'.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private int processedFolders;

        // Store the mapping between vector store and selected folders
        private Dictionary<string, List<string>> _vectorStoreFolders = new Dictionary<string, List<string>>();
        private string _vectorStoreFoldersFilePath; // Path to save the mapping
        private List<string> _excludedFiles; // Added for excluded files

        public MainForm()
        {
            InitializeComponent();
            // Initialize non-nullable fields
            txtNewVectorStoreName = new TextBox();
            btnSelectFolders = new Button();
            listBoxSelectedFolders = new ListBox();
            btnUploadFiles = new Button();
            _excludedFiles = new List<string>();
            _excludedFolders = new List<string>();
            
            LoadExcludedFilesConfig();
            LoadExcludedFoldersConfig(); // Add this line
            _vectorStoreFoldersFilePath = ConfigurationManager.AppSettings["vectorStoreFoldersPath"] ?? @"..\..\vectorStoreFolders.json";
            _vectorStoreManager = new VectorStoreManager();
            LoadVectorStores();
            LoadVectorStoreFolderData(); // Load saved folder data on startup
            using var log = new CtxLogger();
            log.ConfigureXml("Config/LogConfig.xml");

            comboBoxVectorStores.SelectedIndexChanged += comboBoxVectorStores_SelectedIndexChanged;
        }

        private List<string> _excludedFolders; // Add this field

        private void LoadExcludedFoldersConfig()
        {
            string? excludedFoldersConfig = ConfigurationManager.AppSettings["excludedFolders"];
            if (!string.IsNullOrEmpty(excludedFoldersConfig))
            {
                _excludedFolders = excludedFoldersConfig.Split(',').Select(f => f.Trim()).ToList();
            }
            else
            {
                _excludedFolders = new List<string>(); // Initialize as empty list if not configured
            }
        }

        private void LoadExcludedFilesConfig()
        {
            string? excludedFilesConfig = ConfigurationManager.AppSettings["excludedFiles"];
            if (!string.IsNullOrEmpty(excludedFilesConfig))
            {
                _excludedFiles = excludedFilesConfig.Split(',').Select(f => f.Trim()).ToList();
            }
        }

        private void btnClearFolders_Click(object sender, EventArgs e)
        {
            listBoxSelectedFolders.Items.Clear();
            selectedFolders.Clear();
            // Update the stored mapping when clearing folders
            if (comboBoxVectorStores.SelectedItem != null)
            {
                string selectedVectorStoreName = comboBoxVectorStores.SelectedItem.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(selectedVectorStoreName) && _vectorStoreFolders.ContainsKey(selectedVectorStoreName))
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
                    // and removing any entries from OpenAI that are in the file.
                    var combinedStores = vectorStores.Values
                        .Where(v => !_vectorStoreFolders.ContainsKey(v))
                        .Union(_vectorStoreFolders.Keys)
                        .Distinct()
                        .ToList();

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

        private string? GetVectorStoreName()
        {
            // Get the vector store name
            string newVectorStoreName = txtNewVectorStoreName.Text.Trim();
            string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
            string? vectorStoreName = string.IsNullOrEmpty(newVectorStoreName) ? selectedVectorStore : newVectorStoreName;

            // Modify the underlying data source
            var currentDataSource = comboBoxVectorStores.DataSource as List<string>;
            if (currentDataSource != null && !string.IsNullOrEmpty(newVectorStoreName) && !currentDataSource.Contains(newVectorStoreName))
            {
                currentDataSource.Add(newVectorStoreName);
                comboBoxVectorStores.DataSource = null; // Temporarily detach the data source
                comboBoxVectorStores.DataSource = currentDataSource; // Reattach the updated data source
                comboBoxVectorStores.SelectedItem = newVectorStoreName; // Select the new item
            }
            else if (!string.IsNullOrEmpty(selectedVectorStore))
            {
                comboBoxVectorStores.SelectedItem = selectedVectorStore;
            }

            // Clear the new vector store name textbox if a new name was used
            if (!string.IsNullOrEmpty(newVectorStoreName))
            {
                txtNewVectorStoreName.Text = "";
            }

            return vectorStoreName;
        }

        private void btnSelectFolders_Click(object sender, EventArgs e)
        {
            var vectorStoreName = GetVectorStoreName();
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
                        if (!string.IsNullOrEmpty(vectorStoreName))
                        {
                            if (!_vectorStoreFolders.ContainsKey(vectorStoreName))
                            {
                                _vectorStoreFolders[vectorStoreName] = new List<string>();
                            }
                            _vectorStoreFolders[vectorStoreName].Add(selectedPath);
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
                string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
                string? vectorStoreName = string.IsNullOrEmpty(newVectorStoreName) ? selectedVectorStore : newVectorStoreName;

                try
                {
                    string? vectorStoreId = await RecreateVectorStore(vectorStoreName);

                    // Add new vector store name to the combo box if it's not already there
                    if (!string.IsNullOrEmpty(newVectorStoreName) && !comboBoxVectorStores.Items.Contains(newVectorStoreName))
                    {
                        comboBoxVectorStores.Items.Add(newVectorStoreName);
                    }

                    // Select the vector store in the combo box
                    comboBoxVectorStores.SelectedItem = vectorStoreName;

                    // Clear the new vector store name textbox
                    txtNewVectorStoreName.Text = "";

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
                        var docXHandler = new DocXHandler.DocXHandler();
                        docXHandler.ConvertSelectedFoldersToDocx(selectedFolders, saveFileDialog.FileName, _excludedFiles, _excludedFolders);
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
                        var mdHandler = new DocXHandler.MDHandler();
                        mdHandler.ExportSelectedFolders(selectedFolders, saveFileDialog.FileName, _excludedFiles, _excludedFolders);
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
            string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
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

private void comboBoxVectorStores_SelectedIndexChanged(object? sender, EventArgs e)
{
    if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
    {
        string? selectedVectorStoreName = comboBox.SelectedItem.ToString();
        if (!string.IsNullOrEmpty(selectedVectorStoreName))
        {
            LoadSelectedFoldersForVectorStore(selectedVectorStoreName);
        }
    }
}

        private void LoadSelectedFoldersForVectorStore(string? vectorStoreName)
        {
            if (!string.IsNullOrEmpty(vectorStoreName))
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
}
