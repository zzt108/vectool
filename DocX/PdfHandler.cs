using System.Collections.Generic;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using NLogShared = NLogShared;

namespace DocXHandler
{
    public class PdfHandler : FileHandlerBase
    {
        public void ConvertSelectedFoldersToPdf(List<string> folderPaths, string outputPath, List<string> excludedFiles, List<string> excludedFolders)
        {
            Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Margin(20);
                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        foreach (var folderPath in folderPaths)
                        {
                            ProcessFolder(
                                folderPath,
                                column,
                                ProcessFile,
                                excludedFiles,
                                excludedFolders,
                                WriteFolderName,
                                WriteFolderEnd);
                        }
                    });
                });
            }).GeneratePdf(outputPath);
        }

        private void ProcessFile(string file, ColumnDescriptor column, List<string> excludedFiles, List<string> excludedFolders)
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

            column.Section().PaddingLeft(10).Content(section =>
            {
                section.Column(fileColumn =>
                {
                    fileColumn.Item().Text(text =>
                    {
                        text.Span("<File name = ").SemiBold();
                        text.Span($"{relativePath}>").SemiBold().Color(Colors.Grey.Darken2);
                        text.Span(" <Time: ").SemiBold();
                        text.Span($"{lastModified}>").SemiBold().Color(Colors.Grey.Darken2);
                    }).FontSize(10).FontColor(Colors.Black);
                    fileColumn.Item().PaddingLeft(5).Text(content).FontSize(10);
                });
            });
        }

        private void WriteFolderName(ColumnDescriptor column, string folderName)
        {
            column.Item().Text(folderName).FontSize(14).Bold().FontColor(Colors.Black);
        }

        private void WriteFolderEnd(ColumnDescriptor column)
        {
            // You can add something here if you want to mark the end of a folder in PDF
        }

        private void ProcessFolder<TDescriptor, TContext, TProcessFile, TWriteFolderName, TWriteFolderEnd>(
            string folderPath,
            TDescriptor context,
            TProcessFile processFile,
            List<string> excludedFiles,
            List<string> excludedFolders,
            TWriteFolderName writeFolderName,
            TWriteFolderEnd writeFolderEnd = null)
            where TDescriptor : QuestPDF.Fluent.ColumnDescriptor
            where TProcessFile : System.Delegate
            where TWriteFolderName : System.Delegate
            where TWriteFolderEnd : System.Delegate
        {
            string folderName = new DirectoryInfo(folderPath).Name;
            if (IsFolderExcluded(folderName, excludedFolders))
            {
                log.Debug($"Skipping excluded folder: {folderPath}");
                return;
            }
            log.Debug(folderPath);
            writeFolderName.DynamicInvoke(context, folderName);

            string[] files = Directory.GetFiles(folderPath);
            foreach (string file in files)
            {
                processFile.DynamicInvoke(file, context, excludedFiles, excludedFolders);
            }

            string[] subfolders = Directory.GetDirectories(folderPath);
            foreach (string subfolder in subfolders)
            {
                ProcessFolder(subfolder, context, processFile, excludedFiles, excludedFolders, writeFolderName, writeFolderEnd);
            }
            writeFolderEnd?.DynamicInvoke(context);
        }
    }
}