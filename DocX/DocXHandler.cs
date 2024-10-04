using oaiVectorStore;
using System.IO;
using Xceed.Words.NET;

namespace DocXHandler;

public class DocXHandler
{
    public static void ConvertFilesToDocx(string folderPath, string outputDocxPath)
    {
        var doc = DocX.Create(outputDocxPath);

        foreach (var filePath in Directory.GetFiles(folderPath))
        {
            // Check MIME type and upload
            string extension = Path.GetExtension(filePath);
            if (MimeTypeProvider.GetMimeType(extension) == "application/octet-stream") // Skip unknown types
            {
                continue;
            }

            // Check if the file content is not empty
            if (new FileInfo(filePath).Length == 0)
            {
                continue; // Skip empty files
            }

            var mdTag = MimeTypeProvider.GetMdTag(extension);
            string content = File.ReadAllText(filePath);
            if (mdTag != null)
            {
                // Add start and end language tags to the file content
                content = $"```{mdTag}\n{content}\n```";
            }
            doc.InsertParagraph(content).SpacingAfter(20);
        }

        doc.Save();
    }
}

