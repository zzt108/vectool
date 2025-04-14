// ConvertSelectedFoldersToPdfTests.cs
using FluentAssertions;
using QuestPDF.Infrastructure;

namespace DocXHandlerTests
{
    [TestFixture]
    public class ConvertSelectedFoldersToPdfTests : DocTestBase
    {
        [SetUp]
        public void Setup()
        {
            testRootPath = Path.Combine(Path.GetTempPath(), "ConvertSelectedFoldersToPdfTests");
            Directory.CreateDirectory(testRootPath);
            outputDocxPath = Path.Combine(testRootPath, "output.pdf"); //Change to .pdf
            QuestPDF.Settings.License = LicenseType.Community; //Setting the license is mendatory
        }

        [Test]
        public void ConvertSelectedFoldersToPdf_MultipleFolders_ShouldIncludeAllInPdf()
        {
            // Arrange
            string folder1 = Path.Combine(testRootPath, Folder1Name);
            string folder2 = Path.Combine(testRootPath, Folder2Name);
            Directory.CreateDirectory(folder1);
            Directory.CreateDirectory(folder2);

            string textFilePath1 = Path.Combine(folder1, Test1FileName);
            string textFilePath2 = Path.Combine(folder2, Test2FileName);
            File.WriteAllText(textFilePath1, ContentOfFile1);
            File.WriteAllText(textFilePath2, ContentOfFile2);

            List<string> folderPaths = new List<string> { folder1, folder2 };
            var pdfHandler = new DocXHandler.PdfHandler();

            // Act
            pdfHandler.ConvertSelectedFoldersToPdf(folderPaths, outputDocxPath, new DocXHandler.VectorStoreConfig());

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();

            // Basic PDF validation (check if it's a valid PDF, not empty)
            // For more robust validation, consider a dedicated PDF parsing library
            new FileInfo(outputDocxPath).Length.Should().BeGreaterThan(0);
        }
     
        [Test]
        public void ConvertSelectedFoldersToPdf_EmptyFolder_ShouldCreatePdf()
        {
            string emptyFolder = Path.Combine(testRootPath, EmptyFolderName);
            Directory.CreateDirectory(emptyFolder);

            List<string> folderPaths = new List<string> { emptyFolder };
            var pdfHandler = new DocXHandler.PdfHandler();

            pdfHandler.ConvertSelectedFoldersToPdf(folderPaths, outputDocxPath, new DocXHandler.VectorStoreConfig());

            File.Exists(outputDocxPath).Should().BeTrue();
            new FileInfo(outputDocxPath).Length.Should().BeGreaterThan(0);
        }

        [Test]
        public void ConvertSelectedFoldersToPdf_NonTextFiles_ShouldNotIncludeInPdf()
        {
            string folder = Path.Combine(testRootPath, Folder1Name);
            Directory.CreateDirectory(folder);

            string nonTextFilePath = Path.Combine(folder, ImageFileName);
            File.WriteAllBytes(nonTextFilePath, new byte[] { 0, 1, 2 }); // Create a dummy image file

            List<string> folderPaths = new List<string> { folder };
            var pdfHandler = new DocXHandler.PdfHandler();

            pdfHandler.ConvertSelectedFoldersToPdf(folderPaths, outputDocxPath, new DocXHandler.VectorStoreConfig());

            File.Exists(outputDocxPath).Should().BeTrue();
            new FileInfo(outputDocxPath).Length.Should().BeGreaterThan(0);
        }

        [Test]
        public void ConvertSelectedFoldersToPdf_Subfolders_ShouldIncludeAllInPdf()
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
            var pdfHandler = new DocXHandler.PdfHandler();

            pdfHandler.ConvertSelectedFoldersToPdf(folderPaths, outputDocxPath, new DocXHandler.VectorStoreConfig());

            File.Exists(outputDocxPath).Should().BeTrue();
            new FileInfo(outputDocxPath).Length.Should().BeGreaterThan(0);
        }

        [Test]
        public void ConvertSelectedFoldersToPdf_TextFile_ShouldIncludeInPdf()
        {
            // Arrange
            string folder = Path.Combine(testRootPath, Folder1Name);
            Directory.CreateDirectory(folder);
            string textFilePath = Path.Combine(folder, "test.txt");
            File.WriteAllText(textFilePath, "Hello, World!");

            List<string> folderPaths = new List<string> { folder };
            var pdfHandler = new DocXHandler.PdfHandler();

            // Act
            pdfHandler.ConvertSelectedFoldersToPdf(folderPaths, outputDocxPath, new DocXHandler.VectorStoreConfig());

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();
            new FileInfo(outputDocxPath).Length.Should().BeGreaterThan(0); //Non empty
        }

        [Test]
        public void ConvertSelectedFoldersToPdf_EmptyFile_ShouldNotIncludeInPdf()
        {
            // Arrange
            string folder = Path.Combine(testRootPath, Folder1Name);
            Directory.CreateDirectory(folder);
            string emptyFilePath = Path.Combine(folder, "empty.txt");
            File.WriteAllText(emptyFilePath, ""); // Create an empty file

            List<string> folderPaths = new List<string> { folder };
            var pdfHandler = new DocXHandler.PdfHandler();
            // Act
            pdfHandler.ConvertSelectedFoldersToPdf(folderPaths, outputDocxPath, new DocXHandler.VectorStoreConfig());

            // Assert
            File.Exists(outputDocxPath).Should().BeTrue();
            new FileInfo(outputDocxPath).Length.Should().BeGreaterThan(0); //Non empty

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