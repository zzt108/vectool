// UnitTests/DocX/CrossReferencesTests.cs
using Shouldly;
using DocumentFormat.OpenXml.Packaging;

namespace DocXHandlerTests
{
    [TestFixture]
    public class CrossReferencesTests
    {
        private string _root = "";
        private string _outDocx = "";

        [SetUp]
        public void Setup()
        {
            _root = Path.Combine(Path.GetTempPath(), "CrossReferencesTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            var aDir = Path.Combine(_root, "A");
            var bDir = Path.Combine(_root, "B");
            Directory.CreateDirectory(aDir);
            Directory.CreateDirectory(bDir);

            // Declares Foo in A/Foo.cs
            File.WriteAllText(Path.Combine(aDir, "Foo.cs"),
@"namespace DemoA { public class Foo { public int X; } }");

            // References Foo in B/Bar.cs
            File.WriteAllText(Path.Combine(bDir, "Bar.cs"),
@"using DemoA;
namespace DemoB { public class Bar { private Foo _f = new Foo(); } }");

            _outDocx = Path.Combine(_root, "out.docx");
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        [Test]
        public void Docx_ShouldContain_CrossReferences()
        {
            var handler = new DocXHandler.DocXHandler(null, null);
            var folders = Directory.GetDirectories(_root).ToList();

            handler.ConvertSelectedFoldersToDocx(folders, _outDocx, new DocXHandler.VectorStoreConfig());

            File.Exists(_outDocx).ShouldBeTrue();

            using var doc = WordprocessingDocument.Open(_outDocx, false);
            var text = doc.MainDocumentPart!.Document!.Body!.InnerText;

            text.ShouldContain("<cross_references>");
            text.ShouldContain("</cross_references>");
            text.ShouldContain("Foo.cs");
            text.ShouldContain("Bar.cs");
            // Expect that Bar depends on Foo
            text.ShouldContain("depends_on=");
            text.ShouldContain("Foo.cs");
        }
    }
}
