using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using VecTool.Handlers;

namespace VecTool.Studio
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly IUserInterface? _ui;
        private string _statusText = "Ready";
        private int _progressValue = 0;
        private int _progressMaximum = 100;

        // PropertyChanged event for binding infrastructure
        public event PropertyChangedEventHandler? PropertyChanged;

        // Bindable Properties
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

        // Parameterless constructor for designer
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; // Bind to self
        }

        // DI-enabled constructor
        public MainWindow(IUserInterface ui) : this()
        {
            _ui = ui;
        }

        // Test button handler for smoke test
        private void OnTestButtonClick(object? sender, RoutedEventArgs e)
        {
            // Phase 1 manual test: directly update properties
            StatusText = $"Test Status at {DateTime.Now:HH:mm:ss}";
            ProgressValue = new Random().Next(0, 100);

            // TODO Phase 2: Subscribe to IUserInterface events for automatic updates
            // _ui?.UpdateStatus("Test via IUserInterface");
        }

        // INotifyPropertyChanged implementation
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}