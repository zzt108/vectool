using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;

namespace DocXHandlerTests
{
    [TestFixture]
    public class CodeMetaInfoTests
    {
        private string _root = "";
        private string _outDocx = "";

        [SetUp]
        public void Setup()
        {
            _root = Path.Combine(Path.GetTempPath(), "CodeMetaInfo_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            var src = Path.Combine(_root, "Src");
            var tests = Path.Combine(_root, "UnitTests");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(tests);

            File.WriteAllText(Path.Combine(src, "Foo.cs"),
@"namespace Demo {
    public class Foo {
        private readonly IFooDep _dep;
        public Foo(IFooDep dep) { _dep = dep; }
        public int Add(int a, int b) { if (a > 0) { return a + b; } return a + b; }
    }
    public interface IFooDep {}
}");

            File.WriteAllText(Path.Combine(tests, "FooTests.cs"),
@"using NUnit.Framework;
public class FooTests { [Test] public void A() { /* TODO */ } }");

            _outDocx = Path.Combine(_root, "out.docx");
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        [Test]
        public void Docx_ShouldContain_CodeMetaInfo()
        {
            var handler = new DocXHandler.DocXHandler(null, null);
            var folders = Directory.GetDirectories(_root).ToList();

            handler.ConvertSelectedFoldersToDocx(folders, _outDocx, new DocXHandler.VectorStoreConfig());

            File.Exists(_outDocx).ShouldBeTrue();

            using var doc = WordprocessingDocument.Open(_outDocx, false);
            var text = doc.MainDocumentPart!.Document!.Body!.InnerText;

            text.ShouldContain("<codemetainfo>");
            text.ShouldContain("</codemetainfo>");
            text.ShouldContain("Foo.cs");
            text.ShouldContain("metrics sizebytes=");
            text.ShouldContain("analysis complexity=");
            text.ShouldContain("patterns=");
            text.ShouldContain("hastests=");
            text.ShouldContain("signals longmethodscount=");
        }
    }
}
