using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Threading.Tasks;
using Core;

namespace UnitTests.CoreTests
{
    [TestFixture]
    public class GitRunnerTests
    {
        [Test]
        public void Constructor_Should_Throw_For_Null_WorkingDirectory()
        {
            Should.Throw<System.ArgumentNullException>(() => new GitRunner(null!));
        }

        [Test]
        public void IsGitRepository_Should_Return_False_For_NonGit_Directory()
        {
            var tempDir = Directory.CreateTempSubdirectory();
            try
            {
                var result = GitRunner.IsGitRepository(tempDir.FullName);
                result.ShouldBeFalse();
            }
            finally
            {
                Directory.Delete(tempDir.FullName, true);
            }
        }

        [Test]
        public async Task RunGitCommandAsync_Should_Timeout_For_Long_Running_Command()
        {
            var tempDir = Directory.CreateTempSubdirectory();
            try
            {
                var gitRunner = new GitRunner(tempDir.FullName);

                // This should timeout quickly
                await Should.ThrowAsync<System.TimeoutException>(
                    () => gitRunner.RunGitCommandAsync("status", 1)
                );
            }
            finally
            {
                Directory.Delete(tempDir.FullName, true);
            }
        }

        [Test]
        public async Task RunGitCommandAsync_Should_Handle_Invalid_Git_Command()
        {
            var tempDir = Directory.CreateTempSubdirectory();
            try
            {
                var gitRunner = new GitRunner(tempDir.FullName);

                // Invalid git command should throw InvalidOperationException
                await Should.ThrowAsync<System.InvalidOperationException>(
                    () => gitRunner.RunGitCommandAsync("invalid-command")
                );
            }
            finally
            {
                Directory.Delete(tempDir.FullName, true);
            }
        }
    }
}
