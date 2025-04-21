using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DocXHandler
{
    public class PdfHandler(IUserInterface? ui) : FileHandlerBase(ui)
    {
        static PdfHandler()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.EnableDebugging = false;
            QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
        }

        public void ConvertSelectedFoldersToPdf(List<string> folderPaths, string outputPath, VectorStoreConfig vectorStoreConfig)
        {
            var d = Document.Create(document =>
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
                                vectorStoreConfig,
                                ProcessFile,
                                WriteFolderName,
                                WriteFolderEnd);
                        }
                    });
                });
            })
                .WithSettings(new DocumentSettings
                {
                    PdfA = false,
                    CompressDocument = true,
                    ImageCompressionQuality = ImageCompressionQuality.Medium,
                    ImageRasterDpi = 72
                });
            d.GeneratePdf(outputPath);
        }

        private void ProcessFile(string file, ColumnDescriptor column, VectorStoreConfig vectorStoreConfig)
        {
            try
            {
                string fileName = Path.GetFileName(file);
                if (IsFileExcluded(fileName, vectorStoreConfig) || !IsFileValid(file, null))
                {
                    log.Trace($"Skipping excluded file: {file}");
                    return;
                }

                string content = GetFileContent(file);
                if (string.IsNullOrEmpty(content))
                {
                    log.Debug($"Empty content for file: {file}");
                    return;
                }

                string directoryName = Path.GetDirectoryName(file);
                if (string.IsNullOrEmpty(directoryName))
                {
                    directoryName = ".";
                }

                string relativePath = Path.GetRelativePath(directoryName, file);
                string sectionId = Regex.Replace(relativePath, @"[^a-zA-Z0-9_-]", "_");
                DateTime lastModified = File.GetLastWriteTime(file);

                column.Item().PaddingLeft(10).DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black)).Text(text =>
                {
                    text.Span("<File name = ").SemiBold();
                    text.Span($"{relativePath}>").SemiBold().Style(TextStyle.Default.FontColor(Colors.Grey.Darken2));
                    text.Span(" <Time: ").SemiBold();
                    text.Span($"{lastModified}>").SemiBold().Style(TextStyle.Default.FontColor(Colors.Grey.Darken2));
                });

                column.Item().PaddingLeft(15).Text(content).FontSize(10);
            }
            catch (Exception ex)
            {
                log.Debug($"Error processing file {file}: {ex.Message}");
                // Continue processing other files
            }
        }

        private void WriteFolderName(ColumnDescriptor column, string folderName)
        {
            column.Item().Text(folderName).FontSize(14).Bold().FontColor(Colors.Black);
        }

        private void WriteFolderEnd(ColumnDescriptor column)
        {
            // You can add something here if you want to mark the end of a folder in PDF
        }

    }
}