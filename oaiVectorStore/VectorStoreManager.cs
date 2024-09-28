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
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class VectorStoreManager
    {
        private readonly OpenAIClient _openAIClient;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public VectorStoreManager(OpenAIClient openAIClient)
        {
            _httpClient = openAIClient.HttpClient;
            _baseUrl = openAIClient.BaseUrl;
        }


        public VectorStoreManager(string apiKey, string baseUrl)
        {
            _openAIClient = new OpenAIClient(apiKey, baseUrl);
            _httpClient = _openAIClient.HttpClient;
            _baseUrl = _openAIClient.BaseUrl;
        }

        public async Task<string> CreateVectorStoreAsync(string name, List<string> fileIds)
        {
            var payload = new { name, file_ids = fileIds };
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/vector_stores", payload);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error creating vector store: {response.ReasonPhrase}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);
            return responseData?["id"]?.ToString();
        }

        public async Task DeleteVectorStoreAsync(string vectorStoreId)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/vector_stores/{vectorStoreId}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error deleting vector store: {response.ReasonPhrase}");
            }
        }

        public async Task<List<string>> GetAllVectorStoresAsync()
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/vector_stores");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error retrieving vector stores: {response.ReasonPhrase}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);
            var vectorStores = JsonSerializer.Deserialize<List<string>>(responseData?["vector_stores"]?.ToString());

            return vectorStores;
        }

        public async Task AddFileToVectorStoreAsync(string vectorStoreId, string fileId)
        {
            var payload = new { file_id = fileId };
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/vector_stores/{vectorStoreId}/files", payload);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error adding file to vector store: {response.ReasonPhrase}");
            }
        }

        public async Task AddFileToVectorStoreFromPathAsync(string vectorStoreId, string filePath)
        {
            var fileId = await new FileStoreManager(_openAIClient).UploadFileAsync(filePath);
            await AddFileToVectorStoreAsync(vectorStoreId, fileId);
        }

        public async Task<string> RetrieveFileFromFileStoreAsync(string vectorStoreId, string fileId)
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/vector_stores/{vectorStoreId}/files/{fileId}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error retrieving file from vector store: {response.ReasonPhrase}");
            }

            return await response.Content.ReadAsStringAsync();
        }

        public async Task DeleteFileFromVectorStoreAsync(string vectorStoreId, string fileId)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/vector_stores/{vectorStoreId}/files/{fileId}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error deleting file from vector store: {response.ReasonPhrase}");
            }
        }

    }
}
