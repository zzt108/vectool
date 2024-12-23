using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenAI; // Assuming this is where OpenAIClient is defined
using LogCtxShared; // Assuming this is where CtxLogger and Props are defined

namespace oaiUI
{
    public partial class MainForm
    {
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
    }
}