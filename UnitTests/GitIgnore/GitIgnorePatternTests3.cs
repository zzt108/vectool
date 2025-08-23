using GitIgnore.Models;
using NUnit.Framework;
using Shouldly;

namespace GitIgnore.Tests
{
    [TestFixture]
    public class GitIgnorePatternTests
    {
        private const string DUMMY_SOURCE_DIR = "C:\\repo";

        [Test]
        public void IsMatch_RootRelativePath_MatchesDirectoryAndContents()
        {
            // Arrange
            var pattern = new GitIgnorePattern("src/obj", DUMMY_SOURCE_DIR);

            // Assert
            // A pattern for a directory should match the directory itself.
            pattern.IsMatch("src/obj", true).ShouldBeTrue("Should match the directory itself.");

            // It should also match files and subdirectories within that directory.
            // Based on the current implementation, this assertion will likely fail, 
            // demonstrating that the regex is too strict (anchored with '$').
            pattern.IsMatch("src/obj/file.txt", false).ShouldBeTrue("Should match a file inside the directory.");
            pattern.IsMatch("src/obj/sub/file.cs", false).ShouldBeTrue("Should match a file in a subdirectory.");

            // It should not match partial or different paths.
            pattern.IsMatch("src/objects", true).ShouldBeFalse("Should not match a different directory with a similar name.");
            pattern.IsMatch("source/obj", true).ShouldBeFalse("Should not match if not anchored to root.");
        }
    }
}