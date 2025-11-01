using NUnit.Framework;
using Shouldly;
using UnitTests.Fakes;
using VecTool.Core.Abstractions;
using VecTool.Handlers;
using VecTool.RecentFiles;

namespace UnitTests.Handlers
{
    [TestFixture]
    public class TestRunnerHandler_DITests
    {
        [Test]
        public void Should_wire_dependencies_via_canonical_DI_constructor()
        {
            IProcessRunner processRunner = new FakeProcessRunner();
            IUserInterface ui = new FakeUserInterface();
            IRecentFilesManager recentFiles = new NoopRecentFilesManager();

            var handler = new TestRunnerHandler("Fake.sln", null, processRunner, ui, recentFiles);

            handler.ShouldNotBeNull();
        }
    }
}