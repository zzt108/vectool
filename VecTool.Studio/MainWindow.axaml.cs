using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using LogCtxShared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VecTool.Configuration;
using VecTool.Handlers;
using VecTool.Studio.Commands;
using VecTool.Studio.Services;
using VecTool.Studio.Versioning;

namespace VecTool.Studio
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly IUserInterface? _ui;
        private readonly IServiceProvider? _serviceProvider;
        private readonly ILogger<MainWindow> _logger;
        private string _statusText = "Ready";
        private int _progressValue;
        private int _progressMaximum = 100;

        // todo: why is it new?
        public new event PropertyChangedEventHandler? PropertyChanged;

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

        // ✅ Menu commands (Phase 2 Step 2)
        public ICommand ExitCommand { get; private set; }

        public ICommand ConvertToMarkdownCommand { get; private set; }
        public ICommand GetGitChangesCommand { get; private set; }
        public ICommand FileSizeSummaryCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }

        /// <summary>
        /// Designer constructor - safe defaults for XAML designer.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Safe defaults for designer/early runtime
            ExitCommand = new SimpleCommand(() => { });
            ConvertToMarkdownCommand = new SimpleCommand(() => { });
            GetGitChangesCommand = new SimpleCommand(() => { });
            FileSizeSummaryCommand = new SimpleCommand(() => { });
            AboutCommand = new SimpleCommand(() => { });
        }

        /// <summary>
        /// DI-enabled constructor - called from App.OnFrameworkInitializationCompleted.
        /// Phase 2 Step 3: Subscribe to IUserInterface events here.
        /// </summary>
        public MainWindow(IUserInterface _ui, IServiceProvider serviceProvider, ILogger<MainWindow> logger)
            : this()
        {
            this._ui = _ui ?? throw new ArgumentNullException(nameof(_ui));
            this._serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // ✅ Initialize menu commands (Phase 2 Step 2)
            ExitCommand = new SimpleCommand(OnExit);
            ConvertToMarkdownCommand = new SimpleCommand(OnConvertToMarkdown);
            GetGitChangesCommand = new SimpleCommand(OnGetGitChanges);
            FileSizeSummaryCommand = new SimpleCommand(OnFileSizesSummary);
            AboutCommand = new SimpleCommand(OnAbout);

            // Subscribe to IUserInterface events (Phase 2 Step 3)
            // This is the key to event-driven UI updates!
            if (_ui is AvaloniaUserInterface avaloniaUi)
            {
                avaloniaUi.StatusChanged += OnStatusChanged;
                avaloniaUi.ProgressChanged += OnProgressChanged;
                avaloniaUi.MessageShown += OnMessageShown;

                using (Props p = logger?.SetContext()
                    .Add("Operation", "MainWindow.Constructor")
                    .Add("EventsSubscribed", "StatusChanged|ProgressChanged|MessageShown"))
                {
                    logger?.LogDebug("IUserInterface events subscribed");
                }
            }
        }

        // ====================================
        // Event Handlers (Phase 2 Step 3)
        // ====================================

        /// <summary>
        /// Handles StatusChanged event from IUserInterface.
        /// Updates StatusText binding property → UI TextBlock updates automatically.
        /// </summary>
        private void OnStatusChanged(object? sender, UIStatusChangedEventArgs e)
        {
            using (Props p = _logger?.SetContext()
                .Add("EventType", "StatusChanged")
                .Add("StatusText", e.StatusText))
            {
                _logger?.LogTrace("OnStatusChanged triggered");
            }

            StatusText = e.StatusText;
        }

        /// <summary>
        /// Handles ProgressChanged event from IUserInterface.
        /// Updates ProgressValue and ProgressMaximum binding properties → UI ProgressBar updates automatically.
        /// </summary>
        private void OnProgressChanged(object? sender, UIProgressChangedEventArgs e)
        {
            using (Props p = _logger?.SetContext()
                .Add("EventType", "ProgressChanged")
                .Add("Current", e.Current)
                .Add("Maximum", e.Maximum))
            {
                _logger?.LogTrace("OnProgressChanged triggered");
            }

            ProgressValue = e.Current;
            ProgressMaximum = e.Maximum;
        }

        /// <summary>
        /// Handles MessageShown event from IUserInterface.
        /// Phase 2 Step 6 will add the actual MessageBox/ContentDialog.
        /// For now, just logs and acknowledges the event.
        /// </summary>
        private void OnMessageShown(object? sender, UIMessageShownEventArgs e)
        {
            using (Props p = _logger?.SetContext()
                .Add("EventType", "MessageShown")
                .Add("Title", e.Title)
                .Add("MessageType", e.Type.ToString()))
            {
                _logger?.LogInformation($"Message dialog event: {e.Title}");
            }

            // TODO: Phase 2 Step 6 - Show actual MessageBox or ContentDialog
            // For now, event is logged and acknowledged
        }

        // ====================================
        // Command Handlers (Phase 2 Step 2)
        // ====================================

        private void OnExit()
        {
            using (Props p = _logger?.SetContext()
                .Add("CommandName", "Exit")
                .Add("Action", "MenuClick"))
            {
                _logger?.LogInformation("Exit command invoked");

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
                else
                {
                    Close();
                }
            }
        }

        private async void OnConvertToMarkdown()
        {
            using (Props p = _logger?.SetContext()
                .Add("CommandName", "ConvertToMarkdown")
                .Add("Action", "MenuClick"))
            {
                _logger?.LogInformation("Convert to Markdown command invoked - real handler");

                try
                {
                    var handler = _serviceProvider?.GetService<MDHandler>();
                    if (handler is null)
                    {
                        _logger?.LogError("MDHandler not registered in DI container");
                        _ui?.ShowMessage(
                            message: "Handler not available. Check DI registration.",
                            title: "Error",
                            type: MessageType.Error);
                        return;
                    }

                    // Phase 2 Step 4: parameter stub (folder selection UI comes later)
                    // Keep it deterministic and not gigantic, unless you enjoy watching progress bars all day.
                    var selectedFolders = new List<string> { Environment.CurrentDirectory };

                    var outputPath = Path.Combine(
                        Path.GetTempPath(),
                        $"vectool_export_{DateTime.Now:yyyyMMdd_HHmmss}.md");

                    var cfg = new VectorStoreConfig
                    {
                        FolderPaths = selectedFolders
                    };

                    using (Props ctx = _logger?.SetContext()
                        .Add("FolderCount", selectedFolders.Count)
                        .Add("OutputPath", outputPath))
                    {
                        _logger?.LogInformation("Starting MD export");
                    }

                    await handler.ExportSelectedFoldersAsync(outputPath, cfg);

                    _ui?.ShowMessage(
                        message: $"Markdown export completed:\n{outputPath}",
                        title: "Convert to Markdown",
                        type: MessageType.Information);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Convert to Markdown failed");

                    _ui?.ShowMessage(
                        message: $"Export failed: {ex.Message}",
                        title: "Error",
                        type: MessageType.Error);
                }
            }
        }

        private void OnGetGitChanges()
        {
            using (Props p = _logger?.SetContext()
                .Add("CommandName", "GetGitChanges")
                .Add("Action", "MenuClick"))
            {
                _logger?.LogInformation("Get Git Changes command invoked (stub)");

                _ui?.ShowMessage(
                    message: "Feature integration comes later in Phase 2.",
                    title: "Get Git Changes",
                    type: MessageType.Information
                );
            }
        }

        private void OnFileSizesSummary()
        {
            using (Props p = _logger?.SetContext()
                .Add("CommandName", "FileSizeSummary")
                .Add("Action", "MenuClick"))
            {
                _logger?.LogInformation("File Size Summary command invoked (stub)");

                _ui?.ShowMessage(
                    message: "Feature integration comes later in Phase 2.",
                    title: "File Size Summary",
                    type: MessageType.Information
                );
            }
        }

        private void OnAbout()
        {
            using (Props p = _logger?.SetContext()
                .Add("CommandName", "About")
                .Add("Action", "MenuClick"))
            {
                _logger?.LogInformation("About command invoked");

                var versionProvider = _serviceProvider?.GetService<IVersionProvider>();
                var versionText = versionProvider is null
                    ? "VecTool.Studio version info unavailable"
                    : $"{versionProvider.ApplicationName} v{versionProvider.FileVersion}\n\nAvalonia Migration Phase 2";

                _ui?.ShowMessage(
                    message: versionText,
                    title: "About",
                    type: MessageType.Information
                );
            }
        }

        // ====================================
        // Existing Test Button (Phase 1 Keep)
        // ====================================

        private void OnTestButtonClick(object? sender, RoutedEventArgs e)
        {
            using (Props p = _logger?.SetContext()
                .Add("Action", "TestButtonClick"))
            {
                _logger?.LogInformation("Test button clicked");
            }

            StatusText = $"Test Status at {DateTime.Now:HH:mm:ss}";
            ProgressValue = new Random().Next(0, 100);

            // Also test IUserInterface event flow
            _ui?.UpdateStatus($"Test via IUserInterface at {DateTime.Now:HH:mm:ss}");
        }

        // ====================================
        // INotifyPropertyChanged Support
        // ====================================

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}