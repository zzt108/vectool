using System.Text.Json;
using System.Configuration;

namespace oaiUI
{
    public class VectorStoreConfig
    {
        public List<string> FolderPaths { get; set; } = new List<string>();
        public List<string> ExcludedFiles { get; set; } = new List<string>();
        public List<string> ExcludedFolders { get; set; } = new List<string>();
    }

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
                    //_vectorStoreFolders = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)
                    //                      ?? new Dictionary<string, List<string>>();
                    _vectorStoreFolders = JsonSerializer.Deserialize<Dictionary<string, VectorStoreConfig>>(json)
                                          ?? new Dictionary<string, VectorStoreConfig>();
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