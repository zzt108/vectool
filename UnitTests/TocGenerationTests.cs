using NUnit.Framework;
using VecTool.Configuration;

namespace DocXHandlerTests
{
    [TestFixture]
    public class TocGenerationTests
    {
        private string root = null!;
        private string outDocx = null!;
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

    }
}
