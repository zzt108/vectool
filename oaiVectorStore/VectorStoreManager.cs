namespace oaiVectorStore
{
    using OpenAI.VectorStores;
    using OpenAI;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class VectorStoreManager
    {

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

        //public async Task AddFileToVectorStoreAsync(OpenAIClient api, string vectorStoreId, string fileId)
        //{
        //    var file = await api.VectorStoresEndpoint.CreateVectorStoreFileAsync(vectorStoreId, fileId, new ChunkingStrategy(ChunkingStrategyType.Static));
        //}

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
    }
}
