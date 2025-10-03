using NLogShared;
using OpenAI;
using OpenAI.VectorStores;
using System.IO;
using System.Linq;
using System.Text.Json;
using VecTool.Configuration;
using VecTool.Handlers;
using VecTool.Handlers.Traversal; // Added for FileValidator
using VecTool.RecentFiles;
using VecTool.Utils;

namespace oaiVectorStore;

public class VectorStoreManager
{
    private readonly string _vectorStoreFoldersFilePath;
    private Dictionary<string, VectorStoreConfig> _vectorStoreFolders = new();
    private readonly VectorStoreConfig _globalConfig;
    private readonly IUserInterface _ui;
    private readonly IRecentFilesManager _recentFilesManager;
    private readonly OpenAIClient _api;

    public Dictionary<string, VectorStoreConfig> Folders => _vectorStoreFolders;

    public VectorStoreManager(
        OpenAIClient api,
        string vectorStoreFoldersFilePath,
        VectorStoreConfig globalConfig,
        IUserInterface ui,
        IRecentFilesManager recentFilesManager)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _vectorStoreFoldersFilePath = vectorStoreFoldersFilePath ?? throw new ArgumentNullException(nameof(vectorStoreFoldersFilePath));
        _globalConfig = globalConfig ?? throw new ArgumentNullException(nameof(globalConfig));
        _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        _recentFilesManager = recentFilesManager ?? throw new ArgumentNullException(nameof(recentFilesManager));
    }

    public async Task<string> CreateVectorStoreAsync(string name)
    {
        var request = new CreateVectorStoreRequest(name);
        var vectorStore = await _api.VectorStoresEndpoint.CreateVectorStoreAsync(request);
        return vectorStore.Id;
    }

    public async Task<bool> DeleteVectorStoreAsync(string vectorStoreId)
    {
        return await _api.VectorStoresEndpoint.DeleteVectorStoreAsync(vectorStoreId);
    }

    public async Task<Dictionary<string, string>> GetAllVectorStoresAsync()
    {
        try
        {
            var vectorStores = await _api.VectorStoresEndpoint.ListVectorStoresAsync();
            return vectorStores?.Items?.ToDictionary(vs => vs.Id, vs => vs.Name) ?? new Dictionary<string, string>();
        }
        catch (Exception)
        {
            return new Dictionary<string, string>();
        }
    }

    public async Task AddFileToVectorStoreAsync(string vectorStoreId, string fileId)
    {
        await _api.VectorStoresEndpoint.CreateVectorStoreFileAsync(vectorStoreId, fileId);
    }

    public async Task<string> AddFileToVectorStoreFromPathAsync(string vectorStoreId, string filePath)
    {
        var fileId = await new FileStoreManager().UploadFileAsync(_api, filePath);
        await AddFileToVectorStoreAsync(vectorStoreId, fileId);
        return fileId;
    }

    public async Task<bool> DeleteFileFromAllStoresAsync(string vectorStoreId, string fileId)
    {
        var isDeletedInVs = await _api.VectorStoresEndpoint.DeleteVectorStoreFileAsync(vectorStoreId, fileId);
        var isDeletedInFs = await new FileStoreManager().DeleteFileFromFileStoreAsync(_api, fileId);
        return isDeletedInVs && isDeletedInFs;
    }

    public async Task<List<string>> ListAllFilesAsync(string vectorStoreId)
    {
        var files = await _api.VectorStoresEndpoint.ListVectorStoreFilesAsync(vectorStoreId);
        return files.Items.Select(vs => vs.Id).ToList();
    }

    public async Task<string> RecreateVectorStoreAsync(string vectorStoreName)
    {
        var existingStores = await GetAllVectorStoresAsync();
        string vectorStoreId;

        if (existingStores.ContainsValue(vectorStoreName))
        {
            vectorStoreId = existingStores.First(s => s.Value == vectorStoreName).Key;
            await DeleteAllVSFilesAsync(vectorStoreId);
        }
        else
        {
            if (string.IsNullOrEmpty(vectorStoreName))
                throw new ArgumentException("Vector store name is required.", nameof(vectorStoreName));
            vectorStoreId = await CreateVectorStoreAsync(vectorStoreName);
        }

        if (!_vectorStoreFolders.ContainsKey(vectorStoreName))
        {
            // Correct way to initialize config with read-only properties
            var newConfig = new VectorStoreConfig();
            newConfig.ExcludedFileNameParts.AddRange(_globalConfig.ExcludedFileNameParts);
            newConfig.ExcludedFolderNames.AddRange(_globalConfig.ExcludedFolderNames);
            newConfig.ExcludedExtensions.AddRange(_globalConfig.ExcludedExtensions);

            _vectorStoreFolders[vectorStoreName] = newConfig;
            SaveVectorStoreFolderData();
        }
        return vectorStoreId;
    }

    public async Task DeleteAllVSFilesAsync(string vectorStoreId)
    {
        try
        {
            var fileIds = await ListAllFilesAsync(vectorStoreId);
            _ui.WorkStart($"Deleting files from VS {vectorStoreId}", fileIds.Select(f => "").ToList());
            int processedFiles = 0;

            foreach (var fileId in fileIds)
            {
                await DeleteFileFromAllStoresAsync(vectorStoreId, fileId);
                processedFiles++;
                _ui.UpdateProgress(processedFiles);
            }
        }
        finally
        {
            _ui.WorkFinish();
        }
    }

    public async Task UploadFilesAsync(string vectorStoreId, string folderPath, VectorStoreConfig config)
    {
        int processedFolders = 0;
        _ui.UpdateProgress(0);

        var folders = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories)
            .Concat(new[] { folderPath });

        foreach (var folder in folders)
        {
            processedFolders++;
            _ui.UpdateProgress(processedFolders);

            if (config.IsFolderExcluded(Path.GetFileName(folder))) continue;

            _ui.UpdateStatus(folder);

            string[] files;
            try
            {
                files = Directory.GetFiles(folder);
            }
            catch { continue; }

            foreach (string file in files)
            {
                if (config.IsFileExcluded(Path.GetFileName(file))) continue;

                var mimeProvider = new MimeTypeProvider();
                var fileExtension = Path.GetExtension(file);

                // Corrected binary check
                if (mimeProvider.GetMimeType(fileExtension) == "application/octet-stream" || FileValidator.IsBinaryExtension(fileExtension))
                {
                    continue;
                }
                await AddFileToVectorStoreFromPathAsync(vectorStoreId, file);
            }
        }
        _ui.ShowMessage("Files uploaded successfully.", "Upload", MessageType.Information);
    }

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
            _ui.ShowMessage($"Error saving vector store folder data: {ex.Message}", "Saving Error", MessageType.Error);
        }
    }

    public void LoadVectorStoreFolderData()
    {
        if (File.Exists(_vectorStoreFoldersFilePath))
        {
            try
            {
                string json = File.ReadAllText(_vectorStoreFoldersFilePath);
                var loadedData = JsonSerializer.Deserialize<Dictionary<string, VectorStoreConfig>>(json);
                _vectorStoreFolders = loadedData ?? new Dictionary<string, VectorStoreConfig>();
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
