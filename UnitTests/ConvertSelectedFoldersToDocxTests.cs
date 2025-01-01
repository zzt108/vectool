using FluentAssertions;
using NUnit.Framework;
using System.IO;
using System.Collections.Generic;
using DocXHandler;
using DocumentFormat.OpenXml.Packaging;

namespace DocXHandlerTests
{
    [TestFixture]
    public class ConvertSelectedFoldersToDocxTests
    {
        private const string Folder1Name = "Folder1";
        private const string Folder2Name = "Folder2";
        private const string MarkdownFolder1Name = "MarkdownFolder1";
        private const string MarkdownFolder2Name = "MarkdownFolder2";
        private const string MarkdownMainFolderName = "MarkdownMainFolder";
        private const string MarkdownSubFolderName = "MarkdownSubFolder";
        private const string EmptyFolderName = "EmptyFolder";
        private const string MainFolderName = "MainFolder";
        private const string SubFolder1Name = "SubFolder1";
        private const string SubFolder2Name = "SubFolder2";
        
        private const string Test1FileName = "test1.txt";
        private const string Test2FileName = "test2.txt";
        private const string Markdown1FileName = "markdown1.txt";
        private const string Markdown2FileName = "markdown2.txt";
        private const string MainFileName = "main.txt";
        private const string SubFileName = "sub.txt";
        private const string ImageFileName = "image.png";
        
        private const string ContentOfFile1 = "Content of file 1";
        private const string ContentOfFile2 = "Content of file 2";
        private const string ContentOfMarkdownFile1 = "Content of markdown file 1";
        private const string ContentOfMarkdownFile2 = "Content of markdown file 2";
        private const string ContentOfMainFile = "Content of main file";
        private const string ContentOfSubFile1 = "Content of sub file 1";
        private const string ContentOfSubFile2 = "Content of sub file 2";
        private const string ContentOfSubFile = "Content of sub file";

        private string testRootPath = "";
        private string outputDocxPath = "";

        [SetUp]
        public void Setup()
        {
            testRootPath = Path.Combine(Path.GetTempPath(), "ConvertSelectedFoldersToDocxTests");
            Directory.CreateDirectory(testRootPath);
            outputDocxPath = Path.Combine(testRootPath, "output.docx");
        }

        [Test]
        public void ConvertSelectedFoldersToDocx_MultipleFolders_ShouldIncludeAllInDocx()
        {
            string folder1 = Path.Combine(testRootPath, Folder1Name);
            string folder2 = Path.Combine(testRootPath, Folder2Name);
            Directory.CreateDirectory(folder1);
            Directory.CreateDirectory(folder2);

            string textFilePath1 = Path.Combine(folder1, Test1FileName);
            string textFilePath2 = Path.Combine(folder2, Test2FileName);
            File.WriteAllText(textFilePath1, ContentOfFile1);
            File.WriteAllText(textFilePath2, ContentOfFile2);

            List<string> folderPaths = new List<string> { folder1, folder2 };

            var docXHandler = new DocXHandler.DocXHandler();
            docXHandler.ConvertSelectedFoldersToDocx(folderPaths, outputDocxPath, new List<string>(), new List<string>());
            
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc?.MainDocumentPart?.Document.Body;
                body?.ChildElements.Count.Should().BeGreaterThan(5);
                body?.InnerText.Should().Contain(Folder1Name);
                body?.InnerText.Should().Contain(Folder2Name);
                body?.InnerText.Should().Contain(ContentOfFile1);
                body?.InnerText.Should().Contain(ContentOfFile2);
            }
        }

