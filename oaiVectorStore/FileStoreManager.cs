namespace oaiVectorStore
{
    using OpenAI;
    using OpenAI.Files;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class FileStoreManager
    {

        // Delete File from File Store
        public async Task<bool> DeleteFileFromFileStoreAsync(OpenAIClient api, string fileId)
        {
            var isDeleted = await api.FilesEndpoint.DeleteFileAsync(fileId);
            return isDeleted;
        }

        public async Task<string> UploadFileAsync(OpenAIClient api, string filePath)
        {
            var file = await api.FilesEndpoint.UploadFileAsync(filePath, FilePurpose.Assistants);
            return file.Id;
        }

    }
}
