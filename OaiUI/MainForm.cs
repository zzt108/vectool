﻿using LogCtxShared;
using oaiVectorStore;
using OpenAI;
using NLogShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
            using var api = new OpenAIClient();

            try
            {
                var vectorStores = await _vectorStoreManager.GetAllVectorStoresAsync(api);

                comboBoxVectorStores.DataSource = vectorStores.Values.ToArray();
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

            try
            {
                WorkStart("Delete VectorStore files");

                string selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();

                var existingStores = await _vectorStoreManager.GetAllVectorStoresAsync(api);

                if (existingStores.Any(s => s.Value == selectedVectorStore))
                {
                    var vectorStoreId = existingStores.First(s => s.Value == selectedVectorStore).Key;
                    await DeleteAllVSFiles(api, vectorStoreId);

                    var fileIds = await _vectorStoreManager.ListAllFiles(api, vectorStoreId);
                    toolStripStatusLabelInfo.Text = $"Files deleted successfully. Remaining:{fileIds.Count}";

                    MessageBox.Show($"Files deleted successfully. Remaining:{fileIds.Count}");
                }
                else
                {
                    MessageBox.Show("Please select a valid vector store to delete files from.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
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
/**************************************************************
content_copy
download
Use code with caution.
C#

Explanation of Changes:

_vectorStoreFolders and _vectorStoreFoldersFilePath:

A Dictionary<string, List<string>> _vectorStoreFolders is introduced to store the mapping between vector store names (strings) and their lists of selected folder paths.

string _vectorStoreFoldersFilePath = "vectorStoreFolders.json"; defines the path where this mapping will be saved as a JSON file. You can adjust this path as needed.

LoadVectorStoreFolderData():

This method is called when the MainForm is initialized.

It checks if the vectorStoreFolders.json file exists.

If it exists, it reads the JSON content and deserializes it into the _vectorStoreFolders dictionary.

It includes error handling in case the file is not found or the JSON is invalid.

SaveVectorStoreFolderData():

This method serializes the _vectorStoreFolders dictionary into JSON format.

It then writes the JSON data to the vectorStoreFolders.json file.

Error handling is included for potential file writing issues.

btnSelectFolders_Click() Modification:

After a folder is successfully selected and added to the selectedFolders list:

It checks if a vector store is currently selected in the comboBoxVectorStores.

If a vector store is selected, it retrieves the name of the selected vector store.

It updates the _vectorStoreFolders dictionary:

If the vector store name doesn't exist as a key, a new entry is created.

The newly selected folder path is added to the list of folders associated with that vector store.

Finally, SaveVectorStoreFolderData() is called to persist the changes to the JSON file.

btnClearFolders_Click() Modification:

When the "Empty Selected" button is clicked, and a vector store is selected, it clears the associated folder list in _vectorStoreFolders and saves the updated data.

comboBoxVectorStores_SelectedIndexChanged():

An event handler is added to the comboBoxVectorStores.SelectedIndexChanged event.

When the selected item in the combo box changes (i.e., a different vector store is selected):

It checks if a vector store is actually selected.

It calls the new method LoadSelectedFoldersForVectorStore() to load the folders associated with the newly selected vector store.

LoadSelectedFoldersForVectorStore():

This method takes the name of the vector store as input.

It clears the current selectedFolders list and the items in listBoxSelectedFolders.

It checks if the _vectorStoreFolders dictionary contains an entry for the given vectorStoreName.

If it does, it retrieves the list of folders associated with that vector store and populates the selectedFolders list and the listBoxSelectedFolders UI element.

RecreateVectorStore() Modification:

When a new vector store is created, the code now ensures that a corresponding entry (with an empty folder list) is added to the _vectorStoreFolders dictionary and saved. This handles the case where a new vector store has no associated folders initially.

To Use This Functionality:

Build and run your application.

Select a vector store from the dropdown or create a new one.

Click "Select Folders" to choose folders you want to associate with the selected vector store.

The selected folders will be displayed in the list box, and the association will be saved in the vectorStoreFolders.json file.

When you select a different vector store from the dropdown, the previously selected folders for that vector store will automatically load into the list box.

This implementation ensures that the selected folders for each vector store are persisted and automatically loaded, enhancing the user experience. Remember to place the vectorStoreFolders.json file in a location where your application has write access. A good place would be in the same directory as your application's executable or in the user's application data folder if you need more robust storage management.
*/