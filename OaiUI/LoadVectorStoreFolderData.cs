using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace oaiUI
{
    public partial class MainForm
    {
        // Load the vector store folder mapping from the JSON file
        public void LoadVectorStoreFolderData()
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
    }
}