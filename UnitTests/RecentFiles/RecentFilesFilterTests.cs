using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;
using VecTool.Core.RecentFiles;

namespace UnitTests.RecentFiles
{
    [TestFixture]
    public sealed class RecentFilesFilterTests
    {
        [Test]
        public void Filter_Linked_Should_Return_Only_Linked()
        {
            var items = new List<RecentFileItem>
            {
                new RecentFileItem("a.txt", null),
                new RecentFileItem("b.txt", "vs1"),
                new RecentFileItem("c.txt", "vs2")
            };
            var svc = new RecentFilesService(items);

            var result = svc.GetRecentFiles(VectorStoreLinkFilter.Linked);

            result.Count.ShouldBe(2);
            result.ShouldContain(i => i.Path == "b.txt");
            result.ShouldContain(i => i.Path == "c.txt");
        }

        [Test]
        public void Filter_Unlinked_Should_Return_Only_Unlinked()
        {
            var items = new List<RecentFileItem>
            {
                new RecentFileItem("a.txt", null),
                new RecentFileItem("b.txt", "vs1"),
                new RecentFileItem("c.txt", null)
            };
            var svc = new RecentFilesService(items);

            var result = svc.GetRecentFiles(VectorStoreLinkFilter.Unlinked);

            result.Count.ShouldBe(2);
            result.ShouldContain(i => i.Path == "a.txt");
            result.ShouldContain(i => i.Path == "c.txt");
        }

        [Test]
        public void Filter_SpecificStore_Should_Return_Only_That_Store()
        {
            var items = new List<RecentFileItem>
            {
                new RecentFileItem("a.txt", null),
                new RecentFileItem("b.txt", "vs1"),
                new RecentFileItem("c.txt", "vs2"),
                new RecentFileItem("d.txt", "VS1") // case-insensitive
            };
            var svc = new RecentFilesService(items);

            var result = svc.GetRecentFiles(VectorStoreLinkFilter.SpecificStore, "vs1");

            result.Count.ShouldBe(2);
            result.ShouldContain(i => i.Path == "b.txt");
            result.ShouldContain(i => i.Path == "d.txt");
        }
    }
}
