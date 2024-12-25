﻿using oaiVectorStore;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using NLogS = NLogShared;

namespace DocXHandler;

public class DocXHandler : FileHandlerBase
{

    private static void ProcessFolder(string folderPath, Body body, List<string> excludedFiles, List<string> excludedFolders)
    {
        string folderName = new DirectoryInfo(folderPath).Name;
        if (IsExcluded(folderName, excludedFolders))
        {
            log.Debug($"Skipping excluded folder: {folderPath}");
            return;
        }

        log.Debug(folderPath);

        // Create a new paragraph for folder's name with Heading 1 style
        ParagraphProperties folderParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading 1" });
        Paragraph folderParagraph = new Paragraph(folderParagraphProperties, new Run(new Text(folderName)));
        body.Append(folderParagraph);

        // Get all text files in the folder
        string[] files = Directory.GetFiles(folderPath);

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            if (IsFileExcluded(fileName, excludedFiles) || !IsFileValid(file, null))
            {
                log.Debug($"Skipping excluded file: {file}");
                continue; // Skip this file
            }
            string content = GetFileContent(file);

            // Calculate the relative path from rootFolder to folder
            string relativePath = Path.GetRelativePath(folderPath, file).Replace('\\', '_');

            DateTime lastModified = File.GetLastWriteTime(file);

            // Create a new paragraph for each file's name with Heading 2 style
            ParagraphProperties fileParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading 2" });
            Paragraph fileParagraph = new Paragraph(fileParagraphProperties, new Run(new Text($"<File name = {relativePath}> <Time: {lastModified}>")));
            body.Append(fileParagraph);

            // Create a new paragraph for each file's content
            Paragraph para = new Paragraph(new Run(new Text(content)));
            body.Append(para);

            // Create a new paragraph for each file's end
            body.Append(new Paragraph(new Run(new Text($"</File>"))));
        }

        // Process subfolders
        string[] subfolders = Directory.GetDirectories(folderPath);
        foreach (string subfolder in subfolders)
        {
            ProcessFolder(subfolder, body, excludedFiles, excludedFolders);
        }

        // Create a new paragraph for folder's end
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
            }
        }
    }
}
