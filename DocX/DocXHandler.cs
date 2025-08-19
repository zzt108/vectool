using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocXHandler
{
    public class DocXHandler(IUserInterface ui) : FileHandlerBase(ui)
    {
        public void ConvertFilesToDocx(string folderPath, string outputPath, VectorStoreConfig vectorStoreConfig)
        {
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(outputPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = new Body();
                mainPart.Document.Append(body);

                ProcessFolder(
                    folderPath,
                    body,
                    vectorStoreConfig,
                    ProcessFile,
                    WriteFolderName,
                    WriteFolderEnd);
            }
        }

        private void ProcessFile(string file, Body body, VectorStoreConfig vectorStoreConfig)
        {

            try
            {
                string enhancedContent = GetEnhancedFileContent(file, vectorStoreConfig);

                var validator = new OpenXmlContentValidator();

                if (!validator.IsValid(enhancedContent))
                {
                    var violations = validator.FindInvalidCharacters(enhancedContent);
                    var invalidChars = string.Join(", ", violations.Select(c => $"'{((short)c):X}'"));
                    var ex = new InvalidDataException($"{file} contains invalid characters: {invalidChars}. These are not allowed in DocX.");
                    _log.Error(ex, $"File contains invalid characters: {file}. These are not allowed in DocX.");
                    return;
                }

                var relativePath = RelativePath(vectorStoreConfig.CommonRootPath, file);
                var lastModified = File.GetLastWriteTime(file);

                ParagraphProperties fileParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading 2" });
                Paragraph fileParagraph = new Paragraph(fileParagraphProperties,
                    new Run(new Text($"<file path=\"{relativePath}\" last_modified=\"{lastModified}\">")));
                body.Append(fileParagraph);

                // Paragraph para = new Paragraph(new Run(new Text(content)));
                // body.Append(para);
                foreach (var line in enhancedContent.Split(Environment.NewLine))
                {
                    Paragraph para = new Paragraph(new Run(new Text(line)));
                    body.Append(para);
                }

                body.Append(new Paragraph(new Run(new Text($"</file>"))));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error processing file: {file}", ex);
            }
        }

        private void WriteFolderName(Body body, string folderName)
        {
            ParagraphProperties folderParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading 1" });
            Paragraph folderParagraph = new Paragraph(folderParagraphProperties, new Run(new Text(folderName)));
            body.Append(folderParagraph);
        }

        private void WriteFolderEnd(Body body)
        {
            body.Append(new Paragraph(new Run(new Text($"</Folder>"))));
        }

        public void ConvertSelectedFoldersToDocx(List<string> folderPaths, string outputPath, VectorStoreConfig vectorStoreConfig)
        {
            try
            {
                _ui?.WorkStart("To Docx", folderPaths);
                int work = 0;
                try
                {
                    using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(outputPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                    {
                        MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                        mainPart.Document = new Document();
                        Body body = new Body();
                        mainPart.Document.Append(body);

                        // Add AI-optimized context at the beginning
                        AddAIOptimizedContext(folderPaths, body, (bodyContext, content) => {
                            foreach (var line in content.Split(Environment.NewLine))
                            {
                                Paragraph para = new Paragraph(new Run(new Text(line)));
                                bodyContext.Append(para);
                            }
                        });

                        foreach (var folderPath in folderPaths)
                        {
                            // Update the UI status for the current folder
                            // _ui?.UpdateStatus($"Processing folder: {folderPath}");

                            _ui?.UpdateProgress(work++);
                            ProcessFolder(
                                folderPath,
                                body,
                                vectorStoreConfig,
                                ProcessFile,
                                WriteFolderName,
                                WriteFolderEnd);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error processing folder: {folderPaths}", ex);
                    // _log.Error(ex, $"Error processing folder: {folderPaths}"); 
                }
            }
            finally
            {
                _ui?.WorkFinish();
            }
        }
    }
}
