// ✅ FULL FILE VERSION
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

namespace UnitTests
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

            var handler = new TestRunnerHandler(proc, ui, recent);

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            //var sln = Path.Combine(tempDir, "VecTool.sln");
            //await File.WriteAllTextAsync(sln, "Microsoft Visual Studio Solution File, Format Version 12.00");

            try
            {
                var result = await handler.RunTestsAsync("storeX", CancellationToken.None);
                result.ShouldBeNull();
            }
            finally
            {
                TryDeleteDir(tempDir);
            }
        }

        [Test]
        public async Task RunTestsAsync_should_write_output_on_success()
        {
            // IGitRunner git = new FakeGitRunner("main");
            IProcessRunner proc = new FakeProcessRunner(exitCode: 0, stdout: "ok", stderr: "");
            IUserInterface ui = new FakeUserInterface();
            IRecentFilesManager recent = new NoopRecentFilesManager();

            var handler = new TestRunnerHandler(proc, ui, recent);

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            //var sln = Path.Combine(tempDir, "VecTool.sln");
            //await File.WriteAllTextAsync(sln, "Microsoft Visual Studio Solution File, Format Version 12.00");

            try
            {
                var resultPath = await handler.RunTestsAsync("S", CancellationToken.None);
                resultPath.ShouldNotBeNull();
                File.Exists(resultPath!).ShouldBeTrue();
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
