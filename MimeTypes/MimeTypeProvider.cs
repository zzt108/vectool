using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace oaiVectorStore
{
    public class MimeTypeProvider
    {
        private static readonly Dictionary<string, string> _mimeTypes;
        private static readonly Dictionary<string, string> _newExtensions;
        private static readonly Dictionary<string, string> _mdTags;

        static MimeTypeProvider()
        {
            _mimeTypes = LoadDictionaryFromFile("Config\\mimeTypes.json");
            _newExtensions = LoadDictionaryFromFile("Config\\newExtensions.json");
            _mdTags = LoadDictionaryFromFile("Config\\mdTags.json");
        }

        private static Dictionary<string, string> LoadDictionaryFromFile(string fileName)
        {
            var jsonContent = File.ReadAllText(fileName);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent)
                   ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public static string GetMimeType(string? fileExtension)
        {
            if (string.IsNullOrEmpty(fileExtension))
            {
                return "application/octet-stream";
            }

            if (!fileExtension.StartsWith("."))
            {
                fileExtension = $".{fileExtension}";
            }

            return _mimeTypes.TryGetValue(fileExtension, out var mimeType) ? mimeType : "application/octet-stream";
        }

        public static string? GetNewExtension(string fileExtension)
        {
            _newExtensions.TryGetValue(fileExtension, out string? newExtension);
            if (newExtension != null)
            {
                return newExtension;
            }
            else
            {
                return fileExtension;
            }
        }

        public static string? GetMdTag(string fileExtension)
        {
            _mdTags.TryGetValue(fileExtension, out string? mdTag);
            return mdTag;
        }
    }
}/*
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
