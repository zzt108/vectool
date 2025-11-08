using Shouldly;
using NLogShared;
using NUnit.Framework;

namespace NLogAdapter.Tests
{
    [TestFixture]
    public class NLogCtxTests
    {
        private const string ConfigPath = "Config/nlog.config"; // Adjust path as necessary

        [Test]
        public void Init_ShouldInitializeLogger_WhenCanLogIsTrue()
        {
            // Arrange
            var nLogCtx = new CtxLogger();

            // Act
            var result = nLogCtx.ConfigureXml(ConfigPath);

            // Assert
            result.ShouldBeTrue();
        }
    }
}