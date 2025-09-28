using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using Constants; // Add this using

namespace DocXHandlerTests
{
    [TestFixture]
    public class CrossReferencesTests
    {
        private string root;
        private string outDocx;

        [SetUp]
        public void Setup()
        {
            root = Path.Combine(Path.GetTempPath(), "CrossReferencesTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            var aDir = Path.Combine(root, "A");
            var bDir = Path.Combine(root, "B");
            Directory.CreateDirectory(aDir);
            Directory.CreateDirectory(bDir);

            // Declares Foo in A/Foo.cs
            File.WriteAllText(Path.Combine(aDir, "Foo.cs"), @"namespace DemoA
{
    public class Foo
    {
        public int X { get; set; }
    }
}");

            // References Foo in B/Bar.cs
            File.WriteAllText(Path.Combine(bDir, "Bar.cs"), @"using DemoA;

namespace DemoB
{
    public class Bar
    {
        private Foo f = new Foo();
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
        public void DocxShouldContainCrossReferences()
        {
            var handler = new DocXHandler.DocXHandler(null, null);
            var folders = Directory.GetDirectories(root).ToList();
            handler.ConvertSelectedFoldersToDocx(folders, outDocx, new DocXHandler.VectorStoreConfig());

            File.Exists(outDocx).ShouldBeTrue();

            using var doc = WordprocessingDocument.Open(outDocx, false);
            var text = doc.MainDocumentPart!.Document!.Body!.InnerText;

            // ✅ Use constants instead of magic strings
            text.ShouldContain(Tags.CrossReferences); // Instead of "crossreferences"
            text.ShouldContain("Foo.cs");
            text.ShouldContain("Bar.cs");
            // Expect that Bar depends on Foo
            text.ShouldContain("dependson");
            text.ShouldContain("Foo.cs");
        }
    }
}
