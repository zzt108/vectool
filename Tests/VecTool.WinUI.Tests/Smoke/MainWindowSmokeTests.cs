// ✅ FULL FILE VERSION
using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
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
            // Safe, non-throwing bootstrap in CI - this picks up NLog.config colocated with testhost
            try
            {
                LogManager.Setup().LoadConfigurationFromFile(optional: true);
            }
            catch
            {
                // Never throw - logging must not block tests
            }
        }

        [Test]
        public void ShouldCreateWindowAndMenuBar()
        {
            // Note: in real UI tests, host WinUI via UITestAdapter - here we assert construction and named members
            var win = new VecTool.UI.WinUI.MainWindow();
            win.ShouldNotBeNull("MainWindow should construct without exceptions.");

            // ✅ NEW - WinUI 3 direct access to x:Name elements
            // In WinUI 3, XAML elements with x:Name are strongly-typed fields - no FindName needed

            // For unit test access from external assembly, we need helper method or public properties
            // Since we can't access private x:Name fields directly, use VisualTreeHelper
            var actionsMenu = FindElementByName<MenuBarItem>(win, "ActionsMenu");
            actionsMenu.ShouldNotBeNull("ActionsMenu must exist to preserve parity with WinForms Actions menu.");

            var fileMenu = FindElementByName<MenuBarItem>(win, "FileMenu");
            fileMenu.ShouldNotBeNull("FileMenu must exist to preserve parity with WinForms File menu.");

            var helpMenu = FindElementByName<MenuBarItem>(win, "HelpMenu");
            helpMenu.ShouldNotBeNull("HelpMenu must exist to preserve parity with WinForms Help menu.");

            Log.Info("MainWindow created and core menus located.");
        }

        [Test]
        public void ShouldHaveStatusControls()
        {
            var win = new VecTool.UI.WinUI.MainWindow();
            win.ShouldNotBeNull();

            var statusText = FindElementByName<TextBlock>(win, "StatusText");
            statusText.ShouldNotBeNull("StatusText must exist for parity with StatusStrip label.");
            statusText.Text.ShouldBe("Ready", "Default status text should be 'Ready'.");

            var progress = FindElementByName<ProgressBar>(win, "StatusProgress");
            progress.ShouldNotBeNull("StatusProgress must exist for parity with StatusStrip progress.");

            Log.Info("Status controls present with default values.");
        }

        [Test]
        public async Task AboutDialogShouldOpen()
        {
            var win = new VecTool.UI.WinUI.MainWindow();
            win.ShouldNotBeNull();

            // Simulate About click
            var method = typeof(VecTool.UI.WinUI.MainWindow).GetMethod(
                "AboutMenuClick",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            method.ShouldNotBeNull("AboutMenuClick handler should exist.");
            method!.Invoke(win, new object?[] { null, new RoutedEventArgs() });

            // In a real harness we would await dialog - here we just ensure the handler is invocable
            await Task.Delay(10);

            Log.Info("About menu handler invoked successfully.");
        }

        private static T? FindElementByName<T>(Window window, string name) where T : FrameworkElement
        {
            return FindElementByName<T>(window.Content as DependencyObject, name);
        }

        /// <summary>
        /// Recursively searches visual tree for element with specified Name property.
        /// Required for unit tests since x:Name fields are not accessible from external assemblies.
        /// </summary>
        private static T? FindElementByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent == null)
            {
                return null;
            }

            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T element && element.Name == name)
                {
                    return element;
                }

                var result = FindElementByName<T>(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
