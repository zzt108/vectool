#nullable enable
using NUnit.Framework;
using Shouldly;
using System.IO;
using VecTool.Core.Helpers;

namespace UnitTests.Core.Helpers
{
    [TestFixture]
    public sealed class GitHelperTests
    {
        private string tempDir = null!;

        [SetUp]
        public void Setup()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "GitHelperTests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
                // Ignore cleanup failures
            }
        }

        [Test]
        public void IsGitRepository_ValidRepo_ReturnsTrue()
        {
            // Arrange
            var gitDir = Path.Combine(tempDir, ".git");
            Directory.CreateDirectory(gitDir);

            // Act
            var result = GitHelper.IsGitRepository(tempDir);

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void IsGitRepository_NonGitFolder_ReturnsFalse()
        {
            // Act
            var result = GitHelper.IsGitRepository(tempDir);

            // Assert
            result.ShouldBeFalse();
        }

        [Test]
        public void IsGitRepository_SubmoduleFileRef_ReturnsTrue()
        {
            // Arrange - simulate submodule (file reference)
            var gitFile = Path.Combine(tempDir, ".git");
            File.WriteAllText(gitFile, "gitdir: ../.git/modules/mymodule");

            // Act
            var result = GitHelper.IsGitRepository(tempDir);

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void IsGitRepository_NullPath_ReturnsFalse()
        {
            // Act
            var result = GitHelper.IsGitRepository(null!);

            // Assert
            result.ShouldBeFalse();
        }

        [Test]
        public void IsGitRepository_EmptyPath_ReturnsFalse()
        {
            // Act
            var result = GitHelper.IsGitRepository(string.Empty);

            // Assert
            result.ShouldBeFalse();
        }

        [Test]
        public void GetUnstagedChanges_NonGitRepo_ReturnsEmpty()
        {
            // Act
            var result = GitHelper.GetUnstagedChanges(tempDir);

            // Assert
            result.ShouldBeEmpty();
        }

        [Test]
        public void GetChangedFiles_NonGitRepo_ReturnsEmptyList()
        {
            // Act
            var result = GitHelper.GetChangedFiles(tempDir);

            // Assert
            result.ShouldBeEmpty();
        }
    }
}
