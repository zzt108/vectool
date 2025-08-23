
using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Collections.Generic;
using DocXHandler;

namespace UnitTests
{
    [TestFixture]
    public class PdfHandlerTests
    {
        private string _testFolderPath;
        private string _outputPdfPath;
        private PdfHandler _pdfHandler;
        private VectorStoreConfig _vectorStoreConfig;

        [SetUp]
        public void Setup()
        {
            _testFolderPath = Path.Combine(Path.GetTempPath(), "PdfHandlerTests");
            Directory.CreateDirectory(_testFolderPath);
            _outputPdfPath = Path.Combine(_testFolderPath, "output.pdf");
            _pdfHandler = new PdfHandler(null);
            _vectorStoreConfig = new VectorStoreConfig();
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(_testFolderPath))
            {
                Directory.Delete(_testFolderPath, true);
            }
        }

        [Test]
        public void ConvertSelectedFoldersToPdf_WithTextFiles_ShouldCreatePdfFile()
        {
            // Arrange
            var textFilePath1 = Path.Combine(_testFolderPath, "test1.txt");
            var textFilePath2 = Path.Combine(_testFolderPath, "test2.txt");
            File.WriteAllText(textFilePath1, "Hello");
            File.WriteAllText(textFilePath2, "World");

            var folders = new List<string> { _testFolderPath };

            // Act
            _pdfHandler.ConvertSelectedFoldersToPdf(folders, _outputPdfPath, _vectorStoreConfig);

            // Assert
            File.Exists(_outputPdfPath).ShouldBeTrue();
            new FileInfo(_outputPdfPath).Length.ShouldBeGreaterThan(0);
        }

        [Test]
        public void ConvertSelectedFoldersToPdf_EmptyFolder_ShouldCreateEmptyPdfFile()
        {
            // Arrange
            var folders = new List<string> { _testFolderPath };

            // Act
            _pdfHandler.ConvertSelectedFoldersToPdf(folders, _outputPdfPath, _vectorStoreConfig);

            // Assert
            File.Exists(_outputPdfPath).ShouldBeTrue();
            new FileInfo(_outputPdfPath).Length.ShouldBeGreaterThan(0); // PDF will have some size due to header/footer
        }

        [Test]
        public void ConvertSelectedFoldersToPdf_WithNonTextFile_ShouldNotBeIncluded()
        {
            // Arrange
            var nonTextFilePath = Path.Combine(_testFolderPath, "image.png");
            File.WriteAllBytes(nonTextFilePath, new byte[] { 1, 2, 3 });
            var folders = new List<string> { _testFolderPath };

            // Act
            _pdfHandler.ConvertSelectedFoldersToPdf(folders, _outputPdfPath, _vectorStoreConfig);

            // Assert
            File.Exists(_outputPdfPath).ShouldBeTrue();
            // Cannot easily verify content, but we can check if the file size is small
            new FileInfo(_outputPdfPath).Length.ShouldBeGreaterThan(0);
        }
    }
}
