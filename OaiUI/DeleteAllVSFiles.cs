using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLogShared;
using LogCtxShared;
using OpenAI;

namespace oaiUI
{
    public partial class MainForm
    {
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
    }
}