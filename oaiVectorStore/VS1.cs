using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace oaiVectorStore
{

    public class OpenAIVectorStoreClient
    {
        private readonly HttpClient httpClient;
        private readonly string apiKey;
        private const string baseUrl = "https://api.openai.com";

        public OpenAIVectorStoreClient(string apiKey)
        {
            this.apiKey = apiKey;
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public async Task<string> CreateVectorStoreAsync(string name, IEnumerable<string> fileIds)
        {
            var requestContent = new
            {
                name = name,
                file_ids = fileIds
            };

            var response = await httpClient.PostAsync(
                $"{baseUrl}/v1/vector_stores",
                new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<dynamic>(responseContent);

            return responseObject.id;  // assuming the response has an 'id' field
        }

        public async Task AddFilesToVectorStoreAsync(string vectorStoreId, IEnumerable<string> fileIds)
        {
            var requestContent = new
            {
                file_ids = fileIds
            };

            var response = await httpClient.PostAsync(
                $"{baseUrl}/v1/vector_stores/{vectorStoreId}/files",
                new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();
        }

        public async Task<List<string>> GetFilesInVectorStoreAsync(string vectorStoreId)
        {
            var response = await httpClient.GetAsync(
                $"{baseUrl}/v1/vector_stores/{vectorStoreId}/files"
            );

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonConvert.DeserializeObject<dynamic>(responseContent);

            List<string> fileIds = new List<string>();

            foreach (var file in responseObject.files)
            {
                fileIds.Add((string)file.id);
            }

            return fileIds;
        }
        
        public async Task<bool> DeleteFileFromVectorStoreAsync(string vectorStoreId, string fileId)
        {
            var response = await httpClient.DeleteAsync($"vector_stores/{vectorStoreId}/files/{fileId}");

            return response.IsSuccessStatusCode;
        }

        public async Task AttachVectorStoreToAssistantAsync(string assistantId, string vectorStoreId)
        {
            var requestContent = new
            {
                tool_resources = new
                {
                    file_search = new
                    {
                        vector_store_ids = new[] { vectorStoreId }
                    }
                }
            };

            var response = await httpClient.PatchAsync(
                $"{baseUrl}/v1/assistants/{assistantId}",
                new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();
        }

        public async Task AttachVectorStoreToThreadAsync(string threadId, string vectorStoreId)
        {
            var requestContent = new
            {
                tool_resources = new
                {
                    file_search = new
                    {
                        vector_store_ids = new[] { vectorStoreId }
                    }
                }
            };

            var response = await httpClient.PatchAsync(
                $"{baseUrl}/v1/threads/{threadId}",
                new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();
        }
    }
}
