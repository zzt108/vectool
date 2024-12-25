﻿using oaiVectorStore;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using NLogS = NLogShared;

namespace DocXHandler;

public class DocXHandler : FileHandlerBase
{
    private static void ProcessFile(string file, Body body, List<string> excludedFiles, List<string> excludedFolders)
    {
        string fileName = Path.GetFileName(file);
        if (IsFileExcluded(fileName, excludedFiles) || !IsFileValid(file, null))
        {
            log.Debug($"Skipping excluded file: {file}");
            return;
        }
        
        string content = GetFileContent(file);
        string relativePath = Path.GetRelativePath(Path.GetDirectoryName(file), file).Replace('\\', '_');
        DateTime lastModified = File.GetLastWriteTime(file);

        ParagraphProperties fileParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading 2" });
        Paragraph fileParagraph = new Paragraph(fileParagraphProperties, new Run(new Text($"<File name = {relativePath}> <Time: {lastModified}>")));
        body.Append(fileParagraph);

        Paragraph para = new Paragraph(new Run(new Text(content)));
        body.Append(para);

        body.Append(new Paragraph(new Run(new Text($"</File>"))));
    }

    private static void WriteFolderName(Body body, string folderName)
    {
        ParagraphProperties folderParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading 1" });
        Paragraph folderParagraph = new Paragraph(folderParagraphProperties, new Run(new Text(folderName)));
        body.Append(folderParagraph);
    }

    private static void WriteFolderEnd(Body body)
    {
        body.Append(new Paragraph(new Run(new Text($"</Folder>"))));
    }

    public static void ConvertFilesToDocx(string folderPath, string outputPath, List<string> excludedFiles, List<string> excludedFolders)
    {
        using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(outputPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = new Body();
            mainPart.Document.Append(body);

            ProcessFolder(folderPath, body, excludedFiles, excludedFolders, ProcessFile, WriteFolderName, WriteFolderEnd);
        }
    }

    public static void ConvertSelectedFoldersToDocx(List<string> folderPaths, string outputPath, List<string> excludedFiles, List<string> excludedFolders)
    {
        using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(outputPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = new Body();
            mainPart.Document.Append(body);

            foreach (string folderPath in folderPaths)
            {
                ProcessFolder(folderPath, body, excludedFiles, excludedFolders, ProcessFile, WriteFolderName, WriteFolderEnd);
            }
        }
    }
}
