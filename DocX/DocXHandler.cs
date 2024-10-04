using oaiVectorStore;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
//using Xceed.Words.NET;

namespace DocXHandler;

public class DocXHandler
{
    public static void ConvertFilesToDocx(string folderPath, string outputPath)
    {
        // Create a new Word document
        using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(outputPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = new Body();
            mainPart.Document.Append(body);

            // Get all text files in the folder
            string[] files = Directory.GetFiles(folderPath);

            foreach (string file in files)
            {
                // Check MIME type and upload
                string extension = Path.GetExtension(file);
                if (MimeTypeProvider.GetMimeType(extension) == "application/octet-stream") // Skip unknown types
                {
                    continue;
                }

                // Check if the file content is not empty
                if (new FileInfo(file).Length == 0)
                {
                    continue; // Skip empty files
                }

                var mdTag = MimeTypeProvider.GetMdTag(extension);
                string content = File.ReadAllText(file);
                if (mdTag != null)
                {
                    // Add start and end language tags to the file content
                    content = $"```{mdTag}\n{content}\n```";
                }

                // Read the content of each file
                string fileContent = File.ReadAllText(file);

                // Create a new paragraph for each file's content
                Paragraph para = new Paragraph(new Run(new Text(fileContent)));
                body.Append(para);
            }
        }
    }

    /*
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
    */
}

