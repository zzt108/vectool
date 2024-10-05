using System.Linq;
using System.Text;
/*
 File format	MIME type
.c	text/x-c
.cpp	text/x-c++
.cs	text/x-csharp
.css	text/css
.doc	application/msword
.docx	application/vnd.openxmlformats-officedocument.wordprocessingml.document
.go	text/x-golang
.html	text/html
.java	text/x-java
.js	text/javascript
.json	application/json
.md	text/markdown
.pdf	application/pdf
.php	text/x-php
.pptx	application/vnd.openxmlformats-officedocument.presentationml.presentation
.py	text/x-python
.py	text/x-script.python
.rb	text/x-ruby
.sh	application/x-sh
.tex	text/x-tex
.ts	application/typescript
.txt	text/plain 
 */

namespace oaiVectorStore
{
    using OpenAI.VectorStores;
    using OpenAI;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Text;
    using System.Text.Json;
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
            var vectorStores = await api.VectorStoresEndpoint.ListVectorStoresAsync();
            return vectorStores.Items.ToDictionary(vs => vs.Id, vs => vs.Name);
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
                /*
Error deleting files: DeleteFileAsync Failed! HTTP status code: NotFound | Response body: {
  "error": {
    "message": "No such File object: file-QpBv6FnCOpRblkhDfmwG4XN8",
    "type": "invalid_request_error",
    "param": "id",
    "code": null
  }
}

                 */
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


        //public async Task<List<string>> RetrieveAllFilesInVectorStoreAsync(OpenAIClient api, string vectorStoreId)
        //{
        //    List<string> allFileIds = new List<string>();
        //    int offset = 0;
        //    int limit = 20; // Adjust this limit based on your requirements
        //    ListResponse<VectorStoreFileResponse> response;

        //    do
        //    {
        //        response = await api.VectorStoresEndpoint.ListVectorStoreFilesAsync(vectorStoreId, limit: limit, offset: offset);
        //        allFileIds.AddRange(response.Items.Select(vs => vs.Id));
        //        offset += limit; // Move to the next set of files
        //    }
        //    while (response.Items.Count > 0); // Continue until there are no more files

        //    return allFileIds;
        //}

    }
}
