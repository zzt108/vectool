using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using oaiUI.Services;

namespace UnitTests.Services
{
    [TestFixture]
    public sealed class LastSelectionServiceTests
    {
        private string tempDir = null!;
        private LastSelectionService sut = null!;

        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "VecTool_LastSel_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            sut = new LastSelectionService(tempDir, "user-settings.json");
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
        }

        [Test]
        public void Get_Without_File_Returns_Null()
        {
            sut.GetLastSelectedVectorStore().ShouldBeNull();
        }

        [Test]
        public void Set_Then_Get_Roundtrips_Value()
        {
            sut.SetLastSelectedVectorStore("my-store");
            sut.GetLastSelectedVectorStore().ShouldBe("my-store");
        }

        [Test]
        public void Set_Null_Clears_Value()
        {
            sut.SetLastSelectedVectorStore("x");
            sut.SetLastSelectedVectorStore(null);
            sut.GetLastSelectedVectorStore().ShouldBeNull();
        }
    }
}
