// ✅ FULL FILE VERSION
// File: VecTool.Studio/App.axaml.cs

using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using LogCtxShared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VecTool.Configuration.Logging;
using VecTool.Handlers;
using VecTool.Studio.Services;

namespace VecTool.Studio;

public partial class App : Application
{
    // ❌ REMOVED static AppLogger - was bypassing DI/BufferingWrapper
    // private static readonly ILogger logger = AppLogger.For<App>();

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // ✅ NEW - Console for bootstrap logging (before DI configured)
        Console.WriteLine("[App] VecTool.Studio STARTING");
        System.Diagnostics.Trace.WriteLine("[App] VecTool.Studio STARTING (Trace)");

        ServiceProvider = ServiceProviderFactory.CreateServiceProvider();

        // ✅ NEW - Get logger AFTER DI is configured (now goes through BufferingWrapper)
        var logger = ServiceProvider.GetRequiredService<ILogger<App>>();

        using (Props p = logger.SetContext()
            .Add("Operation", "AppStartup")
            .Add("Framework", "Avalonia"))
        {
            logger.LogInformation("VecTool.Studio initialization started");
        }

        // ✅ NEW - UnhandledException handler now uses DI logger
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;

            using (Props p = logger.SetContext()
                .Add("ExceptionType", ex?.GetType().Name ?? "Unknown")
                .Add("Message", ex?.Message ?? "No message"))
            {
                logger.LogCritical(ex, "Unhandled exception occurred");
            }
        };

        RequestedThemeVariant = ThemeVariant.Dark;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var ui = ServiceProvider.GetRequiredService<IUserInterface>();
            var mainWindowLogger = ServiceProvider.GetRequiredService<ILogger<MainWindow>>();

            using (Props p = logger.SetContext()
                .Add("InterfaceType", ui.GetType().Name))
            {
                logger.LogDebug("IUserInterface resolved successfully");
            }

            desktop.MainWindow = new MainWindow(ui, ServiceProvider, mainWindowLogger);

            logger.LogInformation("MainWindow created and assigned");

            // ✅ KEEP - Exit handler for proper disposal
            desktop.Exit += (_, __) =>
            {
                using var ctx = logger.SetContext()
                    .Add("Event", "desktop.Exit")
                    .Add("Action", "DisposeServiceProvider");

                logger.LogInformation("Desktop lifetime exit detected; disposing ServiceProvider");

                if (ServiceProvider is IDisposable d)
                {
                    d.Dispose();
                }
            };
        }

        // 🔄 FIX - Was called twice! Only call once at the end
        base.OnFrameworkInitializationCompleted();
    }
}