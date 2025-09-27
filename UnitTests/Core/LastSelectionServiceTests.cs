using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using oaiUI.Services;

namespace UnitTests
{
    [TestFixture]
    public class LastSelectionServiceTests
    {
        private string _tempDir = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "VecTool_LastSel_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
        }

        [Test]
        public void Get_WhenFileMissing_ShouldReturnNull()
        {
            var svc = new LastSelectionService(_tempDir);
            svc.GetLastSelectedVectorStore().ShouldBeNull();
        }

        [Test]
        public void Set_ThenGet_ShouldRoundTrip()
        {
            var svc = new LastSelectionService(_tempDir);
            svc.SetLastSelectedVectorStore("MyVS");
            svc.GetLastSelectedVectorStore().ShouldBe("MyVS");
        }

        [Test]
        public void Set_Null_ShouldClearValue()
        {
            var svc = new LastSelectionService(_tempDir);
            svc.SetLastSelectedVectorStore("X");
            svc.SetLastSelectedVectorStore(null);
            svc.GetLastSelectedVectorStore().ShouldBeNull();
        }
    }
}
