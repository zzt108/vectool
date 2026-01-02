using NUnit.Framework;
using Shouldly;
using VecTool.Configuration;
using VecTool.Core.Configuration;
using VecTool.Core.RecentFiles;

namespace UnitTests
{
    [TestFixture]
    public sealed class UiStateConfigTests
    {
        [Test]
        public void Should_Persist_Filter_And_StoreId()
        {
            var store = new InMemorySettingsStore();
            var cfg = new UiStateConfig(store);

            cfg.SetRecentFilesFilter(VectorStoreLinkFilter.SpecificStore);
            cfg.SetRecentFilesSpecificStoreId("vsX");

            cfg.GetRecentFilesFilter().ShouldBe(VectorStoreLinkFilter.SpecificStore);
            cfg.GetRecentFilesSpecificStoreId().ShouldBe("vsX");
        }

        [Test]
        public void Should_Persist_Last_Selected_File()
        {
            var store = new InMemorySettingsStore();
            var cfg = new UiStateConfig(store);

            cfg.SetLastSelectedRecentFilePath("c:\\temp\\a.txt");

            cfg.GetLastSelectedRecentFilePath().ShouldBe("c:\\temp\\a.txt");
        }

        [Test]
        public void Should_Default_Filter_To_All()
        {
            var cfg = new UiStateConfig(new InMemorySettingsStore());
            cfg.GetRecentFilesFilter().ShouldBe(VectorStoreLinkFilter.All);
        }
    }
}
