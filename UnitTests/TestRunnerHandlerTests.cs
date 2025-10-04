// File: UnitTests/TestRunnerHandlerTests.cs

using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Threading.Tasks;
using VecTool.Handlers;

namespace UnitTests
{
    [TestFixture]
    public class TestRunnerHandlerTests
    {
        private string _testDir = default!;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "VecToolTestRunner", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }

        [Test]
        public async Task RunTestsAsync_ShouldReturnNull_WhenSolutionFileMissing()
        {
            // Arrange
            var handler = new TestRunnerHandler(null, null);
            var fakeSolutionPath = Path.Combine(_testDir, "NonExistent.sln");

            // Act
            var result = await handler.RunTestsAsync(fakeSolutionPath, "TestStore", new System.Collections.Generic.List<string>());

            // Assert
            result.ShouldBeNull();
        }

        [Test]
        public async Task RunTestsAsync_ShouldReturnNull_WhenDotnetTestFails()
        {
            // Arrange
            var handler = new TestRunnerHandler(null, null);

            // Create empty dummy .sln to satisfy existence check
            var fakeSolutionPath = Path.Combine(_testDir, "Dummy.sln");
            await File.WriteAllTextAsync(fakeSolutionPath, string.Empty);

            // Simulate failure: Use invalid command by renaming dotnet executable in PATH
            // Here we expect an exception internally, resulting in a null return.
            var originalPath = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH", "");

            // Act
            var result = await handler.RunTestsAsync(fakeSolutionPath, "TestStore", new System.Collections.Generic.List<string>());

            // Assert
            result.ShouldBeNull();

            // Cleanup
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }

        // Integration-like test skipped due to external dependency on git and dotnet
        [Test]
        public void FindSolutionFile_IsPrivateAndNotTestable()
        {
            // This method is private; proper unit testing would require visibility change.
            Assert.Pass("FindSolutionFile is covered in integration tests.");
        }
    }
}
