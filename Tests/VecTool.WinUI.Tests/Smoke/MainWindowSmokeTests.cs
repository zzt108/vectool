// ✅ FULL FILE VERSION
// Path: tests/UnitTests/MainWindowSmokeTests.cs
using NUnit.Framework;
using Shouldly;
using System;
using System.Linq;
using VecTool.Configuration;

namespace VecTool.WinUI.Tests.Smoke
{
    /// <summary>
    /// Smoke tests for MainWindow Settings tab logic.
    /// Tests business logic (SettingsViewModel) without WinUI controls.
    /// </summary>
    [TestFixture]
    public class MainWindowSmokeTests
    {
        #region Exclusion Settings Tests

        [Test]
        public void ShouldLoadCorrectExclusionSettings()
        {
            // Arrange
            var global = VectorStoreConfig.FromAppConfig();

            // Act - Test ViewModel logic without UI controls (no TextBox/CheckBox instantiation)
            var vm = SettingsViewModel.Load("TestStore", global, perStore: null);

            // Assert - Effective settings should match global defaults when no custom config exists
            vm.CustomExcludedFiles.ShouldNotBeEmpty("Global config should have excluded files");
            vm.CustomExcludedFolders.ShouldNotBeEmpty("Global config should have excluded folders");
            vm.CustomExcludedFiles.Count.ShouldBe(global.ExcludedFiles.Count);
            vm.CustomExcludedFolders.Count.ShouldBe(global.ExcludedFolders.Count);

            // Default should inherit from global (not custom)
            vm.UseCustomExcludedFiles.ShouldBeFalse("Default should inherit files from global");
            vm.UseCustomExcludedFolders.ShouldBeFalse("Default should inherit folders from global");
        }

        [Test]
        public void ShouldDetectCustomVsInheritedSettings()
        {
            // Arrange
            var global = new VectorStoreConfig
            {
                ExcludedFiles = new() { "*.bin", "*.exe" },
                ExcludedFolders = new() { "bin", "obj" }
            };

            var custom = new VectorStoreConfig
            {
                ExcludedFiles = new() { "*.log" }, // Different from global
                ExcludedFolders = new() { "bin", "obj" } // Same as global
            };

            // Act
            var vm = SettingsViewModel.Load("CustomStore", global, custom);

            // Assert
            vm.UseCustomExcludedFiles.ShouldBeTrue("Files differ from global, should use custom");
            vm.UseCustomExcludedFolders.ShouldBeFalse("Folders match global, should inherit");
            vm.CustomExcludedFiles.ShouldBe(new[] { "*.log" }, ignoreOrder: true);
            vm.CustomExcludedFolders.ShouldBe(new[] { "bin", "obj" }, ignoreOrder: true);
        }

        [Test]
        public void ShouldHandleEmptyCustomConfig()
        {
            // Arrange
            var global = new VectorStoreConfig
            {
                ExcludedFiles = new() { "*.bin" },
                ExcludedFolders = new() { "bin" }
            };

            var empty = new VectorStoreConfig
            {
                ExcludedFiles = new(),
                ExcludedFolders = new()
            };

            // Act
            var vm = SettingsViewModel.Load("EmptyStore", global, empty);

            // Assert
            vm.UseCustomExcludedFiles.ShouldBeTrue("Empty list is custom, not inherited");
            vm.UseCustomExcludedFolders.ShouldBeTrue("Empty list is custom, not inherited");
            vm.CustomExcludedFiles.ShouldBeEmpty();
            vm.CustomExcludedFolders.ShouldBeEmpty();
        }

        #endregion

        #region Text Parsing Tests

        [Test]
        public void ParseMultilineTextShouldHandleVariousFormats()
        {
            // Arrange - Mix of \r\n (Windows) and \n (Unix) line endings with extra whitespace
            var input = "*.bin\r\n*.exe\n  *.log  \n\n*.tmp";

            // Act
            var result = SettingsViewModel.ParseMultilineText(input);

            // Assert
            result.Count.ShouldBe(4);
            result.ShouldContain("*.bin");
            result.ShouldContain("*.exe");
            result.ShouldContain("*.log");
            result.ShouldContain("*.tmp");
        }

