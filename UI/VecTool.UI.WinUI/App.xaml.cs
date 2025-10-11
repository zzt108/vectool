// FULL FILE VERSION
// Path: src/UI/VecTool.UI.WinUI/App.xaml.cs

// Required Imports Template
using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging

using Microsoft.UI.Xaml;
using Vectool.UI.WinUI;

namespace VecTool.UI.WinUI
{
    public partial class App : Application
    {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public App()
        {
            // Initialize NLog once; never throw if config is missing, per guide
            TryInitializeLogging();

            // Initialize generated XAML components
            this.InitializeComponent();

            // Capture UI-thread exceptions and log them with structured properties
            this.UnhandledException += App_UnhandledException;
        }
        private static void TryInitializeLogging()
        {
            try
            {
                // Safe bootstrap: Seq + console with buffering, non-throwing
                // Reads colocated NLog.config if present
                LogManager.Setup().LoadConfigurationFromFile(optional: true);
                Log.Info("WinUI App bootstrap complete");
            }
            catch
            {
                // Do not throw; logging must not crash the process
            }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            VecTool.UI.WinUI.Infrastructure.NLogBootstrap.Init(); // idempotent, safe
            var log = NLog.LogManager.GetCurrentClassLogger();
            log.Info("WinUI application starting {Application} with {ProcessId}", "VecTool.UI.WinUI", Environment.ProcessId);
            var window = new MainWindow();
            window.Activate();
            log.Info("MainWindow activated at {TimestampUtc}", DateTime.UtcNow);
        }
         
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Log exception with template and properties — do not crash the process unless policy dictates
            Log.Error(e.Exception, "Unhandled UI exception at {TimestampUtc}", DateTime.UtcNow);

            // Preserve responsiveness; parity policy can adjust this if legacy behavior differs
            e.Handled = true;
        }
    }
}
