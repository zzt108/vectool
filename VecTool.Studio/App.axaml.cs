using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging; // ✅ ADD
using VecTool.Handlers;
using VecTool.Studio.Services;

namespace VecTool.Studio
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; } = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // 🔄 MODIFY: Add console test BEFORE DI
            Console.WriteLine("=== VecTool.Studio STARTING ===");
            System.Diagnostics.Trace.WriteLine("=== VecTool.Studio STARTING (Trace) ===");

            // 1. Create DI container
            ServiceProvider = ServiceProviderFactory.CreateServiceProvider();

            // ✅ NEW: Test ILogger after DI bootstrapping
            var logger = ServiceProvider.GetRequiredService<ILogger<App>>();
            logger.LogInformation("VecTool.Studio initialized successfully");
            logger.LogInformation("DI container created with {ServiceCount} services",
                ServiceProvider.GetType().GetProperty("Count")?.GetValue(ServiceProvider) ?? "unknown");

            // 2. Register global exception handler
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                System.Diagnostics.Trace.WriteLine($"UNHANDLED: {ex}");
                logger.LogCritical(ex, "Unhandled exception occurred"); // ✅ NEW
            };

            // 3. Set dark theme
            RequestedThemeVariant = ThemeVariant.Dark;
            logger.LogDebug("Dark theme applied"); // ✅ NEW

            // 4. Create main window with DI
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var ui = ServiceProvider.GetRequiredService<IUserInterface>();
                logger.LogDebug("IUserInterface resolved: {InterfaceType}", ui.GetType().Name); // ✅ NEW

                desktop.MainWindow = new MainWindow(ui);
                logger.LogInformation("MainWindow created and assigned"); // ✅ NEW
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}