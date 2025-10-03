using NUnit.Framework;
using DocumentFormat.OpenXml.Packaging;
using Shouldly;
using VecTool.Handlers;
using VecTool.Constants;

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
        public void ConvertSelectedFoldersToDocxMultipleFoldersShouldIncludeAllInDocx()
        {
            // ✅ Create folders with simple names that will appear in XML
            string folder1 = Path.Combine(testRootPath, "src1");  // Simple folder name
            string folder2 = Path.Combine(testRootPath, "src2");  // Simple folder name
            Directory.CreateDirectory(folder1);
            Directory.CreateDirectory(folder2);

            string textFilePath1 = Path.Combine(folder1, Test1FileName);
            string textFilePath2 = Path.Combine(folder2, Test2FileName);
            File.WriteAllText(textFilePath1, ContentOfFile1);
            File.WriteAllText(textFilePath2, ContentOfFile2);

            List<string> folderPaths = new List<string> { folder1, folder2 };
            var docXHandler = new DocXHandler.DocXHandler(null, null);
            docXHandler.ConvertSelectedFoldersToDocx(folderPaths, outputDocxPath, new DocXHandler.VectorStoreConfig());

            File.Exists(outputDocxPath).ShouldBeTrue();

            using var doc = WordprocessingDocument.Open(outputDocxPath, false);
            var body = doc?.MainDocumentPart?.Document.Body;
            body?.ChildElements.Count.Should().BeGreaterThan(5);

            // ✅ Assert for simple folder names, not full paths
            body?.InnerText.Should().Contain("src1");  // Folder name in XML
            body?.InnerText.Should().Contain("src2");  // Folder name in XML
            body?.InnerText.Should().Contain(ContentOfFile1);
            body?.InnerText.Should().Contain(ContentOfFile2);

            // ✅ Also check for constants usage in XML structure
            body?.InnerText.Should().Contain(Tags.TableOfContents);  // Instead of magic "tableofcontents"
            body?.InnerText.Should().Contain(Tags.CrossReferences);   // Instead of magic "crossreferences"  
            body?.InnerText.Should().Contain(Tags.CodeMetaInfo);      // Instead of magic "codemetainfo"
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testRootPath))
                Directory.Delete(testRootPath, true);
        }
    }
}
