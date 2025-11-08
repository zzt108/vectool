using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnitTests.Fakes;
using VecTool.Core.Abstractions;
using VecTool.Handlers;
using VecTool.RecentFiles;

namespace UnitTests.Handlers
{
    [TestFixture]
    public class TestRunnerHandlerDependencyTests
    {
        [Test]
        public async Task RunTestsAsync_should_return_null_when_dotnet_fails()
        {
            // IGitRunner git = new FakeGitRunner("dev");
            IProcessRunner proc = new FakeProcessRunner(exitCode: 1, stdout: "", stderr: "boom");
            IUserInterface ui = new FakeUserInterface();
            IRecentFilesManager recent = new NoopRecentFilesManager();

            var handler = new TestRunnerHandler("Fake.sln", null,proc, ui, recent, "test-branch", "storeX");

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var result = await handler.RunTestsAsync( CancellationToken.None);
                result.ShouldBeNull();
            }
            finally
            {
                TryDeleteDir(tempDir);
            }
        }

        [Test]
        //[Ignore("TODO")]
        public async Task RunTestsAsync_should_not_write_output_on_success()
        {
            // IGitRunner git = new FakeGitRunner("main");
            IProcessRunner proc = new FakeProcessRunner(exitCode: 0, stdout: "ok", stderr: "");
            IUserInterface ui = new FakeUserInterface();
            IRecentFilesManager recent = new NoopRecentFilesManager();

            var handler = new TestRunnerHandler("Fake.sln",null, proc, ui, recent, "main", "S");

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var resultPath = await handler.RunTestsAsync(CancellationToken.None);
                resultPath.ShouldBeNull();
                File.Exists(resultPath!).ShouldBeFalse();
            }
            finally
            {
                TryDeleteDir(tempDir);
            }
        }

        [Test]
        public void ProcessResult_property_model_should_cover_legacy_ctor_use()
        {
            var r = new ProcessResult
            {
                ExitCode = 7,
                StandardOutput = "stdout",
                StandardError = "stderr",
                Duration = TimeSpan.Zero
            };

            r.ExitCode.ShouldBe(7);
            r.StandardOutput.ShouldBe("stdout");
            r.StandardError.ShouldBe("stderr");
        }

        private static void TryDeleteDir(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // Best-effort cleanup
            }
        }
    }
}