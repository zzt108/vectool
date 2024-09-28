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
        public async Task<bool> DeleteFileFromFileStoreAsync(string fileId)
        {
            using var api = new OpenAIClient();
            var isDeleted = await api.FilesEndpoint.DeleteFileAsync(fileId);
            return isDeleted;
        }

        public async Task<string> UploadFileAsync(string filePath)
        {
            using var api = new OpenAIClient();
            var file = await api.FilesEndpoint.UploadFileAsync("path/to/your/file.jsonl", FilePurpose.Assistants);
            return file.Id;
        }

    }
}
