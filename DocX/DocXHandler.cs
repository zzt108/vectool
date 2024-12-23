﻿using oaiVectorStore;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using NLogS = NLogShared;

namespace DocXHandler;

public class DocXHandler
{
    private static NLogS.CtxLogger log = new();

    private static void ProcessFolder(string folderPath, Body body, List<string> excludedFiles, List<string> excludedFolders)
    {

        string folderName = new DirectoryInfo(folderPath).Name;
        if (excludedFolders.Contains(folderName))
        {
            log.Debug($"Skipping excluded folder: {folderPath}");
            return;
        }

        log.Debug(folderPath);

        // Create a new paragraph for folder's name
        body.Append(new Paragraph(new Run(new Text($"<Folder name = {folderPath}>"))));

        // Get all text files in the folder
        string[] files = Directory.GetFiles(folderPath);

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            if (excludedFiles.Any(excludedFile => string.Equals(excludedFile, fileName, StringComparison.OrdinalIgnoreCase)))
            {
                log.Debug($"Skipping excluded file: {file}");
                continue; // Skip this file
            }
            // Check MIME type and upload
            string extension = Path.GetExtension(file);
            if (MimeTypeProvider.GetMimeType(extension) == "application/octet-stream") // Skip unknown types
            {
                continue;
            }

            if (MimeTypeProvider.IsBinary(extension)) // non text types should be uploaded separately
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
        body.Append(new Paragraph(new Run(new Text($"</Folder>"))));
    }

    public static void ConvertFilesToDocx(string folderPath, string outputPath, List<string> excludedFiles, List<string> excludedFolders)
    {
        // Create a new Word document
        using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(outputPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = new Body();
            mainPart.Document.Append(body);

            ProcessFolder(folderPath, body, excludedFiles, excludedFolders);
        }
    }

    public static void ConvertSelectedFoldersToDocx(List<string> folderPaths, string outputPath, List<string> excludedFiles, List<string> excludedFolders)
    {
        // Create a new Word document
        using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(outputPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = new Body();
            mainPart.Document.Append(body);

            foreach (string folderPath in folderPaths)
            {
                ProcessFolder(folderPath, body, excludedFiles, excludedFolders);
                // // Process subfolders
                // string[] subfolders = Directory.GetDirectories(folderPath);
                // foreach (string subfolder in subfolders)
                // {
                //     ProcessFolder(subfolder, body, excludedFiles);
                // }
            }
        }
    }
}
