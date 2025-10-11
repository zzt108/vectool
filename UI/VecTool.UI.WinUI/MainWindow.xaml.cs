//// FULL FILE VERSION
//// Path: src/UI/VecTool.UI.WinUI/MainWindow.xaml.cs

//// Required Imports Template
//using NUnit.Framework;
//using Shouldly;
//using System;
//using NLog; // NLog is mandatory for structured logging

//using Microsoft.UI.Xaml;

//namespace VecTool.UI.WinUI
//{
//    public sealed partial class MainWindow : Window
//    {
//        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

//        public MainWindow()
//        {
//            InitializeComponent();
//            Log.Info("MainWindow constructed at {TimestampUtc}", DateTime.UtcNow);
//        }
//    }
//}

// Required Imports Template
using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging

using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using System.Threading.Tasks;
using VecTool.Handlers;
using VecTool.Configuration;
using VecTool.RecentFiles;

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
            // DI would provide these in the real app
            recentFilesManager = new RecentFilesManager();
            userInterface = new WinUiUserInterface(this, StatusText, StatusProgress);
            ContentHost.Navigate(typeof(RecentFilesPage));
        }

        private async void ConvertToMd_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("ConvertToMd invoked");
            userInterface.WorkStart("Generating MD file...", Array.Empty<string>());
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

        private async void AboutMenu_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("About invoked");
            // Show ContentDialog bound to IVersionProvider with identical labels/values
            var dlg = new AboutDialog(); // page/dialog that reads IVersionProvider
            dlg.XamlRoot = this.Content.XamlRoot;
            await dlg.ShowAsync();
        }

        // Helpers (stubs mimic WinForms helpers for parity)
        private (string[]? folders, string? outputPath) SelectFoldersAndOutputAsync(string ext, string title) => (Array.Empty<string>(), null);
        private string? TryFindSolutionPath() => null;
        private string GetSelectedVectorStoreName() => "default";
        private VectorStoreConfig GetCurrentVectorStoreConfig() => new VectorStoreConfig();
    }
}
