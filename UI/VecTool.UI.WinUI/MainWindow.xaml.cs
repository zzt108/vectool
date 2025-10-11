// Required Imports Template
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NLog; // NLog is mandatory for structured logging
using VecTool.Configuration;
using VecTool.Handlers;
using VecTool.RecentFiles;
using VecTool.UI.WinUI.About;
using VecTool.UI.WinUI.Infrastructure;
using VecTool.UI.Versioning; // For AssemblyVersionProvider


namespace Vectool.UI.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly IRecentFilesManager recentFilesManager;
        private readonly IUserInterface userInterface;
        private readonly DispatcherQueue ui;

        public MainWindow()
        {
            InitializeComponent();
            ui = DispatcherQueue.GetForCurrentThread();
            var config = RecentFilesConfig.FromAppConfig(); // Load from app.config with defaults
            var store = new FileRecentFilesStore(config);
            recentFilesManager = new RecentFilesManager(config, store);
            //userInterface = new WinUiUserInterface(this, StatusText, StatusProgress);
            userInterface = new WinUiUserInterface(StatusText, StatusProgress, ui);
            ContentHost.Navigate(typeof(RecentFilesPage));
        }

        private async void ConvertToMd_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("ConvertToMd invoked");
            userInterface.WorkStart("Generating MD file...", Array.Empty<string>().ToList());
            try
            {
                // Mirror WinForms flow: MDHandler.ExportSelectedFolders(...)
                var handler = new MDHandler(userInterface, recentFilesManager);
                var config = GetCurrentVectorStoreConfig();
                var (folders, outputPath) = await SelectFoldersAndOutputAsync(".md", "Save as Markdown...");
                if (folders is null || outputPath is null) return;

                await Task.Run(() => handler.ExportSelectedFolders(folders, outputPath, config)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated file at {outputPath}", "Success", MessageType.Information);
                Log.Info("MD export completed at {OutputPath}", outputPath);
            }
            catch (Exception ex)
            {
                var evt = new LogEventInfo(LogLevel.Error, Log.Name, "ConvertToMd failed") { Exception = ex };
                Log.Log(evt);
                userInterface.ShowMessage($"An error occurred: {ex.Message}", "Error", MessageType.Error);
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        private async void GetGitChanges_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("GetGitChanges invoked");
            try
            {
                var handler = new GitChangesHandler(userInterface, recentFilesManager);
                var (folders, outputPath) = await SelectFoldersAndOutputAsync(".md", "Save Git Changes As...");
                if (folders is null || outputPath is null) return;

                await Task.Run(() => handler.GetGitChanges(folders, outputPath)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated file at {outputPath}", "Success", MessageType.Information);
                Log.Info("Git changes file created at {OutputPath}", outputPath);
            }
            catch (Exception ex)
            {
                var evt = new LogEventInfo(LogLevel.Error, Log.Name, "GetGitChanges failed") { Exception = ex };
                Log.Log(evt);
                userInterface.ShowMessage($"An error occurred: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private async void FileSizeSummary_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("FileSizeSummary invoked");
            try
            {
                var handler = new FileSizeSummaryHandler(userInterface, recentFilesManager);
                var config = GetCurrentVectorStoreConfig();
                var (folders, outputPath) = await SelectFoldersAndOutputAsync(".txt", "Save File Size Summary As...");
                if (folders is null || outputPath is null) return;

                await Task.Run(() => handler.GenerateFileSizeSummary(folders, outputPath, config)).ConfigureAwait(true);
                userInterface.ShowMessage($"Successfully generated file at {outputPath}", "Success", MessageType.Information);
                Log.Info("File size summary created at {OutputPath}", outputPath);
            }
            catch (Exception ex)
            {
                var evt = new LogEventInfo(LogLevel.Error, Log.Name, "FileSizeSummary failed") { Exception = ex };
                Log.Log(evt);
                userInterface.ShowMessage($"An error occurred: {ex.Message}", "Error", MessageType.Error);
            }
        }

        private async void RunTests_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("RunTests invoked");
            try
            {
                var handler = new TestRunnerHandler(userInterface, recentFilesManager);
                var solutionPath = TryFindSolutionPath();
                if (solutionPath is null)
                {
                    userInterface.ShowMessage("Could not find VecTool.sln in parent directories.", "Solution Not Found", MessageType.Error);
                    return;
                }
                var vsName = GetSelectedVectorStoreName();
                await handler.RunTestsAsync(solutionPath, vsName, Array.Empty<string>()).ConfigureAwait(true);
                Log.Info("RunTests completed for {Solution}", solutionPath);
            }
            catch (Exception ex)
            {
                var evt = new LogEventInfo(LogLevel.Error, Log.Name, "RunTests failed") { Exception = ex };
                Log.Log(evt);
                userInterface.ShowMessage($"Test execution failed: {ex.Message}", "Test Error", MessageType.Error);
            }
        }

        private void ExitMenu_Click(object sender, RoutedEventArgs e) => this.Close();

        // ✅ NEW:
        private async void AboutMenuClick(object sender, RoutedEventArgs e)
        {
            Log.Info("About invoked");

            // Create IVersionProvider from assembly metadata (mimic WinForms AboutForm)
            var versionProvider = new AssemblyVersionProvider(); // Or resolve from DI

            // AboutPage is a ContentDialog wrapper - instantiate with IVersionProvider
            var dialog = new ContentDialog
            {
                Title = "About VecTool",
                Content = new AboutPage(versionProvider),
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();

            Log.Info("About dialog closed");
        }

        // Helpers (stubs mimic WinForms helpers for parity)
        private Task<(string[]? folders, string? outputPath)> SelectFoldersAndOutputAsync(string ext, string title)
        {
            // TODO: Implement WinUI 3 file/folder pickers (StorageFolder, FileSavePicker)
            return Task.FromResult<(string[]?, string?)>((Array.Empty<string>(), null));
        }
        private string? TryFindSolutionPath() => null;
        private string GetSelectedVectorStoreName() => "default";
        private VectorStoreConfig GetCurrentVectorStoreConfig() => new VectorStoreConfig();
    }
}
