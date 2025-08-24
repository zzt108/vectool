//using System;
//using NUnit.Framework;
//using Shouldly;

//namespace UnitTests.GitIgnore
//{
//    [TestFixture]
//    public class GitIgnorePatternTests
//    {
//        private const string TestDirectory = @"C:\TestProject";

//        [Test]
//        public void Constructor_WithValidPattern_ShouldCreatePattern()
//        {
//            // Arrange
//            var pattern = "*.log";

//            // Act
//            var gitIgnorePattern = new GitIgnorePattern(pattern, TestDirectory);

//            // Assert
//            gitIgnorePattern.OriginalPattern.ShouldBe("*.log");
//            gitIgnorePattern.SourceDirectory.ShouldBe(TestDirectory);
//            gitIgnorePattern.IsNegation.ShouldBe(false);
//            gitIgnorePattern.IsDirectoryOnly.ShouldBe(false);
//            gitIgnorePattern.IsValid.ShouldBe(true);
//        }

//        [Test]
//        public void Constructor_WithNegationPattern_ShouldDetectNegation()
//        {
//            // Arrange
//            var pattern = "!important.log";

//            // Act
//            var gitIgnorePattern = new GitIgnorePattern(pattern, TestDirectory);

//            // Assert
//            gitIgnorePattern.IsNegation.ShouldBe(true);
//            gitIgnorePattern.ProcessedPattern.ShouldBe("important.log");
//        }

//        [Test]
//        public void Constructor_WithDirectoryPattern_ShouldDetectDirectoryOnly()
//        {
//            // Arrange
//            var pattern = "build/";

//            // Act
//            var gitIgnorePattern = new GitIgnorePattern(pattern, TestDirectory);

//            // Assert
//            gitIgnorePattern.IsDirectoryOnly.ShouldBe(true);
//            gitIgnorePattern.ProcessedPattern.ShouldBe("build");
//        }

//        [Test]
//        public void Constructor_WithRootRelativePattern_ShouldDetectRootRelative()
//        {
//            // Arrange
//            var pattern = "/bin/debug";

//            // Act
//            var gitIgnorePattern = new GitIgnorePattern(pattern, TestDirectory);

//            // Assert
//            gitIgnorePattern.IsRootRelative.ShouldBe(true);
//            gitIgnorePattern.ProcessedPattern.ShouldBe("bin/debug");
//        }

//        [TestCase("# This is a comment", TestName = "Constructor_WithCommentPattern_ShouldCreateInvalidPattern")]
//        [TestCase("   ", TestName = "Constructor_WithEmptyPattern_ShouldCreateInvalidPattern")]
//        public void Constructor_WithInvalidInput_ShouldCreateInvalidPattern(string pattern)
//        {
//            // Act
//            var gitIgnorePattern = new GitIgnorePattern(pattern, TestDirectory);

//            // Assert
//            gitIgnorePattern.IsValid.ShouldBe(false);
//        }

//        [Test]
//        public void Constructor_WithNullPattern_ShouldThrowException()
//        {
//            // Act & Assert
//            Should.Throw<ArgumentNullException>(() => new GitIgnorePattern(null!, TestDirectory));
//        }

//        [Test]
//        public void IsMatch_WithWildcardPattern_ShouldMatchCorrectFiles()
//        {
//            // Arrange
//            var pattern = new GitIgnorePattern("*.log", TestDirectory);

//            // Act & Assert
//            pattern.IsMatch("application.log", false).ShouldBe(true);
//            pattern.IsMatch("error.log", false).ShouldBe(true);
//            pattern.IsMatch("application.txt", false).ShouldBe(false);
//            pattern.IsMatch("log.txt", false).ShouldBe(false);
//        }

//        [Test]
//        public void IsMatch_WithDirectoryOnlyPattern_ShouldOnlyMatchDirectories()
//        {
//            // Arrange
//            var pattern = new GitIgnorePattern("build/", TestDirectory);

