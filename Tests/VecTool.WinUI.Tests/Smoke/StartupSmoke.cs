// Path: tests/VecTool.WinUI.Tests/Smoke/StartupSmoke.cs

// Required Imports Template
using NLog; // NLog is mandatory for structured logging
using Shouldly;
using System;
using VecTool.Core.Infrastructure;

namespace VecTool.WinUI.Tests.Smoke
{
    [TestFixture]
    public sealed class StartupSmoke
    {
        [Test]
        public void Should_Initialize_Logging_And_Create_Logger()
        {
            NLogBootstrap.Init();
            var logger = LogManager.GetCurrentClassLogger();
            logger.ShouldNotBeNull();

            // Ensure logger can write without throwing even if Seq is down
            Should.NotThrow(() => logger.Info("Smoke test log at {TimestampUtc}", DateTime.UtcNow));
        }
    }
}
