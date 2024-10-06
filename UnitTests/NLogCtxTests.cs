using FluentAssertions;
using NUnit.Framework;
using NLogShared;
using LogCtxShared;

namespace NLogAdapter.Tests
{
    [TestFixture]
    public class NLogCtxTests
    {
        private const string ConfigPath = "Config/nlog.config"; // Adjust path as necessary

        [SetUp]
        public void Setup()
        {
            // Setup any required environment configuration for tests
            // This can include initializing NLog configuration if needed
        }

        [Test]
        public void Init_ShouldInitializeLogger_WhenCanLogIsTrue()
        {
            // Arrange
            var nLogCtx = new CtxLogger();

            // Act
            var result = nLogCtx.ConfigureXml(ConfigPath);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void Clear_ShouldClearScopeContext()
        {
            // Arrange
            var nLogScopeContext = new NLogScopeContext();
            nLogScopeContext.PushProperty("TestKey", "TestValue");

            // Act
            nLogScopeContext.Clear();

            // Assert
            // Here you would verify that the context is cleared, if possible
        }

        [Test]
        public void PushProperty_ShouldAddPropertyToScopeContext()
        {
            // Arrange
            var nLogScopeContext = new NLogScopeContext();
            var key = "TestKey";
            var value = "TestValue";

            // Act
            nLogScopeContext.PushProperty(key, value);

            // Assert
            // Here you would verify that the property was added correctly
        }
        [Test]
        public void CanDoStructuredLog_ShouldLogMessagesCorrectly()
        {
            // Arrange
            // Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine(msg));
            using (var log1 = new CtxLogger())
            {
                log1.ConfigureXml(ConfigPath);
            };

            using var log = new CtxLogger();

            var props = new Props("first", log);

            // Act
            log.Ctx.Set(props);
            log.Trace("Trace message");
            log.Debug("Debug message");
            log.Info("Info message");
            log.Warn("Warn message");
            log.Error(new ArgumentException("Test Argument Exception", "Param name"), "Error message");
            log.Fatal(new ArgumentException("Test Fatal Argument Exception", "Param name"), "Fatal message");

            // Assert
            // Here you could verify the log output if you had a way to capture it
        }
    }
}