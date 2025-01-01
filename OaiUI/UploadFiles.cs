using LogCtxShared; // Assuming this is where CtxLogger and Props are defined
using OpenAI;
using NLogShared;
using oaiVectorStore;

namespace oaiUI
{
    public partial class MainForm
    {
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
                .Add("selectedFolders", string.Join(", ", selectedFolders)) // Replacing AsJson with a simple join
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
                        var docXHandler = new DocXHandler.DocXHandler();
                        docXHandler.ConvertFilesToDocx(folder, outputDocxPath, _excludedFiles, _excludedFolders);
                        string[] files = Directory.GetFiles(folder);

                        foreach (string file in files)
                        {
                            string fileName = Path.GetFileName(file);
                            if (_excludedFiles.Any(excludedFile => string.Equals(excludedFile, fileName, StringComparison.OrdinalIgnoreCase))) continue;
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
    }
}
