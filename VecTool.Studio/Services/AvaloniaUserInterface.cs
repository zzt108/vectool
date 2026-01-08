using Avalonia.Threading;
using LogCtxShared;
using Microsoft.Extensions.Logging;
using System;
using VecTool.Configuration.Logging;
using VecTool.Handlers;

namespace VecTool.Studio.Services
{
    /// <summary>
    /// Avalonia implementation of IUserInterface.
    /// Phase 2: Now event-driven (StatusChanged, ProgressChanged, MessageShown events).
    /// </summary>
    public class AvaloniaUserInterface : IUserInterface
    {
        private static readonly ILogger logger = AppLogger.For<AvaloniaUserInterface>();

        public int TotalWork { get; set; }

        // ✅ NEW: Event definitions (Phase 2 Step 3) - renamed to avoid namespace conflicts
        public event EventHandler<UIStatusChangedEventArgs>? StatusChanged;

        public event EventHandler<UIProgressChangedEventArgs>? ProgressChanged;

        public event EventHandler<UIMessageShownEventArgs>? MessageShown;

        public AvaloniaUserInterface()
        {
            using (Props p = logger.SetContext()
                .Add("Operation", "AvaloniaUserInterface.Construct"))
            {
                logger.LogInformation("AvaloniaUserInterface instance created");
            }
        }

        public void WorkStart(string workText, IEnumerable<string>? selectedFolders)
        {
            Dispatcher.UIThread.Post(() =>
            {
                using (Props p = logger.SetContext()
                    .Add("WorkText", workText)
                    .Add("FolderCount", selectedFolders?.Count() ?? 0))
                {
                    logger.LogInformation("Work started");
                }
            });
        }

        public void WorkFinish()
        {
            Dispatcher.UIThread.Post(() =>
            {
                logger.LogInformation("Work finished");
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
                    // ✅ NEW: Raise event instead of direct UI manipulation
                    StatusChanged?.Invoke(this, new UIStatusChangedEventArgs(statusText));
                }
            });
        }

        /// <summary>
        /// Updates progress with current value. Maximum is derived from TotalWork property.
        /// IUserInterface signature: void UpdateProgress(int current) - 1 parameter only!
        /// </summary>
        public void UpdateProgress(int current)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var maximum = TotalWork > 0 ? TotalWork : 100;
                var percentage = maximum > 0
                    ? (current * 100) / maximum
                    : 0;

                using (Props p = logger.SetContext()
                    .Add("Current", current)
                    .Add("Maximum", maximum)
                    .Add("Percentage", percentage))
                {
                    logger.LogTrace("Progress updated");
                    // ✅ NEW: Raise event with current and calculated maximum
                    ProgressChanged?.Invoke(this, new UIProgressChangedEventArgs(current, maximum));
                }
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
                    // ✅ NEW: Raise event for message dialog (Phase 2 Step 6 will show dialog)
                    MessageShown?.Invoke(this, new UIMessageShownEventArgs(title, message, type));
                }
            });
        }
    }
}