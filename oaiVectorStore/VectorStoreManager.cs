using OpenAI.VectorStores;
using OpenAI;
using DocXHandler;
using LogCtxShared;
using NLogShared;
using System.Configuration;
using System.Text.Json;

namespace oaiVectorStore
{

    public class VectorStoreManager
    {
        private string _vectorStoreFoldersFilePath;
        private Dictionary<string, VectorStoreConfig> _vectorStoreFolders = new (); // not readonly
        private readonly VectorStoreConfig _vectorStoreConfig;
        private readonly IUserInterface _ui;

        public Dictionary<string, VectorStoreConfig>  Folders => _vectorStoreFolders;
        public VectorStoreConfig Config => _vectorStoreConfig;
        public VectorStoreManager(string vectorStoreFoldersFilePath, IUserInterface ui)
        {
            _vectorStoreConfig = VectorStoreConfig.FromAppConfig();
            _vectorStoreFoldersFilePath = vectorStoreFoldersFilePath;
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public async Task<string> CreateVectorStoreAsync(OpenAIClient api, string name, List<string> fileIds)
        {
            var createVectorStoreRequest = new CreateVectorStoreRequest(name);
            return await api.VectorStoresEndpoint.CreateVectorStoreAsync(createVectorStoreRequest);
        }

        public async Task<bool> DeleteVectorStoreAsync(string vectorStoreId)
        {
            using var api = new OpenAIClient();
            var isDeleted = await api.VectorStoresEndpoint.DeleteVectorStoreAsync(vectorStoreId);
            return isDeleted;
        }

        public async Task<Dictionary<string, string>> GetAllVectorStoresAsync(OpenAIClient api)
        {
            try
            {
                var vectorStores = await api.VectorStoresEndpoint.ListVectorStoresAsync();
                if (vectorStores?.Items != null)
                {
                    return vectorStores.Items.ToDictionary(vs => vs.Id, vs => vs.Name);
                }
                else
                {
                    return new Dictionary<string, string>();
                }
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }
        }

        public async Task AddFileToVectorStoreAsync(OpenAIClient api, string vectorStoreId, string fileId)
        {
            var file = await api.VectorStoresEndpoint.CreateVectorStoreFileAsync(vectorStoreId, fileId, new ChunkingStrategy(ChunkingStrategyType.Static));
        }

        public async Task AddFileToVectorStoreFromPathAsync(OpenAIClient api, string vectorStoreId, string filePath)
        {
            var fileId = await new FileStoreManager().UploadFileAsync(api, filePath);
            await AddFileToVectorStoreAsync(api, vectorStoreId, fileId);
        }

        public async Task<string> RetrieveFileFromFileStoreAsync(string vectorStoreId, string fileId)
        {
            using var api = new OpenAIClient();
            var file = await api.VectorStoresEndpoint.GetVectorStoreFileAsync(vectorStoreId, fileId);
            return file;
        }

        public async Task<bool> DeleteFileFromVectorStoreAsync(string vectorStoreId, string fileId)
        {
            using var api = new OpenAIClient();
            var isDeleted = await api.VectorStoresEndpoint.DeleteVectorStoreFileAsync(vectorStoreId, fileId);
            return isDeleted;
        }

        public async Task<bool> DeleteFileFromAllStoreAsync(OpenAIClient api, string vectorStoreId, string fileId)
        {
            try
            {
                var isDeleted = await api.VectorStoresEndpoint.DeleteVectorStoreFileAsync(vectorStoreId, fileId);
                if (isDeleted)
                {
                    isDeleted = await new FileStoreManager().DeleteFileFromFileStoreAsync(api, fileId);
                }
                return isDeleted;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("notfound"))
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task<List<string>> ListAllFiles(string vectorStoreId)
        {
            using var api = new OpenAIClient();
            return await ListAllFiles(api, vectorStoreId);
        }

        public async Task<List<string>> ListAllFiles(OpenAIClient api, string vectorStoreId)
        {
            ListResponse<VectorStoreFileResponse> files = await api.VectorStoresEndpoint.ListVectorStoreFilesAsync(vectorStoreId);
            return files.Items.Select(vs => vs.Id).ToList();

        }

        public async Task<string> RecreateVectorStore(string? vectorStoreName)
        {
            using var api = new OpenAIClient();

            var existingStores = await GetAllVectorStoresAsync(api);
            string vectorStoreId;

            if (existingStores.Values.Contains(vectorStoreName))
            {
                // If it exists, delete all files
                vectorStoreId = existingStores.First(s => s.Value == vectorStoreName).Key;
                await DeleteAllVSFiles(api, vectorStoreId);
            }
            else
            {
                if (string.IsNullOrEmpty(vectorStoreName))
                {
                    throw new ArgumentException(_vectorStoreFoldersFilePath, nameof(vectorStoreName));
                }
                // Create the vector store
                vectorStoreId = await CreateVectorStoreAsync(api, vectorStoreName, new List<string>());
                // When a new vector store is created, ensure it exists in the folder mapping
                if (!_vectorStoreFolders.ContainsKey(vectorStoreName))
                {
                    _vectorStoreFolders[vectorStoreName] = new VectorStoreConfig
                    {
                        ExcludedFiles = new List<string>(_vectorStoreConfig.ExcludedFiles), // Copy from global settings
                        ExcludedFolders = new List<string>(_vectorStoreConfig.ExcludedFolders)
                    };
                    SaveVectorStoreFolderData();
                }
            }

            return vectorStoreId;
        }

        public async Task DeleteAllVSFiles(OpenAIClient api, string vectorStoreId)
        {
            var fileIds = await ListAllFiles(api, vectorStoreId); // List file IDs to delete
            while (fileIds.Count > 0)
            {
                var totalFiles = fileIds.Count;

                int processedFiles = 0;
                _ui.UpdateProgress(processedFiles, totalFiles);

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
                    await DeleteFileFromAllStoreAsync(api, vectorStoreId, fileId);
                    processedFiles++;
                    _ui.UpdateProgress(processedFiles, totalFiles);
                }
                fileIds = await ListAllFiles(api, vectorStoreId); // List file IDs to delete
            }
        }

        public async Task UploadFiles(string vectorStoreId, List<string> selectedFolders)
        {
            var totalFolders = selectedFolders.Sum(folder =>
                Directory.GetDirectories(folder, "*", SearchOption.AllDirectories).Count());

            int processedFolders = 0;
            _ui.UpdateProgress(0, totalFolders);

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
                    _ui.UpdateProgress(processedFolders, totalFolders);
                    log.Debug(folder);

                    if (folder.Contains("\\.") || folder.Contains("\\obj") || folder.Contains("\\bin") || folder.Contains("\\packages"))
                    {
                        continue;
                    }

                    _ui.UpdateStatus(folder);

                    string relativePath = Path.GetRelativePath(rootFolder, folder).Replace('\\', '_');
                    string outputDocxPath = Path.Combine(folder, relativePath + ".docx");

                    try
                    {
                        var docXHandler = new DocXHandler.DocXHandler();
                        docXHandler.ConvertFilesToDocx(folder, outputDocxPath, _vectorStoreConfig);
                        string[] files = Directory.GetFiles(folder);

                        foreach (string file in files)
                        {
                            string fileName = Path.GetFileName(file);
                            if (_vectorStoreConfig.ExcludedFiles.Any(excludedFile => string.Equals(excludedFile, fileName, StringComparison.OrdinalIgnoreCase))) continue;
                            // Check MIME type and upload
                            string extension = Path.GetExtension(file);
                            if (MimeTypeProvider.GetMimeType(extension) == "application/octet-stream") // Skip unknown types
                            {
                                continue;
                            }

                            if (MimeTypeProvider.IsBinary(extension)) // non text types should be uploaded separately
                            {
                                log.Info($"Uploading {file}");
                                await AddFileToVectorStoreFromPathAsync(api, vectorStoreId, file);
                            }
                        }
                    }
                    finally
                    {
                        new FileInfo(outputDocxPath).Delete();
                    }
                }
            }
            _ui.ShowMessage("Files uploaded successfully.");
        }

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
                _ui.ShowMessage($"Error saving vector store folder data: {ex.Message}", "Saving Error", MessageType.Error);
            }
        }
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
                    _ui.ShowMessage($"Error loading vector store folder data: {ex.Message}", "Loading Error", MessageType.Warning);
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
