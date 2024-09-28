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
    using System.Net.Http;

    public class OpenAIClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public OpenAIClient(string apiKey, string baseUrl)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _apiKey = apiKey;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public HttpClient HttpClient => _httpClient;

        public string BaseUrl => _baseUrl;
    }
}
