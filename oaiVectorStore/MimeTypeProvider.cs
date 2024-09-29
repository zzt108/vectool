using System;
using System.Collections.Generic;

namespace oaiVectorStore
{
    public class MimeTypeProvider
    {
        private static readonly Dictionary<string, string> _mimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".c", "text/x-c" },
            { ".cpp", "text/x-c++" },
            { ".cs", "text/x-csharp" },
            { ".csproj", "text/x-msbuild" },
            { ".css", "text/css" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".go", "text/x-golang" },
            { ".html", "text/html" },
            { ".java", "text/x-java" },
            { ".js", "text/javascript" },
            { ".json", "application/json" },
            { ".md", "text/markdown" },
            { ".pdf", "application/pdf" },
            { ".php", "text/x-php" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".py", "text/x-python" },
            { ".rb", "text/x-ruby" },
            { ".sh", "application/x-sh" },
            { ".tex", "text/x-tex" },
            { ".ts", "application/typescript" },
            { ".txt", "text/plain" },
            { ".feature", "text/x-gherkin" },
            // Add more MIME types as needed
        };

        private static readonly Dictionary<string, string> _newExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".cs", ".cs.md" },
            { ".csproj", ".csproj.md" },
            { ".feature", ".feature.md" }
            // Add more new extension mappings as needed
        };

        private static readonly Dictionary<string, string> _mdTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".cs", "csharp" },
            { ".csproj", "msbuild" },
            { ".feature", "gherkin" }
            // Add more new extension mappings as needed
        };

        public static string GetMimeType(string fileExtension)
        {
            // Ensure the file extension starts with a dot
            if (!fileExtension.StartsWith("."))
            {
                fileExtension = $".{fileExtension}";
            }

            return _mimeTypes.TryGetValue(fileExtension, out var mimeType) ? mimeType : "application/octet-stream"; // Default to binary stream if unknown
        }

        public static string? GetNewExtension(string fileExtension) {
            _newExtensions.TryGetValue(fileExtension, out string? newExtension);
            return newExtension; 
        }

        public static string? GetMdTag(string fileExtension) {
            _newExtensions.TryGetValue(fileExtension, out string? mdTag);
            return mdTag; 
        }
    }
}
/*
 ---------------------------

---------------------------
Error uploading files: UploadFileAsync Failed! HTTP status code: BadRequest | Response body: {
  "error": {
    "message": "Invalid extension cs. Supported formats: \"c\", \"cpp\", \"css\", \"csv\", \"docx\", \"gif\", \"go\", \"html\", \"java\", \"jpeg\", \"jpg\", \"js\", \"json\", \"md\", \"pdf\", \"php\", \"pkl\", \"png\", \"pptx\", \"py\", \"rb\", \"tar\", \"tex\", \"ts\", \"txt\", \"webp\", \"xlsx\", \"xml\", \"zip\"",
    "type": "invalid_request_error",
    "param": null,
    "code": null
  }
}

---------------------------
OK   
---------------------------

 */
