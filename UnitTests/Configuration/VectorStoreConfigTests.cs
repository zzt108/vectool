using NUnit.Framework;
using Shouldly;

namespace VecTool.Configuration.Tests
{
    [TestFixture]
    public class VectorStoreConfigTests
    {
        [Test]
        public void StarDot_Matches_NoExtension_Files_Only()
        {
            var cfg = new VectorStoreConfig { ExcludedFiles = new() { "*." } };
            cfg.IsFileExcluded("README").ShouldBeTrue();           // no extension [attached_file:2]
            cfg.IsFileExcluded("Makefile").ShouldBeTrue();         // no extension [attached_file:2]
            cfg.IsFileExcluded("file.txt").ShouldBeFalse();        // has extension [attached_file:2]
            cfg.IsFileExcluded("archive.tar.gz").ShouldBeFalse();  // has extension(s) [attached_file:2]
        }

        [Test]
        public void StarTxt_Matches_Txt_Files()
        {
            var cfg = new VectorStoreConfig { ExcludedFiles = new() { "*.txt" } };
            cfg.IsFileExcluded("a.txt").ShouldBeTrue();            // matches [attached_file:2]
            cfg.IsFileExcluded("b.tx").ShouldBeFalse();            // no match [attached_file:2]
        }

        [Test]
        public void StarDotStar_Matches_Files_With_Dot()
        {
            var cfg = new VectorStoreConfig { ExcludedFiles = new() { "*.*" } };
            cfg.IsFileExcluded("a.txt").ShouldBeTrue();            // has dot [attached_file:2]
            cfg.IsFileExcluded("README").ShouldBeFalse();          // no dot [attached_file:2]
        }

        [Test]
        public void Star_Matches_All()
        {
            var cfg = new VectorStoreConfig { ExcludedFiles = new() { "*" } };
            cfg.IsFileExcluded("anything").ShouldBeTrue();         // everything [attached_file:2]
        }
    }
}