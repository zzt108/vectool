using LogCtxShared;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Files;
using OpenAI.VectorStores;
using VecTool.Configuration.Helpers;
using VecTool.Configuration.Logging;

namespace VecTool.VectorStore.OpenAI
{
    /// <summary>
    /// Manages OpenAI Vector Store operations (create, upload, list).
    /// Restored from legacy v1.25 implementation.
    /// </summary>
    public sealed class OpenAIVectorStoreManager : IVectorStoreProvider, IDisposable
    {
        private static readonly ILogger log = AppLogger.For<OpenAIVectorStoreManager>();
        private readonly OpenAIClient client;
        private bool disposed;

        public OpenAIVectorStoreManager(string apiKey)
        {
            apiKey.ThrowIfNullOrWhiteSpace(nameof(apiKey), log, "OpenAI API key is required.");

            using var ctx = log.SetContext().Add("hasApiKey", !string.IsNullOrWhiteSpace(apiKey));

            // OpenAI-DotNet 8.4.1: new OpenAIClient(OpenAIAuthentication)
            var auth = new OpenAIAuthentication(apiKey);
            client = new OpenAIClient(auth);

            log.LogInformation("OpenAI Vector Store Manager initialized");
        }

        public async Task<string> CreateVectorStoreAsync(string name)
        {
            name.ThrowIfNullOrWhiteSpace(nameof(name), log, "Vector store name is required.");

            using var ctx = log.SetContext().Add("storeName", name);

            try
            {
                log.LogInformation("Creating vector store");

                var request = new CreateVectorStoreRequest(name);
                var response = await client.VectorStoresEndpoint.CreateVectorStoreAsync(request).ConfigureAwait(false);

                if (response == null || string.IsNullOrWhiteSpace(response.Id))
                {
                    var ex = new InvalidOperationException("OpenAI returned null or empty vector store ID.");
                    log.LogError(ex, "Failed to create vector store");
                    throw ex;
                }

                log.LogInformation("Vector store created with ID {VectorStoreId}", response.Id);
                return response.Id;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to create vector store");
                throw;
            }
        }

        public async Task<bool> UploadFileAsync(string vectorStoreId, string filePath)
        {
            vectorStoreId.ThrowIfNullOrWhiteSpace(nameof(vectorStoreId), log, "Vector store ID is required.");
            filePath.ThrowIfNullOrWhiteSpace(nameof(filePath), log, "File path is required.");

            using var ctx = log.SetContext()
                .Add("vectorStoreId", vectorStoreId)
                .Add("filePath", filePath);

            if (!File.Exists(filePath))
            {
                var ex = new FileNotFoundException("File not found", filePath);
                log.LogError(ex, "Upload file not found");
                throw ex;
            }

            try
            {
                log.LogInformation("Uploading file to vector store");

                // Step 1: Upload file to OpenAI Files API
                // OpenAI-DotNet 8.4.1: UploadFileAsync(string filePath, string purpose, CancellationToken)
                var fileResponse = await client.FilesEndpoint
                    .UploadFileAsync(filePath, "assistants")
                    .ConfigureAwait(false);

                if (fileResponse == null || string.IsNullOrWhiteSpace(fileResponse.Id))
                {
                    var ex = new InvalidOperationException("OpenAI returned null or empty file ID.");
                    log.LogError(ex, "Failed to upload file");
                    throw ex;
                }

                log.LogInformation("File uploaded to OpenAI with ID {FileId}", fileResponse.Id);

                // Step 2: Attach file to vector store
                // OpenAI-DotNet 8.4.1: CreateVectorStoreFileAsync(vectorStoreId, fileId)
                var attachResponse = await client.VectorStoresEndpoint
                    .CreateVectorStoreFileAsync(vectorStoreId, fileResponse.Id)
                    .ConfigureAwait(false);

                if (attachResponse == null)
                {
                    log.LogWarning("File attached but response was null");
                    return false;
                }

                log.LogInformation("File {FileId} attached to vector store with status {Status}",
                    fileResponse.Id, attachResponse.Status);

                return true;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to upload file");
                throw;
            }
        }

        public async Task<List<VectorStoreInfo>> ListVectorStoresAsync()
        {
            using var ctx = log.SetContext().Add("operation", "listVectorStores");

            try
            {
                log.LogInformation("Listing vector stores");

                var response = await client.VectorStoresEndpoint.ListVectorStoresAsync().ConfigureAwait(false);

                if (response == null || response.Items == null)
                {
                    log.LogWarning("No vector stores found or null response");
                    return new List<VectorStoreInfo>();
                }

                var stores = response.Items
                    .Select(vs => new VectorStoreInfo
                    {
                        Id = vs.Id,
                        Name = vs.Name ?? "Unnamed",
                        CreatedAt = DateTimeOffset.FromUnixTimeSeconds(vs.CreatedAtUnixTimeSeconds).DateTime,
                        FileCount = vs.FileCounts?.Total ?? 0
                    })
                    .ToList();

                log.LogInformation("Retrieved {StoreCount} vector stores", stores.Count);
                return stores;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to list vector stores");
                throw;
            }
        }

        public void Dispose()
        {
            if (disposed) return;

            // OpenAIClient doesn't implement IDisposable in v8.4.1
            // but we maintain the pattern for future compatibility
            disposed = true;
            log.LogInformation("OpenAI Vector Store Manager disposed");
        }
    }
}