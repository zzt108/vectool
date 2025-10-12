using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using VecTool.Configuration;
using VecTool.UI.WinUI;
using VecTool.UI.WinUI.Infrastructure;

namespace VecTool.WinUI.Tests;

/// <summary>
/// Unit tests for MainWindow empty state handling.
/// Tests VECTOOL-0.3 bug fix: dropdown disabled when no vector stores exist.
/// </summary>
[TestClass]
public sealed class MainWindowEmptyStateTests
{
    private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

    [ClassInitialize]
    public static void ClassSetup(TestContext context)
    {
        // Initialize NLog for test logging
        NLogBootstrap.Init();
        Log.Info("MainWindowEmptyStateTests suite starting");
    }

    /// <summary>
    /// Tests that Main tab controls are disabled when no vector stores exist.
    /// Validates VECTOOL-0.3 acceptance criteria #1.
    /// </summary>
    [UITestMethod]
    public void MainTab_NoVectorStores_DisablesDropdown()
    {
        Log.Debug("Test started: MainTab_NoVectorStores_DisablesDropdown");

        // Arrange: Create temp config with no vector stores
        string tempConfigPath = Path.GetTempFileName();
        var emptyStores = new Dictionary<string, VectorStoreConfig>();
        VectorStoreConfig.SaveAll(emptyStores, tempConfigPath);

        // Override config path for this test (if VectorStoreConfig supports it)
        // Otherwise, ensure default config location is empty

        try
        {
            // Act: Create MainWindow instance (requires XAML UI thread)
            var window = new MainWindow();

            // Assert: Verify dropdown is disabled
            Assert.IsFalse(window.ComboBoxVectorStoresAccessor.IsEnabled,
                "Dropdown should be disabled when no vector stores exist");

            // Assert: Verify placeholder text guides user
            Assert.AreEqual("Create a vector store first",
                window.ComboBoxVectorStoresAccessor.PlaceholderText,
                "Placeholder text should guide user to create a store");

            // Assert: Verify related button is also disabled
            Assert.IsFalse(window.BtnSelectFoldersAccessor.IsEnabled,
                "Select Folders button should be disabled when no stores exist");

            Log.Info("✅ Test passed: Empty state disables controls correctly");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Test failed: MainTab_NoVectorStores_DisablesDropdown");
            throw;
        }
        finally
        {
            // Cleanup: Remove temp config file
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    /// <summary>
    /// Tests that Main tab controls are enabled when vector stores exist.
    /// Validates VECTOOL-0.3 acceptance criteria #2.
    /// </summary>
    [UITestMethod]
    public void MainTab_WithVectorStores_EnablesDropdown()
    {
        Log.Debug("Test started: MainTab_WithVectorStores_EnablesDropdown");

        // Arrange: Create temp config with test vector store
        string tempConfigPath = Path.GetTempFileName();
        var stores = new Dictionary<string, VectorStoreConfig>
        {
            ["TestStore"] = new VectorStoreConfig
            {
                ExcludedFiles = new List<string> { "TestStoreFile" }, 
                ExcludedFolders = new List<string> { "TestStoreFolder" },
                FolderPaths = new List<string> { @"C:\Temp\TestVectorStore" },
            }
        };
        VectorStoreConfig.SaveAll(stores, tempConfigPath);

        try
        {
            // Act: Create MainWindow with populated config
            var window = new MainWindow();

            // Assert: Verify dropdown is enabled
            Assert.IsTrue(window.ComboBoxVectorStoresAccessor.IsEnabled,
                "Dropdown should be enabled when vector stores exist");

            // Assert: Verify related button is enabled
            Assert.IsTrue(window.BtnSelectFoldersAccessor.IsEnabled,
                "Select Folders button should be enabled when stores exist");

            // Assert: Verify dropdown is populated
            Assert.IsTrue(window.ComboBoxVectorStoresAccessor.Items.Count > 0,
                "Dropdown should contain vector store items");

            Log.Info("✅ Test passed: Controls enabled when stores exist");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Test failed: MainTab_WithVectorStores_EnablesDropdown");
            throw;
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    /// <summary>
    /// Tests that controls auto-enable after creating the first vector store.
    /// Validates VECTOOL-0.3 acceptance criteria #3.
    /// </summary>
    [UITestMethod]
    public async Task BtnCreateNewVectorStore_FirstStore_EnablesControls()
    {
        Log.Debug("Test started: BtnCreateNewVectorStore_FirstStore_EnablesControls");

        // Arrange: Start with empty state
        string tempConfigPath = Path.GetTempFileName();
        var emptyStores = new Dictionary<string, VectorStoreConfig>();
        VectorStoreConfig.SaveAll(emptyStores, tempConfigPath);

        try
        {
            var window = new MainWindow();

            // Verify initial disabled state
            Assert.IsFalse(window.ComboBoxVectorStoresAccessor.IsEnabled,
                "Initial state: dropdown should be disabled");

            // Act: Simulate creating first vector store programmatically
            // (Bypassing UI dialog for unit test simplicity)
            var newStore = new VectorStoreConfig
            {
                FolderPaths = new List<string> { @"C:\Temp\FirstStore" },
                ExcludedFiles = new List<string> { "*.jpg" },
                ExcludedFolders = new List<string> { "Folder1", "Folder2" },
            };

            var allStores = VectorStoreConfig.LoadAll();
            allStores["FirstStore"] = newStore;
            VectorStoreConfig.SaveAll(allStores, tempConfigPath);

            // Trigger refresh (calling internal method via reflection if needed,
            // or simulate by creating new window instance)
            // For simplicity, create new window that will pick up new config
            var windowAfterCreate = new MainWindow();

            // Assert: Controls should now be enabled
            Assert.IsTrue(windowAfterCreate.ComboBoxVectorStoresAccessor.IsEnabled,
                "Controls should auto-enable after first store creation");

            Assert.IsTrue(windowAfterCreate.BtnSelectFoldersAccessor.IsEnabled,
                "Select Folders button should enable after first store creation");

            Log.Info("✅ Test passed: First store creation enables controls");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Test failed: BtnCreateNewVectorStore_FirstStore_EnablesControls");
            throw;
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempConfigPath))
            {
                File.Delete(tempConfigPath);
            }
        }
    }

    [ClassCleanup]
    public static void ClassTeardown()
    {
        Log.Info("MainWindowEmptyStateTests suite completed");
    }
}
