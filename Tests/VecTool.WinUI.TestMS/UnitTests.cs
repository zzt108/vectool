using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using System;
using NLog;
using VecTool.Core.Infrastructure; // NLog is mandatory for structurd logging

namespace VecTool.WinUI.TestMS
{
    [TestClass]
    public sealed class StartupSmoke
    {
        [TestMethod]
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
