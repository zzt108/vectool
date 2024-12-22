using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace oaiUI
{
    public partial class MainForm
    {
        // Save the vector store folder mapping to the JSON file
        public void SaveVectorStoreFolderData()
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
    }
}