        [Test]
        public void ConvertSelectedFoldersToDocx_EmptyFolder_ShouldCreateEmptyDocx()
        {
            string emptyFolder = Path.Combine(testRootPath, EmptyFolderName);
            Directory.CreateDirectory(emptyFolder);

            List<string> folderPaths = new List<string> { emptyFolder };

            var docXHandler = new DocXHandler.DocXHandler();
            docXHandler.ConvertSelectedFoldersToDocx(folderPaths, outputDocxPath, new List<string>(), new List<string>());
            
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc?.MainDocumentPart?.Document.Body;
                body?.ChildElements.Count.Should().Be(2);
            }
        }

        [Test]
        public void ConvertSelectedFoldersToDocx_NonTextFiles_ShouldNotIncludeInDocx()
        {
            string folder = Path.Combine(testRootPath, Folder1Name);
            Directory.CreateDirectory(folder);

            string nonTextFilePath = Path.Combine(folder, ImageFileName);
            File.WriteAllBytes(nonTextFilePath, new byte[] { 0, 1, 2 });

            List<string> folderPaths = new List<string> { folder };

            var docXHandler = new DocXHandler.DocXHandler();
            docXHandler.ConvertSelectedFoldersToDocx(folderPaths, outputDocxPath, new List<string>(), new List<string>());
            
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc?.MainDocumentPart?.Document.Body;
                body?.ChildElements.Count.Should().Be(2);
            }
        }

        [Test]
        public void ConvertSelectedFoldersToDocx_Subfolders_ShouldIncludeAllInDocx()
        {
            string mainFolder = Path.Combine(testRootPath, MainFolderName);
            string subFolder1 = Path.Combine(mainFolder, SubFolder1Name);
            string subFolder2 = Path.Combine(mainFolder, SubFolder2Name);
            Directory.CreateDirectory(mainFolder);
            Directory.CreateDirectory(subFolder1);
            Directory.CreateDirectory(subFolder2);

            string textFilePath1 = Path.Combine(mainFolder, Test1FileName);
            string textFilePath2 = Path.Combine(subFolder1, Test2FileName);
            string textFilePath3 = Path.Combine(subFolder2, Markdown1FileName);
            File.WriteAllText(textFilePath1, ContentOfFile1);
            File.WriteAllText(textFilePath2, ContentOfFile2);
            File.WriteAllText(textFilePath3, ContentOfMarkdownFile1);

            List<string> folderPaths = new List<string> { mainFolder };

            var docXHandler = new DocXHandler.DocXHandler();
            docXHandler.ConvertSelectedFoldersToDocx(folderPaths, outputDocxPath, new List<string>(), new List<string>());
            
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc?.MainDocumentPart?.Document.Body;
                body?.ChildElements.Count.Should().BeGreaterThan(5);
                body?.InnerText.Should().Contain(ContentOfFile1);
                body?.InnerText.Should().Contain(ContentOfFile2);
                body?.InnerText.Should().Contain(ContentOfMarkdownFile1);
            }
        }

        [Test]
        public void ExportSelectedFoldersToMarkdown_MultipleFolders_ShouldIncludeAllInMarkdown()
        {
            string folder1 = Path.Combine(testRootPath, MarkdownFolder1Name);
            string folder2 = Path.Combine(testRootPath, MarkdownFolder2Name);
            Directory.CreateDirectory(folder1);
            Directory.CreateDirectory(folder2);

            string textFilePath1 = Path.Combine(folder1, Markdown1FileName);
            string textFilePath2 = Path.Combine(folder2, Markdown2FileName);
            File.WriteAllText(textFilePath1, ContentOfMarkdownFile1);
            File.WriteAllText(textFilePath2, ContentOfMarkdownFile2);

            string outputMarkdownPath = Path.Combine(testRootPath, "output.md");
            List<string> folderPaths = new List<string> { folder1, folder2 };

            var mdHandler = new DocXHandler.MDHandler();
            mdHandler.ExportSelectedFolders(folderPaths, outputMarkdownPath, new List<string>(), new List<string>());
            
            File.Exists(outputMarkdownPath).Should().BeTrue();

            string markdownContent = File.ReadAllText(outputMarkdownPath);
            markdownContent.Should().Contain($"# Folder: {MarkdownFolder1Name}");
            markdownContent.Should().Contain($"## File: {Markdown1FileName}");
            markdownContent.Should().Contain(ContentOfMarkdownFile1);
            markdownContent.Should().Contain($"# Folder: {MarkdownFolder2Name}");
            markdownContent.Should().Contain($"## File: {Markdown2FileName}");
            markdownContent.Should().Contain(ContentOfMarkdownFile2);
        }

        [Test]
        public void ExportSelectedFoldersToMarkdown_WithSubfolders_ShouldIncludeAllFiles()
        {
            string mainFolder = Path.Combine(testRootPath, MarkdownMainFolderName);
            string subFolder = Path.Combine(mainFolder, MarkdownSubFolderName);
            Directory.CreateDirectory(mainFolder);
            Directory.CreateDirectory(subFolder);

            string mainFile = Path.Combine(mainFolder, MainFileName);
            string subFile = Path.Combine(subFolder, SubFileName);
            File.WriteAllText(mainFile, ContentOfMainFile);
            File.WriteAllText(subFile, ContentOfSubFile);

            string outputMarkdownPath = Path.Combine(testRootPath, "output_recursive.md");
            List<string> folderPaths = new List<string> { mainFolder };

            var mdHandler = new DocXHandler.MDHandler();
            mdHandler.ExportSelectedFolders(folderPaths, outputMarkdownPath, new List<string>(), new List<string>());
            
            File.Exists(outputMarkdownPath).Should().BeTrue();
            string markdownContent = File.ReadAllText(outputMarkdownPath);
            markdownContent.Should().Contain($"# Folder: {MarkdownMainFolderName}");
            markdownContent.Should().Contain($"## File: {MainFileName}");
            markdownContent.Should().Contain(ContentOfMainFile);
            markdownContent.Should().Contain($"# Folder: {MarkdownSubFolderName}");
            markdownContent.Should().Contain($"## File: {SubFileName}");
            markdownContent.Should().Contain(ContentOfSubFile);
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(testRootPath))
            {
                Directory.Delete(testRootPath, true);
            }
        }
    }
}
