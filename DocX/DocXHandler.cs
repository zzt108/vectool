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

            // Create a new paragraph for folder's name
            body.Append(new Paragraph(new Run(new Text($"<Folder name = {folderPath}>"))));

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

                if (extension == ".docx") // non text types should be uploadedseparately
                {
                    continue;
                }

                // Check if the file content is not empty
                if (new FileInfo(file).Length == 0)
                {
                    continue; // Skip empty files
                }

                string content = File.ReadAllText(file);
                var mdTag = MimeTypeProvider.GetMdTag(extension);
                if (mdTag != null)
                {
                    // Add start and end language tags to the file content
                    content = $"```{mdTag}\n{content}\n```";
                }

                // Calculate the relative path from rootFolder to folder
                string relativePath = Path.GetRelativePath(folderPath, file).Replace('\\', '_');

                // Create a new paragraph for each file's name
                body.Append(new Paragraph(new Run(new Text($"<File name = {relativePath}>"))));

                // Create a new paragraph for each file's content
                Paragraph para = new Paragraph(new Run(new Text(content)));
                body.Append(para);

                // Create a new paragraph for each file's end
                body.Append(new Paragraph(new Run(new Text($"</File>"))));

            }
            // Create a new paragraph for folder's name
            body.Append(new Paragraph(new Run(new Text($"</Folder"))));
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

