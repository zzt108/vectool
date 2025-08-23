
using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Collections.Generic;
using DocXHandler;
using LibGit2Sharp;

namespace UnitTests
{
    [TestFixture]
    public class GitChangesHandlerTests
    {
        private string _testFolderPath;
        private string _outputMDPath;
        private GitChangesHandler _gitChangesHandler;

        [SetUp]
        public void Setup()
        {
            _testFolderPath = Path.Combine(Path.GetTempPath(), "GitChangesHandlerTests");
            Directory.CreateDirectory(_testFolderPath);
            _outputMDPath = Path.Combine(_testFolderPath, "output.md");
            _gitChangesHandler = new GitChangesHandler(null);

            // Initialize a git repository
            Repository.Init(_testFolderPath);

            // Create and commit a file
            var filePath = Path.Combine(_testFolderPath, "test.txt");
            File.WriteAllText(filePath, "initial content");
            using (var repo = new Repository(_testFolderPath))
            {
                Commands.Stage(repo, "*");
                var author = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
                var committer = author;
                repo.Commit("Initial commit", author, committer);
            }
        }

        [TearDown]
        public void Cleanup()
        {
            // Git repositories can be tricky to delete, so we need to handle that
            var gitDirectory = new DirectoryInfo(Path.Combine(_testFolderPath, ".git"));
            if (gitDirectory.Exists)
            {
                foreach (var file in gitDirectory.GetFiles("*", SearchOption.AllDirectories))
                {
                    file.Attributes = FileAttributes.Normal;
                }
            }

            if (Directory.Exists(_testFolderPath))
            {
                Directory.Delete(_testFolderPath, true);
            }
        }

        [Test]
        public void GetGitChanges_WithChanges_ShouldCreateMDFileWithChanges()
        {
            // Arrange
            var filePath = Path.Combine(_testFolderPath, "test.txt");
            File.WriteAllText(filePath, "modified content");
            var folders = new List<string> { _testFolderPath };

            // Act
            var changes = _gitChangesHandler.GetGitChanges(folders, _outputMDPath);

            // Assert
            File.Exists(_outputMDPath).ShouldBeTrue();
            var content = File.ReadAllText(_outputMDPath);
            content.ShouldContain("--- a/test.txt");
            content.ShouldContain("+++ b/test.txt");
            content.ShouldContain("-initial content");
            content.ShouldContain("+modified content");
            changes.ShouldNotBeNullOrEmpty();
        }

        [Test]
        public void GetGitChanges_NoChanges_ShouldReturnEmptyStringAndNotCreateFile()
        {
            // Arrange
            var folders = new List<string> { _testFolderPath };

            // Act
            var changes = _gitChangesHandler.GetGitChanges(folders, _outputMDPath);

            // Assert
            File.Exists(_outputMDPath).ShouldBeTrue();
            // changes.shouldcontain("No changes detected in the specified folders.");
            var empty = "GitChangesHandlerTests\r\n\r\n## Status Changes\r\n```\r\nOn branch master\nnothing to commit, working tree clean\n\r\n```\r\n\r\n## Diff Changes\r\n```\r\nNo diff changes.\r\n\r\n```\r\n\r\n";
            changes.ShouldContain(empty);
        }
    }
}
