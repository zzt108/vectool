// Path: UnitTests/TestRunnerHandlerTests.cs

using NUnit.Framework;
using Shouldly;
using VecTool.Handlers;

namespace UnitTests.Handlers
{
    [TestFixture]
    public class TestRunnerHandlerTests
    {
        [TestCase(0, "All tests passed.")]
        [TestCase(1, "One or more tests failed (VSTest).")]
        [TestCase(2, "One or more tests failed (MSTest platform).")]
        [TestCase(3, "Test run aborted.")]
        [TestCase(8, "No tests discovered. Check your test project.")]
        [TestCase(10, "Test adapter/infrastructure failure.")]
        //[Ignore("TODO")]
        public void MapExitCodeToMessage_KnownCodes(int code, string expectedStart)
        {
            var msg = TestRunnerHandler.MapExitCodeToMessage(code);
            msg.ShouldStartWith(expectedStart);
        }

        [Test]
        //[Ignore("TODO")]
        public void MapExitCodeToMessage_UnknownCode_ProducesSafeMessage()
        {
            var msg = TestRunnerHandler.MapExitCodeToMessage(99);
            msg.ShouldContain("Unknown exit code 99");
            msg.ShouldEndWith("See output for details.");
        }
    }
}
