using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using NLogS = NLogShared;
using PdfSharp.Pdf;

namespace DocXHandler
{
    public class PDFHandler : FileHandlerBase
    {
        private static readonly NLogS.CtxLogger log = new NLogS.CtxLogger();

        public void ExportSelectedFoldersToPdf(List<string> folderPaths, string outputPath, List<string> excludedFiles, List<string> excludedFolders)
        {
            using (var document = new Document())
            {
                DefineStyles(document); // Define styles for the document
                Section section = document.AddSection();

                foreach (var folderPath in folderPaths)
                {
                    ProcessFolder(
                        folderPath,
                        section, // Pass the section as context
                        excludedFiles,
                        excludedFolders,
                        ProcessFile,
                        WriteFolderName,
                        WriteFolderEnd
                    );
                }

                PdfDocumentRenderer renderer = new PdfDocumentRenderer(true, PdfFontEmbedding.Always)
                {
                    Document = document
                };
                renderer.RenderDocument();
                renderer.PdfDocument.Save(outputPath);
            }
        }

        private void DefineStyles(Document document)
        {
            // Get predefined style "Normal".
            Style style = document.Styles["Normal"];
            // Because all styles are derived from Normal, this is the
            // easiest way to change the default.
            style.Font.Name = "Verdana";
            style = document.Styles.AddStyle("FolderHeading", "Normal");
            style.Font.Size = 16;
            style.Font.Bold = true;
            style = document.Styles.AddStyle("FileHeading", "Normal");
            style.Font.Size = 12;
            style.Font.Bold = true;
        }

        private void WriteFolderName(Section section, string folderName)
        {
            Paragraph paragraph = section.AddParagraph();
            paragraph.Style = "FolderHeading";
            paragraph.AddText($"Folder: {folderName}");
        }

        private void WriteFolderEnd(Section section)
        {
            // You can add something at the end of each folder if needed
        }

        private void ProcessFile(string file, Section section, List<string> excludedFiles, List<string> excludedFolders)
        {
            string fileName = Path.GetFileName(file);
            if (IsFileExcluded(fileName, excludedFiles) || !IsFileValid(file, null))
            {
                log.Debug($"Skipping excluded file: {file}");
                return;
            }

            string relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), file).Replace('\\', '_'); // Or use folderPath as base if needed
            DateTime lastModified = File.GetLastWriteTime(file);

            Paragraph fileParagraph = section.AddParagraph();
            fileParagraph.Style = "FileHeading";
            fileParagraph.AddText($"File: {relativePath} Time: {lastModified}");

            string content = GetFileContent(file);
            Paragraph para = section.AddParagraph();
            para.AddText(content);
        }

        // protected override void ProcessFolder<T>(string folderPath, T context, List<string> excludedFiles, List<string> excludedFolders, List<string>> processFile, Action<T, string> writeFolderName, Action<T> writeFolderEnd = null)

        // protected override void ProcessFolder<T>(string folderPath, T context, List<string> excludedFiles, List<string> excludedFolders, List<string>> processFile, Action<T, string> writeFolderName, Action<T> writeFolderEnd = null)
        // {
        //     string folderName = new DirectoryInfo(folderPath).Name;
        //     if (IsFolderExcluded(folderName, excludedFolders))
        //     {
        //         log.Debug($"Skipping excluded folder: {folderPath}");
        //         return;
        //     }
        //     log.Debug(folderPath);
        //     writeFolderName(context, folderName);

        //     string[] files = Directory.GetFiles(folderPath);
        //     foreach (string file in files)
        //     {
        //         processFile(file, context, excludedFiles, excludedFolders);
        //     }

        //     string[] subfolders = Directory.GetDirectories(folderPath);
        //     foreach (string subfolder in subfolders)
        //     {
        //         ProcessFolder(subfolder, context, excludedFiles, excludedFolders, processFile, writeFolderName, writeFolderEnd);
        //     }
        //     writeFolderEnd?.Invoke(context);
        // }
    }
}