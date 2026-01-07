using Avalonia.Threading;
using LogCtxShared;
using Microsoft.Extensions.Logging;
using VecTool.Handlers;

namespace VecTool.Studio.Services;

/// <summary>
/// Avalonia-specific IUserInterface implementation.
/// Maps core library UI calls → Dispatcher.UIThread for thread-safe updates.
/// (Analogous to WinFormsUserInterface but using Avalonia threading)
/// </summary>
public class AvaloniaUserInterface : IUserInterface
{
    private readonly ILogger _logger;

    public int TotalWork { get; set; }

    public AvaloniaUserInterface(ILogger<AvaloniaUserInterface> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogTrace("AvaloniaUserInterface created - ready for UI updates");
    }

    /// <summary>
    /// Starts work tracking and updates status.
    /// </summary>
    public void WorkStart(string workText, IEnumerable<string> selectedFolders)
    {
        Dispatcher.UIThread.Post(() =>
        {
            using var ctx = _logger.SetContext()
                .Add("workText", workText)
                .Add("folderCount", selectedFolders?.Count() ?? 0);

            _logger.LogInformation("Work started: {workText}", workText);

            // TODO: Bind to MainWindow status bar
            // StatusText = workText;
        });
    }

    /// <summary>
    /// Finishes work tracking and clears status.
    /// </summary>
    public void WorkFinish()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _logger.LogInformation("Work finished");

            // TODO: Reset MainWindow progress bar
            // ProgressValue = 0;
            // StatusText = "Ready";
        });
    }

    /// <summary>
    /// Updates status text (thread-safe via Dispatcher).
    /// </summary>
    public void UpdateStatus(string statusText)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _logger.LogDebug("Status: {statusText}", statusText);

            // TODO: Bind to MainWindow status bar
            // StatusText = statusText;
        });
    }

    /// <summary>
    /// Updates progress bar (thread-safe via Dispatcher).
    /// </summary>
    public void UpdateProgress(int current)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var percentage = TotalWork > 0 ? (current * 100) / TotalWork : 0;
            _logger.LogTrace("Progress: {current}/{TotalWork} ({percentage}%)", current, TotalWork, percentage);

            // TODO: Bind to MainWindow progress bar
            // ProgressValue = current;
            // ProgressMaximum = TotalWork;
        });
    }

    /// <summary>
    /// Shows message dialog (thread-safe via Dispatcher).
    /// </summary>
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

            using var ctx = _logger.SetContext()
                .Add("title", title)
                .Add("messageType", type.ToString());

            _logger.Log(level, "{title}: {message}", title, message);

            // TODO: Show Avalonia MessageBox or ContentDialog
            // await MessageBoxManager.GetMessageBoxStandard(title, message).ShowAsync();
        });
    }
}