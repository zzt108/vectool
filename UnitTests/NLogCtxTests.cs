using LogCtxShared;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NUnit.Framework;
using Shouldly;
using System;

namespace UnitTests;

[TestFixture]
public sealed class NLogCtxTests
{
    [Test]
    public void InitShouldCreateLoggerAndLogWithConfig()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "nlog.config");
        File.Exists(configPath).ShouldBeTrue($"Missing test NLog config: {configPath}");

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(LogLevel.Trace);

            // Uses the UnitTests output-copied nlog.config
            builder.AddNLog(configPath);
        });

        var logger = loggerFactory.CreateLogger<NLogCtxTests>();
        logger.ShouldNotBeNull();

        using var _ = logger.SetContext(new Props()
            .Add("test", nameof(InitShouldCreateLoggerAndLogWithConfig)));

        logger.LogInformation("Logger initialization test message.");
    }
}