using FluentAssertions;

namespace DocXHandlerTests
{
    [TestFixture]
    public class ConvertSelectedFoldersToMDTests : DocTestBase
    {

        [SetUp]
        public void Setup()
        {
            testRootPath = Path.Combine(Path.GetTempPath(), "ConvertSelectedFoldersToDocxTests");
            Directory.CreateDirectory(testRootPath);
            outputDocxPath = Path.Combine(testRootPath, "output.docx");
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

            var mdHandler = new DocXHandler.MDHandler(null);
            mdHandler.ExportSelectedFolders(folderPaths, outputMarkdownPath, new DocXHandler.VectorStoreConfig());
            
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

            var mdHandler = new DocXHandler.MDHandler(null);
            mdHandler.ExportSelectedFolders(folderPaths, outputMarkdownPath, new DocXHandler.VectorStoreConfig());
            
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
