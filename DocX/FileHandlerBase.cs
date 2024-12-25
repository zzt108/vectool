using oaiVectorStore;
using NLogS = NLogShared;
using System.IO;

namespace DocXHandler
{
    public abstract class FileHandlerBase
    {
        protected static NLogS.CtxLogger log = new();

        protected static bool IsFolderExcluded(string name, List<string> excludedList)
        {
            return excludedList.Contains(name);
        }

        protected static bool IsFileExcluded(string fileName, List<string> excludedFiles)
        {
            return excludedFiles.Any(excludedFile => string.Equals(excludedFile, fileName, StringComparison.OrdinalIgnoreCase));
        }

        protected static bool IsFileValid(string file, string outputPath)
        {
            if (file == outputPath)
            {
                return false;
            }

            string extension = Path.GetExtension(file);
            if (MimeTypeProvider.GetMimeType(extension) == "application/octet-stream" || MimeTypeProvider.IsBinary(extension))
            {
                return false;
            }

            return new FileInfo(file).Length > 0;
        }

        protected static string GetFileContent(string file)
        {
            string content = File.ReadAllText(file);
            var mdTag = MimeTypeProvider.GetMdTag(Path.GetExtension(file));
            if (mdTag != null)
            {
                content = $"```{mdTag}\n{content}\n```";
            }
            return content;
        }
    }
}
