using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging
using VecTool.UI.WinUI.Infrastructure;

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

    /// <summary>
    /// Had to create WinUI test, to be able to execute NUnit tests in the other project
    /// I guess x64 test runner was installed upon creation.
    /// </summary>
    [TestClass]
    public partial class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Assert.AreEqual(0, 0);
        }

        // Use the UITestMethod attribute for tests that need to run on the UI thread.
        [UITestMethod]
        public void TestMethod2()
        {
            var grid = new Grid();
            Assert.AreEqual(0, grid.MinWidth);
        }
    }
}
