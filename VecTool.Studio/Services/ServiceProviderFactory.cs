using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using VecTool.Handlers;
using VecTool.Studio.Versioning;

namespace VecTool.Studio.Services;

/// <summary>
/// Avalonia variant of ServiceProviderFactory (DI bootstrap).
/// Follows the proven pattern from VecTool.UI/OaiUI/ServiceProviderFactory.cs
/// </summary>
public static class ServiceProviderFactory
{
    /// <summary>
    /// Creates and configures the DI container for Avalonia application.
    /// </summary>
    public static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // 1. Configure NLog
        ConfigureLogging(services);

        // 2. Register core services (copy from WinForms variant)
        RegisterCoreServices(services);

        // 3. Register Avalonia-specific UI service
        services.AddSingleton<IUserInterface, AvaloniaUserInterface>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Configures NLog logging provider.
    /// Pattern copied from WinForms variant.
    /// </summary>
    private static void ConfigureLogging(IServiceCollection services)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "nlog.config");

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"NLog config not found: {configPath}");
        }

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddNLog(configPath);
        });
    }

    /// <summary>
    /// Registers core application services (shared between WinForms and Avalonia).
    /// Copied from VecTool.UI/OaiUI/ServiceProviderFactory.cs
    /// </summary>
    private static void RegisterCoreServices(IServiceCollection services)
    {
        // ✅ Core services (proven from WinForms)
        services.AddSingleton<IVersionProvider, AssemblyVersionProvider>();

        // ❌ No MainForm registration (Avalonia creates MainWindow differently in App.axaml.cs)
        // ❌ No WinFormsUserInterface registration (replaced by AvaloniaUserInterface above)

        // TODO: Add handler registrations when Phase 02-04 migration happens
        // Example placeholders (commented until handlers are migrated):
        // services.AddSingleton<IExportHandler, XmlMarkdownExportHandler>();
        // services.AddSingleton<IFileSystemTraverser, FileSystemTraverser>();
    }
}