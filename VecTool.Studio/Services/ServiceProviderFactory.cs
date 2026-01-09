using LogCtxShared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using VecTool.Configuration.Logging;
using VecTool.Handlers;
using VecTool.Handlers.Traversal;
using VecTool.RecentFiles;
using VecTool.Studio.Versioning;

namespace VecTool.Studio.Services;

public static class ServiceProviderFactory
{
    //  Static logger via AppLogger
    private static readonly ILogger logger = AppLogger.Create("VecTool.Studio.Services.ServiceProviderFactory");

    public static IServiceProvider CreateServiceProvider()
    {
        Console.WriteLine("[ServiceProviderFactory] Starting DI configuration...");

        // Log with LogCtx
        using (Props p = logger.SetContext()
            .Add("Operation", "CreateServiceProvider")
            .Add("Framework", "Avalonia"))
        {
            logger.LogInformation("DI bootstrapping started");
        }

        var services = new ServiceCollection();

        // 1. Configure NLog
        ConfigureLogging(services);
        Console.WriteLine("[ServiceProviderFactory] NLog configured");

        // 2. Register core services
        RegisterCoreServices(services);
        Console.WriteLine("[ServiceProviderFactory] Core services registered");

        // 3. Register Avalonia-specific UI service
        services.AddSingleton<IUserInterface, AvaloniaUserInterface>();
        Console.WriteLine("[ServiceProviderFactory] AvaloniaUserInterface registered");

        var provider = services.BuildServiceProvider();
        Console.WriteLine("[ServiceProviderFactory] ServiceProvider built successfully");

        // ✅ MODIFIED: Use LogCtx for structured logging
        using (Props p = logger.SetContext()
            .Add("ServiceCount", services.Count))
        {
            logger.LogInformation("ServiceProvider initialized successfully");
        }

        return provider;
    }

    private static void ConfigureLogging(IServiceCollection services)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "nlog.config");
        Console.WriteLine($"[ServiceProviderFactory] Looking for NLog config at: {configPath}");

        // Log with LogCtx
        using (Props p = logger.SetContext()
            .Add("ConfigPath", configPath)
            .Add("FileExists", File.Exists(configPath)))
        {
            logger.LogDebug("NLog config path resolved");
        }

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

        Console.WriteLine("[ServiceProviderFactory] NLog provider added to IServiceCollection");
        logger.LogInformation("NLog logging configured successfully");
    }

    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddSingleton<IVersionProvider, AssemblyVersionProvider>();

        // ✅ NEW: Traversal
        services.AddSingleton<IFileMarkerExtractor, FileMarkerExtractor>();
        services.AddSingleton<IFileSystemTraverser>(sp =>
            new FileSystemTraverser(
                ui: sp.GetRequiredService<IUserInterface>(),
                markerExtractor: sp.GetRequiredService<IFileMarkerExtractor>()));

        // ✅ NEW: No-op recent files manager (Phase 4 MVP)
        services.AddSingleton<IRecentFilesManager, NoopRecentFilesManager>();

        // ✅ NEW: Handler itself
        services.AddTransient<MDHandler>();

        using (Props p = logger.SetContext()
            .Add("ServiceType", "MDHandler")
            .Add("Dependencies", "IFileSystemTraverser|IRecentFilesManager(noop)"))
        {
            logger.LogDebug("Handler services registered for Phase 2 Step 4");
        }
    }
}