//            // Act & Assert
//            pattern.IsMatch("build", true).ShouldBe(true);    // Directory
//            pattern.IsMatch("build", false).ShouldBe(false);  // File
//        }

//        [Test]
//        public void IsMatch_WithDoubleAsteriskPattern_ShouldMatchRecursively()
//        {
//            // Arrange
//            var pattern = new GitIgnorePattern("**/temp", TestDirectory);
//            var pattern2 = new GitIgnorePattern("**/temp/", TestDirectory);

//            // Act & Assert
//            pattern.IsMatch("temp", false).ShouldBe(true);
//            pattern.IsMatch("src/temp", false).ShouldBe(true);
//            pattern.IsMatch("src/deep/temp", false).ShouldBe(true);
//            pattern.IsMatch("temp/file.txt", false).ShouldBe(false);

//            // A pattern ending in `/` should match files inside that directory.
//            pattern2.IsMatch("temp", true).ShouldBe(true);
//            pattern2.IsMatch("temp/file.txt", false).ShouldBe(true);
//        }

//        [Test]
//        public void IsMatch_WithQuestionMarkPattern_ShouldMatchSingleChar()
//        {
//            // Arrange
//            var pattern = new GitIgnorePattern("file?.txt", TestDirectory);

//            // Act & Assert
//            pattern.IsMatch("file1.txt", false).ShouldBe(true);
//            pattern.IsMatch("fileA.txt", false).ShouldBe(true);
//            pattern.IsMatch("file12.txt", false).ShouldBe(false);
//            pattern.IsMatch("file.txt", false).ShouldBe(false);
//        }

//        [Test]
//        public void IsMatch_WithRootRelativePattern_ShouldMatchFromRoot()
//        {
//            // Arrange
//            var pattern = new GitIgnorePattern("/bin", TestDirectory);

//            // Act & Assert
//            pattern.IsMatch("bin", false).ShouldBe(true);
//            pattern.IsMatch("src/bin", false).ShouldBe(false);
//        }

//        [Test]
//        public void IsMatch_WithComplexPattern_ShouldHandleCorrectly()
//        {
//            // Arrange
//            var pattern = new GitIgnorePattern("src/**/*.tmp", TestDirectory);

//            // Act & Assert
//            pattern.IsMatch("src/test.tmp", false).ShouldBe(true);
//            pattern.IsMatch("src/deep/nested/test.tmp", false).ShouldBe(true);
//            pattern.IsMatch("lib/test.tmp", false).ShouldBe(false);
//        }

//        [Test]
//        public void IsMatch_WithDoubleAsteriskObjPattern_ShouldMatchProjectAssetsJson()
//        {
//            // Arrange
//            var pattern = new GitIgnorePattern("**/obj", TestDirectory);

//            // Act & Assert
//            // Should match obj directory itself
//            pattern.IsMatch("obj", true).ShouldBe(true);
//            pattern.IsMatch("src/obj", true).ShouldBe(true);
//            pattern.IsMatch("src/deep/obj", true).ShouldBe(true);

//            // Should match files directly inside obj
//            pattern.IsMatch("obj/file.txt", false).ShouldBe(true);
//            pattern.IsMatch("src/obj/file.txt", false).ShouldBe(true);
//            pattern.IsMatch("src/deep/obj/file.txt", false).ShouldBe(true);

//            // Specifically test project.assets.json
//            pattern.IsMatch("MimeTypes/obj/project.assets.json", false).ShouldBe(true);
//            pattern.IsMatch("AnotherProject/bin/Debug/net8.0/obj/project.assets.json", false).ShouldBe(true);

//            // Should not match if "obj" is part of a filename or not a directory segment
//            pattern.IsMatch("object.txt", false).ShouldBe(false);
//            pattern.IsMatch("myobjfile.log", false).ShouldBe(false);
//            pattern.IsMatch("notobj/file.txt", false).ShouldBe(false);
//        }
//    }
//}