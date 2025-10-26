using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using VecTool.Handlers;
using VecTool.Configuration;
using VecTool.Constants;

namespace DocXHandlerTests
{
    [TestFixture]
    public class CrossReferencesTests
    {
        private string root = null!;
        private string outDocx = null!;

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
    }
}
