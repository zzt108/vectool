using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using LogCtxShared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VecTool.Handlers;
using VecTool.Studio.Commands;
using VecTool.Studio.Versioning;

namespace VecTool.Studio;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly IUserInterface? _ui;
    private readonly IServiceProvider? _serviceProvider;
    private readonly ILogger<MainWindow>? _logger;

    private string _statusText = "Ready";
    private int _progressValue;
    private int _progressMaximum = 100;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
    }

    public int ProgressValue
    {
        get => _progressValue;
        set
        {
            if (_progressValue != value)
            {
                _progressValue = value;
                OnPropertyChanged();
            }
        }
    }

    public int ProgressMaximum
    {
        get => _progressMaximum;
        set
        {
            if (_progressMaximum != value)
            {
                _progressMaximum = value;
                OnPropertyChanged();
            }
        }
    }

    // ✅ NEW: Menu commands (Phase 2 Step 2)
    public ICommand ExitCommand { get; private set; }

    public ICommand ConvertToMarkdownCommand { get; private set; }
    public ICommand GetGitChangesCommand { get; private set; }
    public ICommand FileSizeSummaryCommand { get; private set; }
    public ICommand AboutCommand { get; private set; }

    // Designer constructor
    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        // Safe defaults for designer / early runtime
        ExitCommand = new SimpleCommand(() => { });
        ConvertToMarkdownCommand = new SimpleCommand(() => { });
        GetGitChangesCommand = new SimpleCommand(() => { });
        FileSizeSummaryCommand = new SimpleCommand(() => { });
        AboutCommand = new SimpleCommand(() => { });
    }

    // DI-enabled constructor
    public MainWindow(
        IUserInterface ui,
        IServiceProvider serviceProvider,
        ILogger<MainWindow> logger) : this()
    {
        _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ExitCommand = new SimpleCommand(OnExit);
        ConvertToMarkdownCommand = new SimpleCommand(OnConvertToMarkdown);
        GetGitChangesCommand = new SimpleCommand(OnGetGitChanges);
        FileSizeSummaryCommand = new SimpleCommand(OnFileSizeSummary);
        AboutCommand = new SimpleCommand(OnAbout);
    }

    // Existing smoke test button handler (keep for now)
    private void OnTestButtonClick(object? sender, RoutedEventArgs e)
    {
        StatusText = $"Test Status at {DateTime.Now:HH:mm:ss}";
        ProgressValue = new Random().Next(0, 100);

        _ui?.UpdateStatus("Test via IUserInterface");
    }

    private void OnExit()
    {
        using Props p = _logger?.SetContext()
            .Add("CommandName", "Exit")
            .Add("Action", "MenuClick")!;

        _logger?.LogInformation("Exit command invoked");

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
            return;
        }

        Close();
    }

    private void OnConvertToMarkdown()
    {
        using Props p = _logger?.SetContext()
            .Add("CommandName", "ConvertToMarkdown")
            .Add("Action", "MenuClick")!;

        _logger?.LogInformation("Convert to Markdown command invoked (stub)");

        _ui?.ShowMessage(
            message: "Feature integration comes in Step 4. This is just command plumbing.",
            title: "Convert to Markdown",
            type: MessageType.Information);
    }

    private void OnGetGitChanges()
    {
        using Props p = _logger?.SetContext()
            .Add("CommandName", "GetGitChanges")
            .Add("Action", "MenuClick")!;

        _logger?.LogInformation("Get Git Changes command invoked (stub)");

        _ui?.ShowMessage(
            message: "Feature integration comes later in Phase 2.",
            title: "Get Git Changes",
            type: MessageType.Information);
    }

    private void OnFileSizeSummary()
    {
        using Props p = _logger?.SetContext()
            .Add("CommandName", "FileSizeSummary")
            .Add("Action", "MenuClick")!;

        _logger?.LogInformation("File Size Summary command invoked (stub)");

        _ui?.ShowMessage(
            message: "Feature integration comes later in Phase 2.",
            title: "File Size Summary",
            type: MessageType.Information);
    }

    private void OnAbout()
    {
        using Props p = _logger?.SetContext()
            .Add("CommandName", "About")
            .Add("Action", "MenuClick")!;

        _logger?.LogInformation("About command invoked");

        var versionProvider = _serviceProvider?.GetService<IVersionProvider>();
        var versionText = versionProvider is null
            ? "VecTool.Studio (version info unavailable)"
            : $"{versionProvider.ApplicationName} v{versionProvider.FileVersion}";

        _ui?.ShowMessage(
            message: $"{versionText}\r\n\r\nAvalonia Migration: Phase 2",
            title: "About",
            type: MessageType.Information);
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}