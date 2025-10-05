using NUnit.Framework;
using Shouldly;
using System.IO;
using VecTool.Handlers; // Adjust namespace if VecTool class is elsewhere
using VecTool.Utils;

namespace VecToolDev.Tests
{
    [TestFixture]
    public class VecToolTests
    {
        private FileHandlerBase _handler;
        private string _tempFilePath;

        [SetUp]
        public void SetUp()
        {
            // Using a concrete subclass or a minimal implementation for testing
            _handler = new TestFileHandler();
            _tempFilePath = Path.GetTempFileName();
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempFilePath))
                File.Delete(_tempFilePath);
        }

        [Test]
        public void GetFileSizeFormatted_ShouldReturnBytes_WhenUnder1KB()
        {
            File.WriteAllBytes(_tempFilePath, new byte[512]);
            var result = FileSizeFormatter.GetFileSizeFormatted(_tempFilePath);
            result.ShouldBe("512 B");
        }

        [Test]
        public void GetFileSizeFormatted_ShouldReturnKB_WhenOver1KB()
        {
            File.WriteAllBytes(_tempFilePath, new byte[2048]);
            var result = FileSizeFormatter.GetFileSizeFormatted(_tempFilePath);
            result.ShouldBe("2 KB");
        }

        [Test]
        public void GetFileSizeFormatted_ShouldReturnMB_ForLargeFiles()
        {
            File.WriteAllBytes(_tempFilePath, new byte[5 * 1024 * 1024]);
            var result = FileSizeFormatter.GetFileSizeFormatted(_tempFilePath);
            result.ShouldBe("5 MB");
        }

        // Minimal concrete implementation for testing abstract base
        private class TestFileHandler : FileHandlerBase
        {
            public TestFileHandler() : base(null, null) { }
        }
    }
}
