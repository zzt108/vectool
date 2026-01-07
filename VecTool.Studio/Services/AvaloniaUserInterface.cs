using System;
using Avalonia.Threading;
using LogCtxShared; // ✅ NEW
using Microsoft.Extensions.Logging;
using VecTool.Configuration.Logging; // ✅ NEW
using VecTool.Handlers;

namespace VecTool.Studio.Services
{
    public class AvaloniaUserInterface : IUserInterface
    {
        // ✅ MODIFIED: Static logger via AppLogger (no DI injection)
        private static readonly ILogger logger = AppLogger.For<AvaloniaUserInterface>();

        public int TotalWork { get; set; }

        // ✅ REMOVED: No logger parameter in constructor
        public AvaloniaUserInterface()
        {
            // ✅ NEW: Log instantiation with LogCtx
            using (Props p = logger.SetContext()
                .Add("Operation", "AvaloniaUserInterface.Construct"))
            {
                logger.LogInformation("AvaloniaUserInterface instance created");
            }
        }

        public void WorkStart(string workText, IEnumerable<string> selectedFolders)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // ✅ MODIFIED: Use LogCtx for structured logging
                using (Props p = logger.SetContext()
                    .Add("WorkText", workText)
                    .Add("FolderCount", selectedFolders?.Count() ?? 0))
                {
                    logger.LogInformation("Work started");
                }
                // TODO: Bind to MainWindow status bar
            });
        }

        public void WorkFinish()
        {
            Dispatcher.UIThread.Post(() =>
            {
                logger.LogInformation("Work finished");
                // TODO: Reset MainWindow progress bar
            });
        }

        public void UpdateStatus(string statusText)
        {
            Dispatcher.UIThread.Post(() =>
            {
                using (Props p = logger.SetContext()
                    .Add("StatusText", statusText))
                {
                    logger.LogDebug("Status updated");
                }
                // TODO: Bind to MainWindow status bar
            });
        }

        public void UpdateProgress(int current)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var percentage = (TotalWork > 0) ? (current * 100) / TotalWork : 0;

                using (Props p = logger.SetContext()
                    .Add("Current", current)
                    .Add("Total", TotalWork)
                    .Add("Percentage", percentage))
                {
                    logger.LogTrace("Progress updated");
                }
                // TODO: Bind to MainWindow progress bar
            });
        }

        public void ShowMessage(string message, string title, MessageType type)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var level = type switch
                {
                    MessageType.Information => LogLevel.Information,
                    MessageType.Warning => LogLevel.Warning,
                    MessageType.Error => LogLevel.Error,
                    _ => LogLevel.Information,
                };

                using (Props p = logger.SetContext()
                    .Add("Title", title)
                    .Add("MessageType", type.ToString()))
                {
                    logger.Log(level, message);
                }
                // TODO: Show Avalonia MessageBox or ContentDialog
            });
        }
    }
}