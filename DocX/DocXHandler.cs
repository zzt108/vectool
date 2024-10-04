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

            string content = File.ReadAllText(filePath);
            doc.InsertParagraph(content).SpacingAfter(20);
        }

        doc.Save();
    }
}

