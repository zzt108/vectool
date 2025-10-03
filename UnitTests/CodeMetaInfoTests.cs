using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using VecTool.Handlers;
using VecTool.Constants;

namespace DocXHandlerTests
{
    [TestFixture]
    public class CodeMetaInfoTests
    {
        private string root;
        private string outDocx;

        [SetUp]
        public void Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "CodeMetaInfo", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            var src = Path.Combine(root, "Src");
            var tests = Path.Combine(root, "UnitTests");
            Directory.CreateDirectory(src);
            Directory.CreateDirectory(tests);

            File.WriteAllText(Path.Combine(src, "Foo.cs"), @"namespace Demo
{
    public class Foo
    {
        private readonly IFooDep dep;
        
        public Foo(IFooDep dep)
        {
            this.dep = dep;
        }
        
        public int Add(int a, int b)
        {
            if (a < 0) return a + b;
            return a + b;
        }
    }
    
    public interface IFooDep { }
}");

            File.WriteAllText(Path.Combine(tests, "FooTests.cs"), @"using NUnit.Framework;

public class FooTests
{
    [Test]
    public void A()
    {
        // TODO
    }
}");

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
        public void DocxShouldContainCodeMetaInfo()
        {
            var handler = new DocXHandler.DocXHandler(null, null);
            var folders = Directory.GetDirectories(root).ToList();
            handler.ConvertSelectedFoldersToDocx(folders, outDocx, new DocXHandler.VectorStoreConfig());

            File.Exists(outDocx).ShouldBeTrue();

            using var doc = WordprocessingDocument.Open(outDocx, false);
            var text = doc.MainDocumentPart!.Document!.Body!.InnerText;

            // ✅ Use constants instead of magic strings
            text.ShouldContain(Tags.CodeMetaInfo); // Instead of "codemetainfo"
            text.ShouldContain("Foo.cs");
            text.ShouldContain("metrics");
            text.ShouldContain("sizebytes");
            text.ShouldContain("analysis");
            text.ShouldContain("complexity");
            text.ShouldContain("patterns");
            text.ShouldContain("hastests");
            text.ShouldContain("signals");
            text.ShouldContain("longmethodscount");
        }
    }
}
