using FluentAssertions;
using NUnit.Framework;
using System.IO;
using DocXHandler;
using DocumentFormat.OpenXml.Packaging;

namespace DocXHandlerTests
{
    [TestFixture]
    public class DocXHandlerTests
    {
        private string testFolderPath;
        private string outputDocxPath;

        [SetUp]
        public void Setup()
        {
            // Create a temporary folder for testing
            testFolderPath = Path.Combine(Path.GetTempPath(), "DocXHandlerTests");
            Directory.CreateDirectory(testFolderPath);

            // Define the output DOCX file path
            outputDocxPath = Path.Combine(testFolderPath, "output.docx");
        }

        [TearDown]
        public void Cleanup()
        {
            // Clean up the test folder after each test
            if (Directory.Exists(testFolderPath))
            {
                Directory.Delete(testFolderPath, true);
            }
        }

        [Test]
        public void ConvertFilesToDocx_EmptyFolder_ShouldCreateEmptyDocx()
        {
            // Act
            DocXHandler.DocXHandler.ConvertFilesToDocx(testFolderPath, outputDocxPath);

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            // Check if the document is empty
            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc.MainDocumentPart.Document.Body;
                body.ChildElements.Count.Should().Be(2); // Folder tags added
            }
        }

        [Test]
        public void ConvertFilesToDocx_NonTextFiles_ShouldNotIncludeInDocx()
        {
            // Arrange
            string nonTextFilePath = Path.Combine(testFolderPath, "image.png");
            File.WriteAllBytes(nonTextFilePath, new byte[] { 0, 1, 2 }); // Create a dummy image file

            // Act
            DocXHandler.DocXHandler.ConvertFilesToDocx(testFolderPath, outputDocxPath);

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            // Check if the document is empty
            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc.MainDocumentPart.Document.Body;
                body.ChildElements.Count.Should().Be(2); // Expecting an empty document, folder tags added
            }
        }

        [Test]
        public void ConvertFilesToDocx_TextFile_ShouldIncludeInDocx()
        {
            // Arrange
            string textFilePath = Path.Combine(testFolderPath, "test.txt");
            File.WriteAllText(textFilePath, "Hello, World!");

            // Act
            DocXHandler.DocXHandler.ConvertFilesToDocx(testFolderPath, outputDocxPath);

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc.MainDocumentPart.Document.Body;
                body.ChildElements.Count.Should().BeGreaterThan(0); // Expecting at least one element
                body.InnerText.Should().Contain("Hello, World!"); // Check if the content is included
            }
        }

        [Test]
        public void ConvertFilesToDocx_EmptyFile_ShouldNotIncludeInDocx()
        {
            // Arrange
            string emptyFilePath = Path.Combine(testFolderPath, "empty.txt");
            File.WriteAllText(emptyFilePath, ""); // Create an empty file

            // Act
            DocXHandler.DocXHandler.ConvertFilesToDocx(testFolderPath, outputDocxPath);

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc.MainDocumentPart.Document.Body;
                body.ChildElements.Count.Should().Be(2); // Expecting an empty document
            }
        }
    }
}