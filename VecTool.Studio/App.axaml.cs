using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // ✅ KEEP for ILogger interface
using LogCtxShared; // ✅ NEW - LogCtx wrapper
using VecTool.Configuration.Logging; // ✅ NEW - AppLogger
using VecTool.Handlers;
using VecTool.Studio.Services;

namespace VecTool.Studio
{
    public partial class App : Application
    {
        // ✅ NEW: Static logger via AppLogger (not DI)
        private static readonly ILogger logger = AppLogger.For<App>();

        public IServiceProvider ServiceProvider { get; private set; } = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Console test BEFORE DI
            Console.WriteLine("=== VecTool.Studio STARTING ===");
            System.Diagnostics.Trace.WriteLine("=== VecTool.Studio STARTING (Trace) ===");

            // ✅ NEW: Log with LogCtx context
            using (Props p = logger.SetContext()
                .Add("Operation", "AppStartup")
                .Add("Framework", "Avalonia"))
            {
                logger.LogInformation("VecTool.Studio initialization started");
            }

            // 1. Create DI container
            ServiceProvider = ServiceProviderFactory.CreateServiceProvider();

            // ✅ MODIFIED: Use AppLogger + LogCtx
            using (Props p = logger.SetContext()
                .Add("ServiceCount", ServiceProvider.GetType().GetProperty("Count")?.GetValue(ServiceProvider) ?? "unknown"))
            {
                logger.LogInformation("DI container created successfully");
            }

            // 2. Register global exception handler
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                System.Diagnostics.Trace.WriteLine($"UNHANDLED: {ex}");

                // ✅ NEW: Log unhandled exceptions with LogCtx
                using (Props p = logger.SetContext()
                    .Add("ExceptionType", ex?.GetType().Name ?? "Unknown")
                    .Add("Message", ex?.Message ?? "No message"))
                {
                    logger.LogCritical(ex, "Unhandled exception occurred");
                }
            };

            // 3. Set dark theme
            RequestedThemeVariant = ThemeVariant.Dark;
            logger.LogDebug("Dark theme applied");

            // 4. Create main window with DI
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var ui = ServiceProvider.GetRequiredService<IUserInterface>();

                using (Props p = logger.SetContext()
                    .Add("InterfaceType", ui.GetType().Name))
                {
                    logger.LogDebug("IUserInterface resolved successfully");
                }

                desktop.MainWindow = new MainWindow(ui);
                logger.LogInformation("MainWindow created and assigned");
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}