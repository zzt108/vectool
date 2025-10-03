// Path: UnitTests/Config/PerVectorStoreSettingsTests.cs
using NUnit.Framework;
using oaiUI.Config;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using VecTool.Configuration;

namespace UnitTests.Config
{
    [TestFixture]
    public class PerVectorStoreSettingsTests
    {
        private const string vsA = "vs-A";
        private const string vsB = "vs-B";
        private const string vsC = "vs-C";
        private const string vsInherit = "vs-Inherit";
        private const string round = "round-trip";

        private static VectorStoreConfig Global => new VectorStoreConfig
        {
            ExcludedFiles = new List<string> { ".bin", ".exe" },
            ExcludedFolders = new List<string> { "bin", "obj" }
        };

        [Test]
        public void FromWithNullPerConfigTreatedAsInherit()
        {
            // Arrange
            var global = Global;

            // Act
            var vm = PerVectorStoreSettings.From(vsA, global, null);

            // Assert
            vm.Name.ShouldBe(vsA);
            vm.UseCustomExcludedFiles.ShouldBeFalse();
            vm.UseCustomExcludedFolders.ShouldBeFalse();
            vm.CustomExcludedFiles.ShouldBe(global.ExcludedFiles, ignoreOrder: true);
            vm.CustomExcludedFolders.ShouldBe(global.ExcludedFolders, ignoreOrder: true);
        }

        [Test]
        public void FromEqualToGlobalTreatedAsInherit()
        {
            // Arrange
            var global = Global;
            var per = new VectorStoreConfig
            {
                ExcludedFiles = new List<string> { ".exe", ".bin" }, // Same files, different order
                ExcludedFolders = new List<string> { "obj", "bin" }  // Same folders, different order
            };

            // Act
            var vm = PerVectorStoreSettings.From(vsA, global, per);

            // Assert
            vm.UseCustomExcludedFiles.ShouldBeFalse();
            vm.UseCustomExcludedFolders.ShouldBeFalse();
        }

        [Test]
        public void FromDifferentThanGlobalTreatedAsCustom()
        {
            // Arrange
            var global = Global;
            var per = new VectorStoreConfig
            {
                ExcludedFiles = new List<string> { ".tmp" },
                ExcludedFolders = new List<string> { "dist" }
            };

            // Act
            var vm = PerVectorStoreSettings.From(vsB, global, per);

            // Assert
            vm.UseCustomExcludedFiles.ShouldBeTrue();
            vm.UseCustomExcludedFolders.ShouldBeTrue();
            vm.CustomExcludedFiles.ShouldBe(new[] { ".tmp" }, ignoreOrder: true);
            vm.CustomExcludedFolders.ShouldBe(new[] { "dist" }, ignoreOrder: true);
        }

        [Test]
        public void SaveMergesIntoDictionaryRespectsInheritance()
        {
            // Arrange
            var global = Global;
            var all = new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);

            // Act & Assert for inherited
            var vmInherit = new PerVectorStoreSettings(vsInherit,
                useCustomExcludedFiles: false, useCustomExcludedFolders: false,
                customExcludedFiles: Array.Empty<string>(), customExcludedFolders: Array.Empty<string>());
            PerVectorStoreSettings.Save(all, vmInherit, global);

            all.ShouldContainKey(vsInherit);
            all[vsInherit].ExcludedFiles.ShouldBe(global.ExcludedFiles, ignoreOrder: true);
            all[vsInherit].ExcludedFolders.ShouldBe(global.ExcludedFolders, ignoreOrder: true);

            // Act & Assert for custom
            var vmCustomFiles = new PerVectorStoreSettings(vsC,
                useCustomExcludedFiles: true, useCustomExcludedFolders: false,
                customExcludedFiles: new[] { ".log" }, customExcludedFolders: Array.Empty<string>());
            PerVectorStoreSettings.Save(all, vmCustomFiles, global);

            all[vsC].ExcludedFiles.ShouldBe(new[] { ".log" }, ignoreOrder: true);
            all[vsC].ExcludedFolders.ShouldBe(global.ExcludedFolders, ignoreOrder: true);
        }

        [Test]
        public void SaveAllLoadAllRoundTripDoesNotBreakShape()
        {
            // Arrange
            var global = Global;
            var temp = Path.Combine(Path.GetTempPath(), $"vecstores-{Guid.NewGuid():N}.json");

            try
            {
                var dict = new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);
                var vm = new PerVectorStoreSettings(round, true, true, new[] { ".cache" }, new[] { ".git" });
                PerVectorStoreSettings.Save(dict, vm, global);

                // Act
                VectorStoreConfig.SaveAll(dict, temp);
                var reloaded = VectorStoreConfig.LoadAll(temp);

                // Assert
                reloaded.ShouldNotBeNull();
                reloaded.ShouldContainKey(round);
                reloaded[round].ExcludedFiles.ShouldBe(new[] { ".cache" }, ignoreOrder: true);
                reloaded[round].ExcludedFolders.ShouldBe(new[] { ".git" }, ignoreOrder: true);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); }
                catch { /* ignore */ }
            }
        }
    }
}
