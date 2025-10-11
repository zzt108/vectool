// FULL FILE VERSION
// Path: UI/VecTool.UI.WinUI/MainWindow.xaml.cs

using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VecTool.Configuration;
using VecTool.Constants;
using VecTool.Handlers;
using VecTool.RecentFiles;
using VecTool.Core.Versioning;
using VecTool.UI.WinUI.About;
using VecTool.UI.WinUI.Infrastructure;

namespace VecTool.UI.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly IUserInterface userInterface;
        private readonly IRecentFilesManager recentFilesManager;

        public MainWindow()
        {
            Log.Info("MainWindow initializing");

            InitializeComponent();

            // Initialize UI wrapper with WinUI controls and dispatcher
            userInterface = new WinUiUserInterface(StatusText, StatusProgress, DispatcherQueue);

            // Initialize Recent Files system
            var config = RecentFilesConfig.FromAppConfig();
            var store = new FileRecentFilesStore(config);
            recentFilesManager = new RecentFilesManager(config, store);

            Log.Info("MainWindow initialized");
        }

        // Menu handlers (stubs - preserve parity with WinForms handlers)
        private async void ConvertToMdMenu_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("ConvertToMd invoked");
            var (folders, outputPath) = await SelectFoldersAndOutputAsync(".md", "Save Markdown File").ConfigureAwait(true);

            if (folders == null || string.IsNullOrEmpty(outputPath))
                return;

            try
            {
                userInterface.WorkStart("Generating MD...", new List<string>(folders));
                var config = GetCurrentVectorStoreConfig();
                var handler = new MDHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.ExportSelectedFolders(new List<string>(folders), outputPath, config)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated {outputPath}", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ConvertToMd failed");
                userInterface.ShowMessage($"Error: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        private async void GetGitChangesMenu_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("GetGitChanges invoked");
            var (folders, outputPath) = await SelectFoldersAndOutputAsync(".changes.md", "Save Git Changes File").ConfigureAwait(true);

            if (folders == null || string.IsNullOrEmpty(outputPath))
                return;

            try
            {
                userInterface.WorkStart("Generating Git changes...", new List<string>(folders));
                var handler = new GitChangesHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.GetGitChanges(new List<string>(folders), outputPath)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated {outputPath}", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetGitChanges failed");
                userInterface.ShowMessage($"Error: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        private async void FileSizeSummaryMenu_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("FileSizeSummary invoked");
            var (folders, outputPath) = await SelectFoldersAndOutputAsync(".summary.txt", "Save File Size Summary").ConfigureAwait(true);

            if (folders == null || string.IsNullOrEmpty(outputPath))
                return;

            try
            {
                userInterface.WorkStart("Generating file size summary...", new List<string>(folders));
                var config = GetCurrentVectorStoreConfig();
                var handler = new FileSizeSummaryHandler(userInterface, recentFilesManager);
                await Task.Run(() => handler.GenerateFileSizeSummary(new List<string>(folders), outputPath, config)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated {outputPath}", "Success", MessageType.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "FileSizeSummary failed");
                userInterface.ShowMessage($"Error: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        private async void AboutMenu_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("About invoked");

            try
            {
                // Create IVersionProvider from assembly metadata (parity with WinForms AboutForm)
                var versionProvider = new AssemblyVersionProvider();

                // AboutPage is a Page displaying version info - wrap in ContentDialog for modal behavior
                var aboutContent = new AboutPage(versionProvider);

                var dialog = new ContentDialog
                {
                    Title = "About VecTool",
                    Content = aboutContent,
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot // Critical for WinUI dialog parenting
                };

                await dialog.ShowAsync();

                Log.Info("About dialog closed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "About dialog failed");
                userInterface.ShowMessage($"Could not display About dialog: {ex.Message}", "Error", MessageType.Error);
            }
        }

        // ? NEW: Run Tests handler
        private async void RunTests_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("RunTests invoked");

            var solutionPath = TryFindSolutionPath();
            if (solutionPath is null)
            {
                userInterface.ShowMessage(
                    "Could not find VecTool.sln in parent directories.",
                    "Solution Not Found",
                    MessageType.Error);
                return;
            }

            var vsName = GetSelectedVectorStoreName();
            var handler = new TestRunnerHandler(userInterface, recentFilesManager);

            try
            {
                userInterface.WorkStart("Running unit tests...", new List<string>());
                await handler.RunTestsAsync(solutionPath, vsName, new List<string>()).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "RunTests failed");
                userInterface.ShowMessage($"Test execution failed: {ex.Message}", "Test Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        // ? NEW: Exit handler
        private void ExitMenu_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("Exit menu invoked");
            this.Close();
        }


        // Helpers (stubs - mimic WinForms helpers for parity)
        private Task<(string[]? folders, string? outputPath)> SelectFoldersAndOutputAsync(string ext, string title)
        {
            // TODO Phase 2: Implement WinUI 3 folder/file pickers
            // Use Windows.Storage.Pickers.FolderPicker and FileSavePicker with proper COM initialization
            // For now, return empty to allow compilation and preserve handler signatures
            return Task.FromResult<(string[]?, string?)>((Array.Empty<string>(), null));
        }

        private string? TryFindSolutionPath()
        {
            // TODO: Implement solution file discovery (walk up from BaseDirectory)
            return null;
        }

        private string GetSelectedVectorStoreName()
        {
            // TODO: Read from ComboBox or state when UI is complete
            return "default";
        }

        private VectorStoreConfig GetCurrentVectorStoreConfig()
        {
            // TODO: Load from persisted vector store configs
            return new VectorStoreConfig();
        }
    }
}
