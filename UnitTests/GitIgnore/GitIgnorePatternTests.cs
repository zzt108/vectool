using System;
using NUnit.Framework;
using Shouldly;
using GitIgnore.Models;

namespace GitIgnore.Tests
{
    [TestFixture]
    public class GitIgnorePatternTests
    {
        private const string TestDirectory = @"C:\TestProject";

        [SetUp]
        public void Setup()
        {
            // Setup runs before each test method
        }

        [Test]
        public void Constructor_WithValidPattern_ShouldCreatePattern()
        {
            // Arrange
            var pattern = "*.log";

            // Act
            var gitIgnorePattern = new GitIgnorePattern(pattern, TestDirectory);

            // Assert
            gitIgnorePattern.OriginalPattern.ShouldBe("*.log");
            gitIgnorePattern.SourceDirectory.ShouldBe(TestDirectory);
            gitIgnorePattern.IsNegation.ShouldBe(false);
            gitIgnorePattern.IsDirectoryOnly.ShouldBe(false);
            gitIgnorePattern.IsValid.ShouldBe(true);
        }

        [Test]
        public void Constructor_WithNegationPattern_ShouldDetectNegation()
        {
            // Arrange
            var pattern = "!important.log";

            // Act
            var gitIgnorePattern = new GitIgnorePattern(pattern, TestDirectory);

            // Assert
            gitIgnorePattern.IsNegation.ShouldBe(true);
            gitIgnorePattern.ProcessedPattern.ShouldBe("important.log");
        }

        [Test]
        public void Constructor_WithDirectoryPattern_ShouldDetectDirectoryOnly()
        {
            // Arrange
            var pattern = "build/";

            // Act
            var gitIgnorePattern = new GitIgnorePattern(pattern, TestDirectory);

            // Assert
            gitIgnorePattern.IsDirectoryOnly.ShouldBe(true);
            gitIgnorePattern.ProcessedPattern.ShouldBe("build");
        }

        [Test]
        public void Constructor_WithRootRelativePattern_ShouldDetectRootRelative()
        {
            // Arrange
            var pattern = "/bin/debug";

            // Act
            var gitIgnorePattern = new GitIgnorePattern(pattern, TestDirectory);

            // Assert
            gitIgnorePattern.IsRootRelative.ShouldBe(true);
            gitIgnorePattern.ProcessedPattern.ShouldBe("bin/debug");
        }

        [Test]
        public void Constructor_WithCommentPattern_ShouldCreateInvalidPattern()
        {
            // Arrange
            var pattern = "# This is a comment";

            // Act
            var gitIgnorePattern = new GitIgnorePattern(pattern, TestDirectory);

            // Assert
            gitIgnorePattern.IsValid.ShouldBe(false);
        }

        [Test]
        public void Constructor_WithEmptyPattern_ShouldCreateInvalidPattern()
        {
            // Arrange
            var pattern = "   ";

            // Act
            var gitIgnorePattern = new GitIgnorePattern(pattern, TestDirectory);

            // Assert
            gitIgnorePattern.IsValid.ShouldBe(false);
        }

        [Test]
        public void Constructor_WithNullPattern_ShouldThrowException()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => new GitIgnorePattern(null, TestDirectory));
        }

        [Test]
        public void IsMatch_WithWildcardPattern_ShouldMatchCorrectFiles()
        {
            // Arrange
            var pattern = new GitIgnorePattern("*.log", TestDirectory);

            // Act & Assert
            pattern.IsMatch("application.log", false).ShouldBe(true);
            pattern.IsMatch("error.log", false).ShouldBe(true);
            pattern.IsMatch("application.txt", false).ShouldBe(false);
            pattern.IsMatch("log.txt", false).ShouldBe(false);
        }

        [Test]
        public void IsMatch_WithDirectoryOnlyPattern_ShouldOnlyMatchDirectories()
        {
            // Arrange
            var pattern = new GitIgnorePattern("build/", TestDirectory);

            // Act & Assert
            pattern.IsMatch("build", true).ShouldBe(true);   // Directory
            pattern.IsMatch("build", false).ShouldBe(false);  // File
        }

        [Test]
        public void IsMatch_WithDoubleAsteriskPattern_ShouldMatchRecursively()
        {
            // Arrange
            var pattern = new GitIgnorePattern("**/temp", TestDirectory);

            // Act & Assert
            pattern.IsMatch("temp", false).ShouldBe(true);
            pattern.IsMatch("src/temp", false).ShouldBe(true);
            pattern.IsMatch("src/deep/temp", false).ShouldBe(true);
            pattern.IsMatch("temp/file.txt", false).ShouldBe(false);
        }

        [Test]
        public void IsMatch_WithQuestionMarkPattern_ShouldMatchSingleChar()
        {
            // Arrange
            var pattern = new GitIgnorePattern("file?.txt", TestDirectory);

            // Act & Assert
            pattern.IsMatch("file1.txt", false).ShouldBe(true);
            pattern.IsMatch("fileA.txt", false).ShouldBe(true);
            pattern.IsMatch("file12.txt", false).ShouldBe(false);
            pattern.IsMatch("file.txt", false).ShouldBe(false);
        }

        [Test]
        public void IsMatch_WithRootRelativePattern_ShouldMatchFromRoot()
        {
            // Arrange
            var pattern = new GitIgnorePattern("/bin", TestDirectory);

            // Act & Assert
            pattern.IsMatch("bin", false).ShouldBe(true);
            pattern.IsMatch("src/bin", false).ShouldBe(false);
        }

        [Test]
        public void IsMatch_WithComplexPattern_ShouldHandleCorrectly()
        {
            // Arrange
            var pattern = new GitIgnorePattern("src/**/*.tmp", TestDirectory);

            // Act & Assert
            pattern.IsMatch("src/test.tmp", false).ShouldBe(true);
            pattern.IsMatch("src/deep/nested/test.tmp", false).ShouldBe(true);
            pattern.IsMatch("lib/test.tmp", false).ShouldBe(false);
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup after each test method
        }
    }
}