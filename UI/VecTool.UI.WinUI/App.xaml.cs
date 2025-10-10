// FULL FILE VERSION
// Path: src/UI/VecTool.UI.WinUI/App.xaml.cs

// Required Imports Template
using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging

using Microsoft.UI.Xaml;

namespace VecTool.UI.WinUI
{
    public partial class App : Application
    {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public App()
        {
            // Initialize generated XAML components
            this.InitializeComponent();

            // Capture UI-thread exceptions and log them with structured properties
            this.UnhandledException += App_UnhandledException;
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Initialize NLog once, safely, with Seq + buffering as per org standards
            VecTool.UI.WinUI.Infrastructure.NLogBootstrap.Init();

            // Message-template logging (no string interpolation) for Seq queryability
            Log.Info("WinUI application starting {Application} with {ProcessId}", "VecTool.UI.WinUI", Environment.ProcessId);

            // Create and activate the main window — zero behavior change beyond tech swap
            var window = new MainWindow();
            window.Activate();

            Log.Info("MainWindow activated at {TimestampUtc}", DateTime.UtcNow);
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
