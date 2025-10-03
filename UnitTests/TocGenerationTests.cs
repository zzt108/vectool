using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using VecTool.Configuration;
using VecTool.Handlers;
using VecTool.Constants;

namespace DocXHandlerTests
{
    [TestFixture]
    public class TocGenerationTests
    {
        private string root;
        private string outDocx;
        private VectorStoreConfig config = new();

        [SetUp]
        public void Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "TocGenerationTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            var srcA = Path.Combine(root, "SectionA");
            var srcB = Path.Combine(root, "SectionB");
            Directory.CreateDirectory(srcA);
            Directory.CreateDirectory(srcB);

            File.WriteAllText(Path.Combine(srcA, "a1.cs"), "class A1 { }");
            File.WriteAllText(Path.Combine(srcA, "a2.md"), "# A2");
            File.WriteAllText(Path.Combine(srcB, "b1.txt"), "B1");

            Directory.CreateDirectory(Path.Combine(srcB, "Nested"));
            File.WriteAllText(Path.Combine(srcB, "Nested", "b2.cs"), "class B2 { }");

            outDocx = Path.Combine(root, "out.docx");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
            catch { /* ignore */ }
        }

        [Test]
        public void DocxShouldContainTableOfContents()
        {
            var handler = new DocXHandler(null, null);
            var folders = Directory.GetDirectories(root).ToList();
            handler.ConvertSelectedFoldersToDocx(folders, outDocx, config);

            File.Exists(outDocx).ShouldBeTrue();

            using var doc = WordprocessingDocument.Open(outDocx, false);
            var text = doc.MainDocumentPart!.Document!.Body!.InnerText;

            // ✅ Use constants instead of magic strings
            text.ShouldContain(Tags.TableOfContents); // Instead of "tableofcontents" 
        }
    }
}
