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
        private string testRootPath;
        private string outputDocxPath;

        [SetUp]
        public void Setup()
        {
            // Create a temporary root folder for testing
            testRootPath = Path.Combine(Path.GetTempPath(), "ConvertSelectedFoldersToDocxTests");
            Directory.CreateDirectory(testRootPath);

            // Define the output DOCX file path
            outputDocxPath = Path.Combine(testRootPath, "output.docx");
        }

        [Test]
        public void ConvertSelectedFoldersToDocx_MultipleFolders_ShouldIncludeAllInDocx()
        {
            // Arrange
            string folder1 = Path.Combine(testRootPath, "Folder1");
            string folder2 = Path.Combine(testRootPath, "Folder2");
            Directory.CreateDirectory(folder1);
            Directory.CreateDirectory(folder2);

            string textFilePath1 = Path.Combine(folder1, "test1.txt");
            string textFilePath2 = Path.Combine(folder2, "test2.txt");
            File.WriteAllText(textFilePath1, "Content of file 1");
            File.WriteAllText(textFilePath2, "Content of file 2");

            List<string> folderPaths = new List<string> { folder1, folder2 };

            // Act
            DocXHandler.DocXHandler.ConvertSelectedFoldersToDocx(folderPaths, outputDocxPath);

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc.MainDocumentPart.Document.Body;
                body.ChildElements.Count.Should().BeGreaterThan(5); // Expecting at least one element
                body.InnerText.Should().Contain($"<Folder name = {folder1}>");
                body.InnerText.Should().Contain($"<Folder name = {folder2}>");
                body.InnerText.Should().Contain("Content of file 1");
                body.InnerText.Should().Contain("Content of file 2");
            }
        }

        [Test]
        public void ConvertSelectedFoldersToDocx_EmptyFolder_ShouldCreateEmptyDocx()
        {
            // Arrange
            string emptyFolder = Path.Combine(testRootPath, "EmptyFolder");
            Directory.CreateDirectory(emptyFolder);

            List<string> folderPaths = new List<string> { emptyFolder };

            // Act
            DocXHandler.DocXHandler.ConvertSelectedFoldersToDocx(folderPaths, outputDocxPath);

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc.MainDocumentPart.Document.Body;
                body.ChildElements.Count.Should().Be(2); // Folder tags added
            }
        }

        [Test]
        public void ConvertSelectedFoldersToDocx_NonTextFiles_ShouldNotIncludeInDocx()
        {
            // Arrange
            string folder = Path.Combine(testRootPath, "Folder");
            Directory.CreateDirectory(folder);

            string nonTextFilePath = Path.Combine(folder, "image.png");
            File.WriteAllBytes(nonTextFilePath, new byte[] { 0, 1, 2 }); // Create a dummy image file

            List<string> folderPaths = new List<string> { folder };

            // Act
            DocXHandler.DocXHandler.ConvertSelectedFoldersToDocx(folderPaths, outputDocxPath);

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc.MainDocumentPart.Document.Body;
                body.ChildElements.Count.Should().Be(2); // Expecting an empty document, folder tags added
            }
        }

        [Test]
        public void ConvertSelectedFoldersToDocx_Subfolders_ShouldIncludeAllInDocx()
        {
            // Arrange
            string mainFolder = Path.Combine(testRootPath, "MainFolder");
            string subFolder1 = Path.Combine(mainFolder, "SubFolder1");
            string subFolder2 = Path.Combine(mainFolder, "SubFolder2");
            Directory.CreateDirectory(mainFolder);
            Directory.CreateDirectory(subFolder1);
            Directory.CreateDirectory(subFolder2);

            string textFilePath1 = Path.Combine(mainFolder, "mainFile.txt");
            string textFilePath2 = Path.Combine(subFolder1, "subFile1.txt");
            string textFilePath3 = Path.Combine(subFolder2, "subFile2.txt");
            File.WriteAllText(textFilePath1, "Content of main file");
            File.WriteAllText(textFilePath2, "Content of sub file 1");
            File.WriteAllText(textFilePath3, "Content of sub file 2");

            List<string> folderPaths = new List<string> { mainFolder };

            // Act
            DocXHandler.DocXHandler.ConvertSelectedFoldersToDocx(folderPaths, outputDocxPath);

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc.MainDocumentPart.Document.Body;
                body.ChildElements.Count.Should().BeGreaterThan(5); // Expecting at least one element
                body.InnerText.Should().Contain("Content of main file");
                body.InnerText.Should().Contain("Content of sub file 1");
                body.InnerText.Should().Contain("Content of sub file 2");
            }
        }

        [TearDown]
        public void Cleanup()
        {
            // Clean up the test folder after each test
            if (Directory.Exists(testRootPath))
            {
                Directory.Delete(testRootPath, true);
            }
        }
    }
}
