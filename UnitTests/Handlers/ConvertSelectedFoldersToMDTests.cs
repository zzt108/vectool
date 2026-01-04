using DocXHandlerTests;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;
using VecTool.Configuration;
using VecTool.Handlers;

namespace UnitTests.Handlers
{
    [TestFixture]
    public class ConvertSelectedFoldersToMDTests : DocTestBase
    {
        private readonly ILogger logger = TestLogger.For<ConvertSelectedFoldersToMDTests>();

        private static VectorStoreConfig GetVectorStoreConfig(List<string> list)
        {
            return new VectorStoreConfig { FolderPaths = list };
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(testRootPath))
            {
                Directory.Delete(testRootPath, true);
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

            var mdHandler = new MDHandler(logger, null, null);
            mdHandler.ExportSelectedFolders(
                outputMarkdownPath,
                GetVectorStoreConfig(new List<string> { folder1, folder2 }));

            File.Exists(outputMarkdownPath).ShouldBeTrue();

            string markdownContent = File.ReadAllText(outputMarkdownPath);
            markdownContent.ShouldContain($"# Folder: {MarkdownFolder1Name}");
            markdownContent.ShouldContain($"## File: {Markdown1FileName}");
            markdownContent.ShouldContain(ContentOfMarkdownFile1);
            markdownContent.ShouldContain($"# Folder: {MarkdownFolder2Name}");
            markdownContent.ShouldContain($"## File: {Markdown2FileName}");
            markdownContent.ShouldContain(ContentOfMarkdownFile2);
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

            var mdHandler = new MDHandler(logger, null, null);
            mdHandler.ExportSelectedFolders(outputMarkdownPath, new VectorStoreConfig() { FolderPaths = new List<string> { mainFolder } });

            File.Exists(outputMarkdownPath).ShouldBeTrue();
            string markdownContent = File.ReadAllText(outputMarkdownPath);
            markdownContent.ShouldContain($"# Folder: {MarkdownMainFolderName}");
            markdownContent.ShouldContain($"## File: {MainFileName}");
            markdownContent.ShouldContain(ContentOfMainFile);
            markdownContent.ShouldContain($"# Folder: {MarkdownSubFolderName}");
            markdownContent.ShouldContain($"## File: {SubFileName}");
            markdownContent.ShouldContain(ContentOfSubFile);
        }

        [SetUp]
        public void Setup()
        {
            testRootPath = Path.Combine(Path.GetTempPath(), "ConvertSelectedFoldersToDocxTests");
            Directory.CreateDirectory(testRootPath);
            outputDocxPath = Path.Combine(testRootPath, "output.docx");
        }
    }
}