using FluentAssertions;
using DocumentFormat.OpenXml.Packaging;

namespace DocXHandlerTests
{

    [TestFixture]
    public class ConvertSelectedFoldersToDocxTests : DocTestBase
    {

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

            var docXHandler = new DocXHandler.DocXHandler(null);
            docXHandler.ConvertSelectedFoldersToDocx(folderPaths, outputDocxPath, new DocXHandler.VectorStoreConfig());
            
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

            var docXHandler = new DocXHandler.DocXHandler(null);
            docXHandler.ConvertSelectedFoldersToDocx(folderPaths, outputDocxPath, new DocXHandler.VectorStoreConfig());
            
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc?.MainDocumentPart?.Document.Body;
                body?.InnerText.Should().NotContain("```");
                // body?.ChildElements.Count.Should().Be(2); // original empty document
                body?.ChildElements.Count.Should().Be(28); // AI guidance added to empty documents too
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

            var docXHandler = new DocXHandler.DocXHandler(null);
            docXHandler.ConvertSelectedFoldersToDocx(folderPaths, outputDocxPath, new DocXHandler.VectorStoreConfig());
            
            File.Exists(outputDocxPath).Should().BeTrue();

            using (var doc = WordprocessingDocument.Open(outputDocxPath, false))
            {
                var body = doc?.MainDocumentPart?.Document.Body;
                body?.ChildElements.Count.Should().Be(28);
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

            var docXHandler = new DocXHandler.DocXHandler(null);
            docXHandler.ConvertSelectedFoldersToDocx(folderPaths, outputDocxPath, new DocXHandler.VectorStoreConfig());
            
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
