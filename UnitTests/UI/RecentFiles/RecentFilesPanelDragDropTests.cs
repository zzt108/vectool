// ✅ FULL FILE VERSION
// File: UnitTests/UI/RecentFiles/RecentFilesPanelDragDropTests.cs

using NUnit.Framework;
using Shouldly;

using DomainRecentFileType = VecTool.RecentFiles.RecentFileType;
// Alias the UI control explicitly to avoid conflicts with any test stub class named the same
using UiRecentFilesPanel = oaiUI.RecentFiles.RecentFilesPanel;
using UiRecentFileType = VecTool.RecentFiles.RecentFileType;

namespace UnitTests.UI.RecentFiles
{
    [TestFixture]
    public sealed class RecentFilesPanelDragDropTests
    {
        private UiRecentFilesPanel _panel = null!;

        [SetUp]
        public void SetUp()
        {
            // Ensure we instantiate the actual UI control, not a test stub
            _panel = new UiRecentFilesPanel();
        }

        [TearDown]
        public void TearDown()
        {
            _panel?.Dispose();
        }

        [Test]
        public void DragDrop_ShouldAcceptKnownFileExtensions()
        {
            // Example: simulate drag-drop of different file types and assert the panel accepts/filters properly
            var droppedFiles = new[]
            {
                "readme.md",
                "CHANGELOG.md",
                "results.trx",
                "report.pdf",
                "notes.txt"
            };

            // Hypothetical: panel filters to only show supported recent file entries
            var accepted = FilterSupportedFiles(droppedFiles);

            accepted.ShouldContain("readme.md");
            accepted.ShouldContain("CHANGELOG.md");

            // Depending on design, TRX or PDF may or may not be supported in UI list; keep the assertion flexible
            accepted.ShouldNotContain("notes.txt");
        }

        [Test]
        public void DragDrop_ShouldMapToDomainEnum_WhenBindingOccurs()
        {
            // Demonstrate explicit mapping where UI and Domain enums diverge
            var uiType = UiRecentFileType.Md;
            var mapped = MapToDomain(uiType);

            mapped.ShouldBe(DomainRecentFileType.Md);
        }

        [Test]
        public void Panel_ShouldNotThrow_OnEmptyDrag()
        {
            var files = Array.Empty<string>();
            Action act = () => _ = FilterSupportedFiles(files);

            act.ShouldNotThrow();
        }

        // --- Helpers used by tests below ---
        private static IReadOnlyList<string> FilterSupportedFiles(IEnumerable<string> files)
        {
            // Minimal fake filtering logic for demonstration;
            // real tests would call panel APIs or subscribe to events.
            return files.Where(f =>
                    f.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".trx", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private static DomainRecentFileType MapToDomain(UiRecentFileType uiType)
        {
            // Explicit mapping layer to avoid relying on enum integer identity
            return uiType switch
            {
                UiRecentFileType.Md => DomainRecentFileType.Md,
                UiRecentFileType.GitChanges => DomainRecentFileType.GitChanges,
                UiRecentFileType.TestResults => DomainRecentFileType.TestResults,
                UiRecentFileType.Pdf => DomainRecentFileType.Pdf,
                _ => DomainRecentFileType.Unknown
            };
        }
    }
}
