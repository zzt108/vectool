// UnitTests/Core/GitRunnerTests.cs
using Shouldly;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Core; // GitRunner

namespace UnitTests.Core
{
    [TestFixture]
    public class GitRunnerTests
    {
        private string _tempDir = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "VecTool_Git_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, true); } catch { /* ignore */ }
        }

        [Test]
        public async Task RunGitCommandAsync_Should_Throw_On_NonRepo_With_InvalidOperation()
        {
            // Arrange: folder is not a git repo
            var gitRunner = new GitRunner(_tempDir);

            // Act + Assert
            var ex = await Should.ThrowAsync<InvalidOperationException>(
                gitRunner.RunGitCommandAsync("status", timeoutSeconds: 2));

            ex.Message.ShouldContain("Git command failed", Case.Insensitive);
        }

        [Test]
        public async Task RunGitCommandAsync_Should_Timeout_For_Long_Running_Command()
        {
            // Arrange: init repo and create a guaranteed long-running alias
            RunInDir("git", "init");
            // Windows-friendly: use PowerShell sleep; on *nix fallback to /bin/sh sleep
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                RunInDir("git", "config alias.slow \"!powershell -NoProfile -NonInteractive -Command Start-Sleep 5\"");
            }
            else
            {
                RunInDir("git", "config alias.slow \"!sh -c 'sleep 5'\"");
            }

            var gitRunner = new GitRunner(_tempDir);

            // Act + Assert: 1s timeout against a 5s sleep => TimeoutException
            await Should.ThrowAsync<TimeoutException>(
                gitRunner.RunGitCommandAsync("slow", timeoutSeconds: 1));
        }

        [Test]
        public async Task RunGitCommandAsync_Should_Return_Output_On_Success()
        {
            // Arrange
            RunInDir("git", "init");
            var gitRunner = new GitRunner(_tempDir);

            // Act
            var output = await gitRunner.RunGitCommandAsync("status --porcelain", timeoutSeconds: 10);

            // Assert
            output.ShouldNotBeNull();
        }

        private void RunInDir(string fileName, string arguments)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = _tempDir,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            p.Start();
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                var err = p.StandardError.ReadToEnd();
                throw new InvalidOperationException($"Failed: {fileName} {arguments} -> {err}");
            }
        }
    }
}
