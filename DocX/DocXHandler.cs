using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DocXHandler
{
    public interface IDocumentContentValidator
    {
        bool IsValid(string content);
        IEnumerable<char> FindInvalidCharacters(string content);
    }

    public class OpenXmlContentValidator : IDocumentContentValidator
    {
        private static readonly char[] NonPrintableCharacters =
            Enumerable.Range(0, 9)             // ASCII 0-8
            .Concat(Enumerable.Range(11, 2))   // ASCII 11-12
            .Concat(Enumerable.Range(14, 18))  // ASCII 14-31
            .Select(i => (char)i)
            .ToArray();

        public bool IsValid(string content)
        {
            if (content == null)
                return true;

            return !NonPrintableCharacters.Any(content.Contains);
        }

        public IEnumerable<char> FindInvalidCharacters(string content)
        {
            if (content == null)
                return Enumerable.Empty<char>();

            return NonPrintableCharacters
                .Where(content.Contains)
                .ToList();
        }
    }


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
            string fileName = Path.GetFileName(file);
            if (IsFileExcluded(fileName, vectorStoreConfig) || !IsFileValid(file, null))
            {
                _log.Trace($"Skipping excluded file: {file}");
                return;
            }

            try
            {
                string content = GetFileContent(file);
                //if (content.Contains('\0'))
                //{
                //    var ex =  new InvalidDataException($"File contains character #0: {file}. This is not allowed in DocX.");
                //    _log.Error(ex, $"File contains character #0: {file}. This is not allowed in DocX."); 
                //}

                var validator = new OpenXmlContentValidator();

                if (!validator.IsValid(content))
                {
                    var violations = validator.FindInvalidCharacters(content);
                    var invalidChars = string.Join(", ", violations.Select(c => $"'{((short)c):X}'"));
                    var ex = new InvalidDataException($"{file} contains invalid characters: {invalidChars}. These are not allowed in DocX.");
                    _log.Error(ex, $"File contains invalid characters: {file}. These are not allowed in DocX.");
                    return;
                }

                string relativePath = Path.GetRelativePath(Path.GetDirectoryName(file), file).Replace('\\', '_');
                DateTime lastModified = File.GetLastWriteTime(file);

                ParagraphProperties fileParagraphProperties = new ParagraphProperties(new ParagraphStyleId() { Val = "Heading 2" });
                Paragraph fileParagraph = new Paragraph(fileParagraphProperties, new Run(new Text($"<File name = {relativePath}> <Time: {lastModified}>")));
                body.Append(fileParagraph);

                Paragraph para = new Paragraph(new Run(new Text(content)));
                body.Append(para);

                body.Append(new Paragraph(new Run(new Text($"</File>"))));
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
