using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Windows.Forms;

namespace Vectool.OaiUI;

internal static class Program
{
    private static IServiceProvider? serviceProvider;

    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        serviceProvider = ServiceProviderFactory.Create();

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Program");

        logger.LogInformation("Starting VecTool application.");

        Application.ThreadException += (_, args) =>
        {
            logger.LogError(args.Exception, "Unhandled UI thread exception.");
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            logger.LogCritical(ex, "Unhandled domain exception.");
        };

        var mainForm = serviceProvider.GetRequiredService<MainForm>();

        Application.Run(mainForm);

        if (serviceProvider is IDisposable d)
        {
            d.Dispose();
        }
    }
}