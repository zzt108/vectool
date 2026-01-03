using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Vectool.UI.Versioning;

namespace Vectool.OaiUI;

public static class ServiceProviderFactory
{
    public static IServiceProvider Create()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddNLog("NLog.config");
        });

        // Core app services used by MainForm today
        services.AddSingleton<IVersionProvider, AssemblyVersionProvider>();

        // UI
        services.AddTransient<MainForm>();

        // Handlers/services: register later when migrating them (Phase 02-04).
    }
}