using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using VecTool.Handlers;
using VecTool.Studio.Services;

namespace VecTool.Studio;

public partial class App : Application
{
    /// <summary>
    /// DI container for the application.
    /// </summary>
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // 1. Create DI container
        ServiceProvider = ServiceProviderFactory.CreateServiceProvider();

        // 2. Register global exception handler
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            System.Diagnostics.Trace.WriteLine($"UNHANDLED: {ex}");
            // Allow crash for now - enhance later with logging
        };

        // 3. Set dark theme
        RequestedThemeVariant = ThemeVariant.Dark;

        // Inject IUserInterface into MainWindow
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var ui = ServiceProvider.GetRequiredService<IUserInterface>();
            desktop.MainWindow = new MainWindow(ui);
        }

        base.OnFrameworkInitializationCompleted();
    }
}