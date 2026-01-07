using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using VecTool.Handlers;
using VecTool.Studio.Versioning;

namespace VecTool.Studio.Services;

public static class ServiceProviderFactory
{
    public static IServiceProvider CreateServiceProvider()
    {
        // ✅ NEW: Early console logging before DI
        Console.WriteLine("[ServiceProviderFactory] Starting DI configuration...");

        var services = new ServiceCollection();

        // 1. Configure NLog
        ConfigureLogging(services);
        Console.WriteLine("[ServiceProviderFactory] NLog configured"); // ✅ NEW

        // 2. Register core services
        RegisterCoreServices(services);
        Console.WriteLine("[ServiceProviderFactory] Core services registered"); // ✅ NEW

        // 3. Register Avalonia-specific UI service
        services.AddSingleton<IUserInterface, AvaloniaUserInterface>();
        Console.WriteLine("[ServiceProviderFactory] AvaloniaUserInterface registered"); // ✅ NEW

        var provider = services.BuildServiceProvider();
        Console.WriteLine("[ServiceProviderFactory] ServiceProvider built successfully"); // ✅ NEW

        // ✅ NEW: Test logging immediately after provider is built
        var logger = provider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("ServiceProvider initialized with {ServiceCount} registrations",
            services.Count);

        return provider;
    }

    private static void ConfigureLogging(IServiceCollection services)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "nlog.config");
        Console.WriteLine($"[ServiceProviderFactory] Looking for NLog config at: {configPath}"); // ✅ NEW

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

        Console.WriteLine("[ServiceProviderFactory] NLog provider added to IServiceCollection"); // ✅ NEW
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        // ✅ Core services (proven from WinForms)
        services.AddSingleton<IVersionProvider, AssemblyVersionProvider>();
        Console.WriteLine("[ServiceProviderFactory] IVersionProvider registered"); // ✅ NEW

        // TODO: Add handler registrations when Phase 02-04 migration happens
    }
}