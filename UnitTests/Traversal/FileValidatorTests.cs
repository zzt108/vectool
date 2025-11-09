using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using VecTool.Configuration;
using VecTool.Handlers.Traversal;
using VecTool.Utils;

namespace UnitTests.Traversal
{
    [TestFixture]
    public class FileValidatorTests
    {
        private string testDir = default!;
        private VectorStoreConfig config = default!;

        [SetUp]
        public void Setup()
        {
            testDir = Path.Combine(Path.GetTempPath(), $"FileValidatorTests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(testDir);
            config = new VectorStoreConfig(testDir);
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, true);
            }
            catch { /* Swallow cleanup exceptions */ }
        }

        [Test]
        public void ShouldIncludeInExport_TextFile_ReturnsTrue()
        {
            // Arrange
            var csFile = CreateTestFile("Test.cs", "using System;");

            // Act
            var result = FileValidator.ShouldIncludeInExport(csFile, config);

            // Assert
            result.ShouldBeTrue();
        }

        [Test]
        public void ShouldIncludeInExport_BinaryFile_ReturnsFalse()
        {
            // Arrange
            var ttfFile = CreateBinaryFile("Font.ttf", 1024);

            // Act
            var result = FileValidator.ShouldIncludeInExport(ttfFile, config);

            // Assert
            result.ShouldBeFalse("Binary files (.ttf) should be excluded by MimeTypeProvider");
        }

        [Test]
        public void ShouldIncludeInExport_ExcludedByConfig_ReturnsFalse()
        {
            // Arrange
            var logFile = CreateTestFile("App.log", "Log entry");
            config.ExcludedFiles.Add("*.log");

            // Act
            var result = FileValidator.ShouldIncludeInExport(logFile, config);

            // Assert
            result.ShouldBeFalse("Files excluded by VectorStoreConfig should be filtered");
        }

        [Test]
        public void ShouldIncludeInExport_CommonCodeFiles_ReturnsTrue()
        {
            // Arrange - Files that should pass MimeTypeProvider.IsBinary check
            var files = new[]
            {
                CreateTestFile("App.config", "<configuration/>"),
                CreateTestFile("Program.cs", "class Program {}"),
                CreateTestFile("README.md", "# Title"),
                CreateTestFile("data.json", "{}"),
                CreateTestFile("Project.csproj", "<Project/>"),
                CreateTestFile("Solution.sln", "# Solution"),
            };

            // Act & Assert
            foreach (var file in files)
            {
                var result = FileValidator.ShouldIncludeInExport(file, config);
                result.ShouldBeTrue($"{Path.GetFileName(file)} should be included (not binary per mdTags.json)");
            }
        }

        [Test]
        public void ShouldIncludeInExport_BinaryFiles_ReturnsFalse()
        {
            // Arrange - Files marked as "application/binary" in mdTags.json
            var binaryFiles = new[]
            {
                CreateBinaryFile("font.ttf", 512),
                CreateBinaryFile("font.otf", 512),
                CreateBinaryFile("font.woff", 256),
                CreateBinaryFile("font.woff2", 256),
                CreateBinaryFile("image.png", 1024),
                CreateBinaryFile("photo.jpg", 2048),
                CreateBinaryFile("archive.zip", 4096),
                CreateBinaryFile("lib.dll", 8192),
                CreateBinaryFile("app.exe", 16384),
            };

            // Act & Assert
            foreach (var file in binaryFiles)
            {
                var result = FileValidator.ShouldIncludeInExport(file, config);
                result.ShouldBeFalse($"{Path.GetFileName(file)} should be excluded (binary per mdTags.json)");
            }
        }

        [Test]
        public void IsBinaryExtension_UseMimeTypeProvider_MatchesMdTagsJson()
        {
            // Arrange - Test known extensions from mdTags.json
            var binaryExtensions = new[] { ".ttf", ".otf", ".woff", ".woff2", ".png", ".jpg", ".dll", ".exe", ".zip" };
            var textExtensions = new[] { ".cs", ".json", ".xml", ".md", ".txt", ".csproj", ".sln" };

            // Act & Assert - Binary extensions
            foreach (var ext in binaryExtensions)
            {
                var result = FileValidator.IsBinary(ext, filePath: null);
                result.ShouldBeTrue($"{ext} should be binary per mdTags.json");
            }

            // Act & Assert - Text extensions
            foreach (var ext in textExtensions)
            {
                var result = FileValidator.IsBinary(ext, filePath: null);
                result.ShouldBeFalse($"{ext} should NOT be binary per mdTags.json");
            }
        }

