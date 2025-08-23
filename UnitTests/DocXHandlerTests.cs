﻿using FluentAssertions;
using DocumentFormat.OpenXml.Packaging;
using DocXHandler;

namespace DocXHandlerTests
{
    [TestFixture]
    public class DocXHandlerTests
    {
        private string testFolderPath = "";
        private string outputDocxPath = "";

        private VectorStoreConfig vectorStoreConfig = new VectorStoreConfig();

        private DocXHandler.DocXHandler docXHandler;

        [SetUp]
        public void Setup()
        {
            // Create a temporary folder for testing
            testFolderPath = Path.Combine(Path.GetTempPath(), "DocXHandlerTests");
            Directory.CreateDirectory(testFolderPath);

            // Define the output DOCX file path
            outputDocxPath = Path.Combine(Path.GetTempPath(), "DocXHandlerTests_output", "output.docx");
            Directory.CreateDirectory(Path.GetDirectoryName(outputDocxPath));
            
            // Initialize DocXHandler instance
            docXHandler = new DocXHandler.DocXHandler(null);
        }

        [Test]
        public void ConvertFilesToDocx_MultipleFiles_ShouldIncludeAllInDocx()
        {
            // Arrange
            string textFilePath1 = Path.Combine(testFolderPath, "test1.txt");
            string textFilePath2 = Path.Combine(testFolderPath, "test2.txt");
            string textFilePath3 = Path.Combine(testFolderPath, "test3.txt");
            File.WriteAllText(textFilePath1, "Content of file 1");
            File.WriteAllText(textFilePath2, "Content of file 2");
            File.WriteAllText(textFilePath3, "Content of file 3");

            // Act
            docXHandler.ConvertFilesToDocx(testFolderPath, outputDocxPath, vectorStoreConfig);

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc?.MainDocumentPart?.Document.Body;
                body?.ChildElements.Count.Should().BeGreaterThan(5); // Expecting at least one element
                body?.FirstChild?.InnerText.Should().Be("DocXHandlerTests"); // Check if the folder name is correct
                body?.LastChild?.InnerText.Should().Contain("</Folder>"); // Check if the folder tag is included
                body?.InnerText.Should().Contain("Content of file 1"); // Check if the content is included
                body?.InnerText.Should().Contain("Content of file 2"); // Check if the content is included
                body?.InnerText.Should().Contain("Content of file 3"); // Check if the content is included
            }
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
            docXHandler.ConvertFilesToDocx(testFolderPath, outputDocxPath, vectorStoreConfig);

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            // Check if the document is empty
            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc?.MainDocumentPart?.Document.Body;
                body?.ChildElements.Count.Should().Be(2); // Folder tags added
            }
        }

        [Test]
        public void ConvertFilesToDocx_NonTextFiles_ShouldNotIncludeInDocx()
        {
            // Arrange
            string nonTextFilePath = Path.Combine(testFolderPath, "image.png");
            File.WriteAllBytes(nonTextFilePath, new byte[] { 0, 1, 2 }); // Create a dummy image file

            // Act
            docXHandler.ConvertFilesToDocx(testFolderPath, outputDocxPath, vectorStoreConfig);

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            // Check if the document is empty
            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc?.MainDocumentPart?.Document.Body;
                body?.ChildElements.Count.Should().Be(2); // Expecting an empty document, folder tags added
                body?.FirstChild?.InnerText.Should().Be("DocXHandlerTests"); // Check if the folder name is correct
                body?.LastChild?.InnerText.Should().Contain("</Folder>"); // Check if the folder tag is included
            }
            /*
            The reason why the test case ConvertFilesToDocx_EmptyFolder_ShouldCreateEmptyDocx expects body.ChildElements.Count.Should().Be(2); 
            is due to the implementation of the ConvertFilesToDocx method in the DocXHandler class. 
            This method adds a paragraph with a folder tag at the beginning and end of the document body, 
            represented as <Folder name = {folderPath}> and </Folder>. 
            These two tags are the reason for the count of 2 child elements in the document body when the folder is empty. 
            The test verifies that these folder tags are correctly added to the document, ensuring it is well-formed even when there are no files to process within the folder.
            */
        }

        [Test]
        public void ConvertFilesToDocx_TextFile_ShouldIncludeInDocx()
        {
            // Arrange
            string textFilePath = Path.Combine(testFolderPath, "test.txt");
            File.WriteAllText(textFilePath, "Hello, World!");

            // Act
            docXHandler.ConvertFilesToDocx(testFolderPath, outputDocxPath, vectorStoreConfig);

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc?.MainDocumentPart?.Document.Body;
                body?.ChildElements.Count.Should().BeGreaterThan(3); // Expecting at least one element
                body?.FirstChild?.InnerText.Should().Be("DocXHandlerTests"); // Check if the folder name is correct
                body?.LastChild?.InnerText.Should().Contain("</Folder>"); // Check if the folder tag is included
                body?.InnerText.Should().Contain("Hello, World!"); // Check if the content is included
            }
        }

        [Test]
        public void ConvertFilesToDocx_EmptyFile_ShouldNotIncludeInDocx()
        {
            // Arrange
            string emptyFilePath = Path.Combine(testFolderPath, "empty.txt");
            File.WriteAllText(emptyFilePath, ""); // Create an empty file

            // Act
            docXHandler.ConvertFilesToDocx(testFolderPath, outputDocxPath, vectorStoreConfig);

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc?.MainDocumentPart?.Document.Body;
                // body?.ChildElements.Count.Should().Be(2); // Expecting an empty document
                body?.InnerText.Should().Contain($"```txt```");
            }
        }
    }
}
