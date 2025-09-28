using Shouldly;
using DocumentFormat.OpenXml.Packaging;
using DocXHandler;

namespace DocXHandlerTests
{
    [TestFixture]
    public class TocGenerationTests
    {
        private string _root = "";
        private string _outDocx = "";
        private VectorStoreConfig _config = new VectorStoreConfig();

        [SetUp]
        public void Setup()
        {
            _root = Path.Combine(Path.GetTempPath(), "TocGenerationTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            var srcA = Path.Combine(_root, "SectionA");
            var srcB = Path.Combine(_root, "SectionB");
            Directory.CreateDirectory(srcA);
            Directory.CreateDirectory(srcB);

            File.WriteAllText(Path.Combine(srcA, "a1.cs"), "class A1 {}");
            File.WriteAllText(Path.Combine(srcA, "a2.md"), "# A2");
            File.WriteAllText(Path.Combine(srcB, "b1.txt"), "B1");
            Directory.CreateDirectory(Path.Combine(srcB, "Nested"));
            File.WriteAllText(Path.Combine(srcB, "Nested", "b2.cs"), "class B2 {}");

            _outDocx = Path.Combine(_root, "out.docx");
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        [Test]
        public void Docx_ShouldContain_TableOfContents()
        {
            var handler = new DocXHandler.DocXHandler(null, null);
            var folders = Directory.GetDirectories(_root).ToList();

            handler.ConvertSelectedFoldersToDocx(folders, _outDocx, _config);

            File.Exists(_outDocx).ShouldBeTrue();

            using var doc = WordprocessingDocument.Open(_outDocx, false);
            var text = doc.MainDocumentPart!.Document!.Body!.InnerText;

            text.ShouldContain("<tableofcontents>");
            text.ShouldContain("</tableofcontents>");
            text.ShouldContain("SectionA");
            text.ShouldContain("SectionB");
            text.ShouldContain("a1.cs");
            text.ShouldContain("b2.cs");
        }
    }
}