        [Test]
        public void ParseMultilineTextShouldRemoveDuplicates()
        {
            // Arrange - Case-insensitive duplicates
            var input = "*.bin\r\n*.BIN\r\n*.exe\r\n*.EXE";

            // Act
            var result = SettingsViewModel.ParseMultilineText(input);

            // Assert
            result.Count.ShouldBe(2, "Duplicates should be removed (case-insensitive)");
            result.ShouldContain("*.bin");
            result.ShouldContain("*.exe");
        }

        [Test]
        public void ParseMultilineTextShouldHandleEmptyInput()
        {
            // Act
            var resultNull = SettingsViewModel.ParseMultilineText(null);
            var resultEmpty = SettingsViewModel.ParseMultilineText(string.Empty);
            var resultWhitespace = SettingsViewModel.ParseMultilineText("   \r\n\n   ");

            // Assert
            resultNull.ShouldBeEmpty();
            resultEmpty.ShouldBeEmpty();
            resultWhitespace.ShouldBeEmpty();
        }

        #endregion

        #region Persistence Tests

        [Test]
        public void ShouldSaveCustomSettings()
        {
            // Arrange
            var global = new VectorStoreConfig
            {
                ExcludedFiles = new() { "*.bin" },
                ExcludedFolders = new() { "bin" }
            };

            var all = new Dictionary<string, VectorStoreConfig>();
            var vm = new SettingsViewModel
            {
                VectorStoreName = "TestStore",
                UseCustomExcludedFiles = true,
                UseCustomExcludedFolders = false,
                CustomExcludedFiles = new() { "*.log", "*.tmp" },
                CustomExcludedFolders = new() { "bin" } // Same as global (inherited)
            };

            // Act
            SettingsViewModel.Save(all, vm, global);

            // Assert
            all.ShouldContainKey("TestStore");
            var saved = all["TestStore"];
            saved.ExcludedFiles.ShouldBe(new[] { "*.log", "*.tmp" }, ignoreOrder: true);
            saved.ExcludedFolders.ShouldBe(new[] { "bin" }, ignoreOrder: true);
        }

        [Test]
        public void ShouldConvertToEffectiveConfig()
        {
            // Arrange
            var global = new VectorStoreConfig
            {
                ExcludedFiles = new() { "*.bin" },
                ExcludedFolders = new() { "bin" }
            };

            var existing = new VectorStoreConfig
            {
                FolderPaths = new() { @"C:\Projects\MyProject" } // Should be preserved
            };

            var vm = new SettingsViewModel
            {
                VectorStoreName = "TestStore",
                UseCustomExcludedFiles = true,
                UseCustomExcludedFolders = false,
                CustomExcludedFiles = new() { "*.log" },
                CustomExcludedFolders = new() { "bin" }
            };

            // Act
            var result = vm.ToEffectiveVectorStoreConfig(global, existing);

            // Assert
            result.ExcludedFiles.ShouldBe(new[] { "*.log" }, ignoreOrder: true);
            result.ExcludedFolders.ShouldBe(new[] { "bin" }, ignoreOrder: true);
            result.FolderPaths.ShouldBe(new[] { @"C:\Projects\MyProject" },true, "FolderPaths should be preserved");
        }

        #endregion

        #region Validation Tests

        [Test]
        public void LoadShouldThrowOnNullArguments()
        {
            // Arrange
            var global = new VectorStoreConfig();

            // Act & Assert
            Should.Throw<ArgumentException>(() => SettingsViewModel.Load(null, global, null));
            Should.Throw<ArgumentException>(() => SettingsViewModel.Load(string.Empty, global, null));
            Should.Throw<ArgumentException>(() => SettingsViewModel.Load("  ", global, null));
            Should.Throw<ArgumentNullException>(() => SettingsViewModel.Load("Test", null, null));
        }

        [Test]
        public void SaveShouldThrowOnNullArguments()
        {
            // Arrange
            var all = new Dictionary<string, VectorStoreConfig>();
            var vm = new SettingsViewModel { VectorStoreName = "Test" };
            var global = new VectorStoreConfig();

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => SettingsViewModel.Save(null, vm, global));
            Should.Throw<ArgumentNullException>(() => SettingsViewModel.Save(all, null, global));
            Should.Throw<ArgumentNullException>(() => SettingsViewModel.Save(all, vm, null));
        }

        #endregion
    }
}
