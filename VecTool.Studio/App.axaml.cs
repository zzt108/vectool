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
    private static readonly ILogger logger = AppLogger.For<App>();
    // TODO: Move to App;

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Console.WriteLine("VecTool.Studio STARTING");
        System.Diagnostics.Trace.WriteLine("VecTool.Studio STARTING (Trace)");

        using (Props p = logger.SetContext()
            .Add("Operation", "AppStartup")
            .Add("Framework", "Avalonia"))
        {
            logger.LogInformation("VecTool.Studio initialization started");
        }

        ServiceProvider = ServiceProviderFactory.CreateServiceProvider();

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
            var serviceProvider = ServiceProvider;

            var ui = serviceProvider.GetRequiredService<IUserInterface>();
            var mainWindowLogger = serviceProvider.GetRequiredService<ILogger<MainWindow>>();

            using (Props p = logger.SetContext()
                .Add("InterfaceType", ui.GetType().Name))
            {
                logger.LogDebug("IUserInterface resolved successfully");
            }

            desktop.MainWindow = new MainWindow(ui, serviceProvider, mainWindowLogger);

            logger.LogInformation("MainWindow created and assigned");
        }

        base.OnFrameworkInitializationCompleted();
    }
}