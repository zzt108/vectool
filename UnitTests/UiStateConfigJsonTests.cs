// File: UnitTests/UiStateConfigJsonTests.cs
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using VecTool.Core.Configuration;

namespace UnitTests
{
    [TestFixture]
    public sealed class UiStateConfigJsonTests
    {
        private string _tempDir = default!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "vectool-Ui-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); } catch { /* ignore */ }
        }

        [Test]
        public void Load_WithoutFile_ReturnsDefaults()
        {
            var state = UiStateConfig.Load(_tempDir);
            state.ShouldNotBeNull();
            state.RecentFilesColumnWidths.Count.ShouldBe(0);
            state.RecentFilesRowHeightScale.ShouldBeNull();
        }

        [Test]
        public void Save_Then_Load_RoundTripsValues()
        {
            var before = new UiStateConfig.UiState
            {
                RecentFilesRowHeightScale = 1.25
            };
            before.RecentFilesColumnWidths["File"] = 420;
            before.RecentFilesColumnWidths["Type"] = 120;

            UiStateConfig.Save(before, _tempDir);

            var after = UiStateConfig.Load(_tempDir);
            after.RecentFilesRowHeightScale.ShouldBe(1.25);
            after.RecentFilesColumnWidths.ShouldContainKeyAndValue("File", 420);
            after.RecentFilesColumnWidths.ShouldContainKeyAndValue("Type", 120);
        }
    }
}
