// File: Vectool.UI.WinUI.Tests/MainWindowSmokeTests.cs

// Required Imports Template
using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;

namespace VecTool.WinUI.Tests.Smoke
{
    [TestFixture]
    public sealed class MainWindowSmokeTests
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // Safe, non-throwing bootstrap; in CI this picks up NLog.config colocated with testhost
            try
            {
                LogManager.Setup().LoadConfigurationFromFile(optional: true);
            }
            catch
            {
                // Never throw: logging must not block tests
            }
        }

        [Test]
        public void Should_create_window_and_menu_bar()
        {
            // Note: in real UI tests, host WinUI via UITestAdapter; here we assert construction and named members
            var win = new VecTool.UI.WinUI.MainWindow();
            win.ShouldNotBeNull("MainWindow should construct without exceptions.");

            // Using reflection to find named controls since we don't have a visual tree host in this smoke test
            var menu = win.FindName("ActionsMenu") as MenuBarItem;
            menu.ShouldNotBeNull("ActionsMenu must exist to preserve parity with WinForms Actions menu.");

            var file = win.FindName("FileMenu") as MenuBarItem;
            file.ShouldNotBeNull("FileMenu must exist to preserve parity with WinForms File menu.");

            var help = win.FindName("HelpMenu") as MenuBarItem;
            help.ShouldNotBeNull("HelpMenu must exist to preserve parity with WinForms Help menu.");

            Log.Info("MainWindow created and core menus located.");
        }

        [Test]
        public void Should_have_status_controls()
        {
            var win = new VecTool.UI.WinUI.MainWindow();
            win.ShouldNotBeNull();

            var statusText = win.FindName("StatusText") as TextBlock;
            statusText.ShouldNotBeNull("StatusText must exist for parity with StatusStrip label.");
            statusText.Text.ShouldBe("Ready", "Default status text should be 'Ready'.");

            var progress = win.FindName("StatusProgress") as ProgressBar;
            progress.ShouldNotBeNull("StatusProgress must exist for parity with StatusStrip progress.");
            Log.Info("Status controls present with default values.");
        }

        [Test]
        public async Task About_dialog_should_open()
        {
            var win = new VecTool.UI.WinUI.MainWindow();
            win.ShouldNotBeNull();

            // Simulate About click
            var method = typeof(VecTool.UI.WinUI.MainWindow).GetMethod("AboutMenu_Click", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method.ShouldNotBeNull("AboutMenu_Click handler should exist.");
            method!.Invoke(win, new object?[] { null, new RoutedEventArgs() });

            // In a real harness we would await dialog; here we just ensure the handler is invocable
            await Task.Delay(10);
            Log.Info("About menu handler invoked successfully.");
        }
    }
}
