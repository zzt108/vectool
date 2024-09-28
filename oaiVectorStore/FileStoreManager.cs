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
    using System.Text.Json;
    using System.Threading.Tasks;

    public class FileStoreManager
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public FileStoreManager(OpenAIClient openAiClient)
        {
            _httpClient = openAiClient.HttpClient;
            _baseUrl = openAiClient.BaseUrl;
        }

        public FileStoreManager(string apiKey, string baseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _apiKey = apiKey;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        // Delete File from File Store
        public async Task DeleteFileFromFileStoreAsync(string fileId)
        {
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/files/{fileId}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error deleting file from file store: {response.ReasonPhrase}");
            }
        }

        public async Task<string> UploadFileAsync(string filePath)
        {
            using var multipartContent = new MultipartFormDataContent();
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream"); // or another appropriate MIME type

            multipartContent.Add(streamContent, "file", Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync($"{_baseUrl}/files", multipartContent);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error uploading file: {response.ReasonPhrase}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<Dictionary<string, object>>(responseBody);
            return responseData?["id"]?.ToString();
        }

    }
}