        [Test]
        public void MimeTypeProvider_IsBinary_FontFiles()
        {
            // Arrange - Font extensions that triggered the original bug
            var fontExtensions = new[] { ".ttf", ".otf", ".woff", ".woff2", ".eot" };

            // Act & Assert
            foreach (var ext in fontExtensions)
            {
                var isBinary = FileValidator.IsBinary(ext, null);
                isBinary.ShouldBeTrue($"{ext} should be marked as binary in mdTags.json");
            }
        }

        [Test]
        public void ShouldIncludeInExport_EmptyFile_ReturnsFalse()
        {
            // Arrange
            var emptyFile = CreateTestFile("Empty.cs", string.Empty);

            // Act
            var result = FileValidator.ShouldIncludeInExport(emptyFile, config);

            // Assert
            result.ShouldBeFalse("Empty files should be excluded by IsFileValid");
        }

        [Test]
        public void ShouldIncludeInExport_NullOrEmptyPath_ReturnsFalse()
        {
            // Act & Assert
            FileValidator.ShouldIncludeInExport(null!, config).ShouldBeFalse();
            FileValidator.ShouldIncludeInExport(string.Empty, config).ShouldBeFalse();
            FileValidator.ShouldIncludeInExport("   ", config).ShouldBeFalse();
        }

        [Test]
        public void ShouldIncludeInExport_NonExistentFile_ReturnsFalse()
        {
            // Arrange
            var fakePath = Path.Combine(testDir, "DoesNotExist.cs");

            // Act
            var result = FileValidator.ShouldIncludeInExport(fakePath, config);

            // Assert
            result.ShouldBeFalse("Non-existent files should be excluded");
        }

        [Test]
        public void ShouldIncludeInExport_MultipleExclusionPatterns_WorkCorrectly()
        {
            // Arrange
            config.ExcludedFiles.Add("*.log");
            config.ExcludedFiles.Add("*.tmp");
            config.ExcludedFiles.Add("Debug*");

            var logFile = CreateTestFile("App.log", "Log content");
            var tmpFile = CreateTestFile("Temp.tmp", "Temp content");
            var debugFile = CreateTestFile("Debug.cs", "Debug code");
            var normalFile = CreateTestFile("Program.cs", "Normal code");

            // Act & Assert
            FileValidator.ShouldIncludeInExport(logFile, config).ShouldBeFalse();
            FileValidator.ShouldIncludeInExport(tmpFile, config).ShouldBeFalse();
            FileValidator.ShouldIncludeInExport(debugFile, config).ShouldBeFalse();
            FileValidator.ShouldIncludeInExport(normalFile, config).ShouldBeTrue();
        }

        [Test]
        public void ShouldIncludeInExport_ConsistencyBetweenMDHandlerAndSummaryHandler()
        {
            // Arrange - Mix of files that should/shouldn't be included
            var shouldInclude = new[]
            {
                CreateTestFile("Code.cs", "class Test {}"),
                CreateTestFile("Config.json", "{}"),
                CreateTestFile("Readme.md", "# Title"),
            };

            var shouldExclude = new[]
            {
                CreateBinaryFile("Font.ttf", 1024),   // ← The original 180 .ttf files
                CreateBinaryFile("Image.png", 2048),
                CreateBinaryFile("Archive.zip", 4096),
            };

            // Act & Assert - These results MUST be identical for MDHandler and FileSizeSummaryHandler
            foreach (var file in shouldInclude)
            {
                FileValidator.ShouldIncludeInExport(file, config)
                    .ShouldBeTrue($"{Path.GetFileName(file)} should be included in BOTH export and summary");
            }

            foreach (var file in shouldExclude)
            {
                FileValidator.ShouldIncludeInExport(file, config)
                    .ShouldBeFalse($"{Path.GetFileName(file)} should be excluded from BOTH export and summary");
            }
        }

        // ✅ Helper methods
        private string CreateTestFile(string name, string content)
        {
            var path = Path.Combine(testDir, name);
            File.WriteAllText(path, content);
            return path;
        }

        private string CreateBinaryFile(string name, int sizeBytes)
        {
            var path = Path.Combine(testDir, name);
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.SetLength(sizeBytes);
            }
            return path;
        }
    }
}
