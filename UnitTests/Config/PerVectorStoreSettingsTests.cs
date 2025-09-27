// File: UnitTests/Config/PerVectorStoreSettingsTests.cs
using DocXHandler;
using oaiUI.Config;
using Shouldly;

namespace UnitTests.Config
{
    [TestFixture]
    public sealed class PerVectorStoreSettingsTests
    {
        private static VectorStoreConfig Global()
        {
            return new VectorStoreConfig
            {
                ExcludedFiles = new List<string> { "*.bin", "*.exe" },
                ExcludedFolders = new List<string> { "bin", "obj" }
            };
        }

        [Test]
        public void From_EqualToGlobal_TreatedAsInherit()
        {
            var global = Global();
            var per = new VectorStoreConfig
            {
                ExcludedFiles = new List<string> { "*.exe", "*.bin" },
                ExcludedFolders = new List<string> { "obj", "bin" }
            };

            var vm = PerVectorStoreSettings.From("vsA", global, per);

            vm.UseCustomExcludedFiles.ShouldBeFalse();
            vm.UseCustomExcludedFolders.ShouldBeFalse();
        }

        [Test]
        public void From_DifferentThanGlobal_TreatedAsCustom()
        {
            var global = Global();
            var per = new VectorStoreConfig
            {
                ExcludedFiles = new List<string> { "*.tmp" },
                ExcludedFolders = new List<string> { "dist" }
            };

            var vm = PerVectorStoreSettings.From("vsB", global, per);

            vm.UseCustomExcludedFiles.ShouldBeTrue();
            vm.UseCustomExcludedFolders.ShouldBeTrue();
            vm.CustomExcludedFiles.ShouldBe(new[] { "*.tmp" }, ignoreOrder: true);
            vm.CustomExcludedFolders.ShouldBe(new[] { "dist" }, ignoreOrder: true);
        }

        [Test]
        public void Save_MergesIntoDictionary_RespectsInheritance()
        {
            var global = Global();
            var all = new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);

            var vmInherit = new PerVectorStoreSettings("vsInherit", false, false, Array.Empty<string>(), Array.Empty<string>());
            PerVectorStoreSettings.Save(all, vmInherit, global);

            all.ShouldContainKey("vsInherit");
            all["vsInherit"].ExcludedFiles.ShouldBe(global.ExcludedFiles, ignoreOrder: true);
            all["vsInherit"].ExcludedFolders.ShouldBe(global.ExcludedFolders, ignoreOrder: true);

            var vmCustomFiles = new PerVectorStoreSettings("vsC", true, false, new[] { "*.log" }, Array.Empty<string>());
            PerVectorStoreSettings.Save(all, vmCustomFiles, global);

            all["vsC"].ExcludedFiles.ShouldBe(new[] { "*.log" }, ignoreOrder: true);
            all["vsC"].ExcludedFolders.ShouldBe(global.ExcludedFolders, ignoreOrder: true);
        }

        [Test]
        public void SaveAll_LoadAll_RoundTrip_DoesNotBreakShape()
        {
            var global = Global();
            var temp = Path.Combine(Path.GetTempPath(), $"vecstores-{Guid.NewGuid():N}.json");

            try
            {
                var dict = new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);
                var vm = new PerVectorStoreSettings("round", true, true, new[] { "*.cache" }, new[] { ".git" });
                PerVectorStoreSettings.Save(dict, vm, global);

                VectorStoreConfig.SaveAll(dict, temp);

                var reloaded = VectorStoreConfig.LoadAll(temp);
                reloaded.ShouldContainKey("round");
                reloaded["round"].ExcludedFiles.ShouldBe(new[] { "*.cache" }, ignoreOrder: true);
                reloaded["round"].ExcludedFolders.ShouldBe(new[] { ".git" }, ignoreOrder: true);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { /* ignore */ }
            }
        }
    }
}
