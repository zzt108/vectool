using System.Text.Json;
using System.Configuration;
using DocXHandler;

namespace oaiUI
{

    public partial class MainForm
    {
        // Load the vector store folder mapping from the JSON file
        public void LoadVectorStoreFolderData()
        {
            string vectorStoreFoldersPath = ConfigurationManager.AppSettings["vectorStoreFoldersPath"] ?? @"..\..\vectorStoreFolders.json";
            if (File.Exists(vectorStoreFoldersPath))
            {
                try
                {
                    string json = File.ReadAllText(vectorStoreFoldersPath);
                    _vectorStoreFolders = JsonSerializer.Deserialize<Dictionary<string, VectorStoreConfig>>(json)
                                          ?? new Dictionary<string, VectorStoreConfig>();

                    // Handle migration from old format (if needed)
                    if (_vectorStoreFolders.Count == 0)
                    {
                        var oldFormat = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
                        if (oldFormat != null)
                        {
                            foreach (var kvp in oldFormat)
                            {
                                _vectorStoreFolders[kvp.Key] = new VectorStoreConfig
                                {
                                    FolderPaths = kvp.Value,
                                    ExcludedFiles = new List<string>(_vectorStoreConfig.ExcludedFiles),
                                    ExcludedFolders = new List<string>(_vectorStoreConfig.ExcludedFolders)
                                };
                            }
                            SaveVectorStoreFolderData(); // Save in new format
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading vector store folder data: {ex.Message}", "Loading Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _vectorStoreFolders = new Dictionary<string, VectorStoreConfig>();
                }
            }
            else
            {
                _vectorStoreFolders = new Dictionary<string, VectorStoreConfig>();
            }
        }
    }
}