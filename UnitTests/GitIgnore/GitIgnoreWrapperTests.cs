//// File: UnitTests/GitIgnoreWrapperTests.cs - NEW TEST FILE
//using NUnit.Framework;
//using Shouldly;
//using DocXHandler;

//namespace UnitTests.GitIgnore
//{
//    [TestFixture]
//    public class GitIgnoreWrapperTests
//    {
//        private string _testDirectory;
//        private GitIgnoreWrapper _wrapper;

//        [SetUp]
//        public void Setup()
//        {
//            _testDirectory = Path.Combine(Path.GetTempPath(), $"GitIgnoreTest_{Guid.NewGuid():N}");
//            Directory.CreateDirectory(_testDirectory);

//            // Create test .gitignore
//            File.WriteAllLines(Path.Combine(_testDirectory, ".gitignore"), new[]
//            {
//                "*.log",
//                "bin/",
//                "obj/"
//            });

//            _wrapper = new GitIgnoreWrapper(_testDirectory);
//        }

//        [TearDown]
//        public void TearDown()
//        {
//            _wrapper?.Dispose();
//            if (Directory.Exists(_testDirectory))
//                Directory.Delete(_testDirectory, true);
//        }

//        [Test]
//        public void ShouldIgnore_LogFiles_ReturnsTrue()
//        {
//            // Arrange
//            var logFile = Path.Combine(_testDirectory, "test.log");
//            File.WriteAllText(logFile, "test");

//            // Act
//            var shouldIgnore = _wrapper.ShouldIgnore(logFile);

//            // Assert
//            shouldIgnore.ShouldBeTrue();
//        }

//        [Test]
//        public void GetNonIgnoredFiles_ShouldExcludeIgnoredFiles()
//        {
//            // Arrange
//            File.WriteAllText(Path.Combine(_testDirectory, "test.log"), "log");
//            File.WriteAllText(Path.Combine(_testDirectory, "test.txt"), "text");

//            // Act
//            var nonIgnoredFiles = _wrapper.GetNonIgnoredFiles(_testDirectory).ToList();

//            // Assert
//            nonIgnoredFiles.ShouldContain(f => f.EndsWith("test.txt"));
//            nonIgnoredFiles.ShouldNotContain(f => f.EndsWith("test.log"));
//        }
//    }
//}
