using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using XmlStringDiscovery;

namespace UnitTests.XmlStringDiscovery
{
    [TestFixture]
    public class XmlStringCatalogTests
    {
        private string _root = null!;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "XmlStringDiscovery_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            // Production sample dir
            var prod = Path.Combine(_root, "DocX");
            Directory.CreateDirectory(prod);

            // Tests sample dir
            var tests = Path.Combine(_root, "UnitTests");
            Directory.CreateDirectory(tests);

            // Production sample file: structure + metadata + content + header-like
            File.WriteAllText(Path.Combine(prod, "Generator.cs"),
@"using System;
class X {
   void A() {
      var a = ""tableofcontents"";
      var b = ""crossreferences"";
      var c = ""file name={0} path={1} ext={2}"";
      var d = ""aiguidance"";
      var e = @""projectsummary"";
      var f = ""section name=Code"";
      var g = ""somethingElse"";
   }
}
");

            // Test-only sample: introduces an extra tag only under UnitTests
            File.WriteAllText(Path.Combine(tests, "GeneratorTests.cs"),
@"using System;
class T {
   void A() {
      var a = ""testonlytag"";
      var b = ""file path=C:\temp name=foo"";
   }
}
");
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_root, true); } catch { /* ignore */ }
        }

        [Test]
        public void ShouldFindAllXmlTags()
        {
            var scanner = new XmlTagScanner();
            var catalog = scanner.Scan(new ScanOptions { RootDirectory = _root });

            catalog.All.Keys.ShouldContain("tableofcontents");
            catalog.All.Keys.ShouldContain("crossreferences");
            catalog.All.Keys.ShouldContain("file name={0} path={1} ext={2}");
            catalog.All.Keys.ShouldContain("aiguidance");
            catalog.All.Keys.ShouldContain("projectsummary");
            catalog.All.Keys.ShouldContain("section name=Code");
            catalog.All.Keys.ShouldContain("testonlytag");
        }

        [Test]
        public void ShouldClassifyTagsByContext()
        {
            var scanner = new XmlTagScanner();
            var catalog = scanner.Scan(new ScanOptions { RootDirectory = _root });

            catalog.All["tableofcontents"].Category.ShouldBe(TagContextCategory.Structure);
            catalog.All["crossreferences"].Category.ShouldBe(TagContextCategory.Structure);
            catalog.All["file name={0} path={1} ext={2}"].Category.ShouldBe(TagContextCategory.Metadata);
            catalog.All["aiguidance"].Category.ShouldBe(TagContextCategory.Content);
        }

        [Test]
        public void ShouldIdentifyTestOnlyStrings()
        {
            var scanner = new XmlTagScanner();
            var catalog = scanner.Scan(new ScanOptions { RootDirectory = _root });

            catalog.All["testonlytag"].IsTestOnly.ShouldBeTrue();
            catalog.TestOnly.ContainsKey("testonlytag").ShouldBeTrue();
            catalog.Production.ContainsKey("testonlytag").ShouldBeFalse();

            catalog.All["tableofcontents"].IsTestOnly.ShouldBeFalse();
            catalog.Production.ContainsKey("tableofcontents").ShouldBeTrue();
        }
    }
}
