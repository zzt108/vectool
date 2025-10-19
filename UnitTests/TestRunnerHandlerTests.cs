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
    public class TestRunnerHandlerTests
    {
        [Test]
        public async Task RunTestsAsync_returns_null_when_solution_missing()
        {
            IGitRunner git = new FakeGitRunner("dev");
            IProcessRunner proc = new FakeProcessRunner(exitCode: 0);
            var handler = new TestRunnerHandler(git, proc, ui: null, recentFilesManager: null);

            var result = await handler.RunTestsAsync("Store", Array.Empty<string>(), CancellationToken.None);
            result.ShouldBeNull();
        }

        [Test]
        public async Task RunTestsAsync_writes_file_when_exitcode_zero()
        {
            IGitRunner git = new FakeGitRunner("dev");
            IProcessRunner proc = new FakeProcessRunner(exitCode: 0, stdout: "ok");
            IRecentFilesManager recent = new NoopRecentFilesManager();
            var handler = new TestRunnerHandler(git, proc, ui: null, recentFilesManager: recent);

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var sln = Path.Combine(tempDir, "VecTool.sln");
            await File.WriteAllTextAsync(sln, "Microsoft Visual Studio Solution File, Format Version 12.00");

            try
            {
                var result = await handler.RunTestsAsync("S", Array.Empty<string>(), CancellationToken.None);
                result.ShouldNotBeNull();
                File.Exists(result!).ShouldBeTrue();
            }
            finally
            {
                TryDeleteDir(tempDir);
            }
        }

        [Test]
        public async Task RunTestsAsync_returns_null_when_exitcode_nonzero()
        {
            IGitRunner git = new FakeGitRunner("dev");
            IProcessRunner proc = new FakeProcessRunner(exitCode: 2, stdout: "", stderr: "fail");
            var handler = new TestRunnerHandler(git, proc, ui: null, recentFilesManager: null);

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var sln = Path.Combine(tempDir, "VecTool.sln");
            await File.WriteAllTextAsync(sln, "Microsoft Visual Studio Solution File, Format Version 12.00");

            try
            {
                var result = await handler.RunTestsAsync("S", Array.Empty<string>(), CancellationToken.None);
                result.ShouldBeNull();
            }
            finally
            {
                TryDeleteDir(tempDir);
            }
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
                // Swallow in tests
            }
        }
    }
}
