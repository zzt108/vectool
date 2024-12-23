using System.Text.Json;
using System.Configuration;

namespace oaiUI
{
    public partial class MainForm
    {
        // Save the vector store folder mapping to the JSON file
        public void SaveVectorStoreFolderData()
        {
            string vectorStoreFoldersPath = ConfigurationManager.AppSettings["vectorStoreFoldersPath"] ?? @"..\..\vectorStoreFolders.json";
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_vectorStoreFolders, options);
                File.WriteAllText(vectorStoreFoldersPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving vector store folder data: {ex.Message}", "Saving Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}