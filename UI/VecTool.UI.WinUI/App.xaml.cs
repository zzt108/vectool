// Path: src/UI/VecTool.UI.WinUI/App.xaml.cs
// Required Imports Template

using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging
using Microsoft.UI.Xaml;
using VecTool.Core.Infrastructure;
using Microsoft.UI.Dispatching;

namespace VecTool.UI.WinUI
{
    public partial class App : Application
    {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        // ✅ NEW: Public property for MainWindow instance (Phase 3.1 Fix)
        public MainWindow? MainWindow { get; private set; }

        public App()
        {
            // Initialize NLog once (never throw if config is missing, per guide)
            TryInitializeLogging();

            // Initialize generated XAML components
            this.InitializeComponent();

            // Capture UI-thread exceptions and log them with structured properties
            this.UnhandledException += AppUnhandledException;
        }

        private static void TryInitializeLogging()
        {
            try
            {
                // Safe bootstrap (Seq + console with buffering, non-throwing)
                // Reads colocated NLog.config if present
                LogManager.Setup().LoadConfigurationFromFile( optional:true );
                Log.Info("WinUI App bootstrap complete");
            }
            catch
            {
                // Do not throw – logging must not crash the process
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            NLogBootstrap.Init(); // structured logging bootstrap, idempotent​
            MainWindow = new MainWindow(); // assign property so it’s available elsewhere​
            MainWindow.Activate(); // show window​

            // Use the window’s DispatcherQueue to avoid null on startup
            MainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                var log = LogManager.GetCurrentClassLogger();
                log.Info("WinUI launched with {Args}", args.Arguments); // NLog structured logging[1]
            });
        }

        private void AppUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Log exception with template and properties (do not crash the process unless policy dictates)
            Log.Error(e.Exception, "Unhandled UI exception at {TimestampUtc}", DateTime.UtcNow);

            // Preserve responsiveness (parity policy can adjust this if legacy behavior differs)
            e.Handled = true;
        }
    }
}
