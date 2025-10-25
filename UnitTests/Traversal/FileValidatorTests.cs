// ✅ FULL FILE VERSION
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using VecTool.Configuration;
using VecTool.Handlers.Traversal;

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
            config = new VectorStoreConfig();
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
            result.ShouldBeFalse("Binary files (.ttf) should be excluded");
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
            result.ShouldBeFalse("Files excluded by config should be filtered");
        }

        [Test]
        public void ShouldIncludeInExport_CommonCodeFiles_ReturnsTrue()
        {
            // Arrange
            var files = new[]
            {
                CreateTestFile("Program.cs", "class Program {}"),
                CreateTestFile("App.config", "<configuration/>"),
                CreateTestFile("README.md", "# Title"),
                CreateTestFile("data.json", "{}"),
                CreateTestFile("styles.css", "body {}"),
                CreateTestFile("Project.csproj", "<Project/>"),
                CreateTestFile("Solution.sln", "# Solution"),
            };

            // Act & Assert
            foreach (var file in files)
            {
                var result = FileValidator.ShouldIncludeInExport(file, config);
                result.ShouldBeTrue($"{Path.GetFileName(file)} should be included");
            }
        }

        [Test]
        public void ShouldIncludeInExport_BinaryFiles_ReturnsFalse()
        {
            // Arrange
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
                result.ShouldBeFalse($"{Path.GetFileName(file)} should be excluded (binary)");
            }
        }

        [Test]
        public void IsTextFile_CommonCodeExtensions_ReturnsTrue()
        {
            // Arrange
            var textFiles = new[]
            {
                CreateTestFile("code.cs", ""),
                CreateTestFile("project.csproj", ""),
                CreateTestFile("solution.sln", ""),
                CreateTestFile("readme.md", ""),
                CreateTestFile("config.json", ""),
                CreateTestFile("data.xml", ""),
                CreateTestFile("styles.css", ""),
                CreateTestFile("script.js", ""),
                CreateTestFile("page.html", ""),
                CreateTestFile("settings.yml", ""),
                CreateTestFile("query.sql", ""),
            };

            // Act & Assert
            foreach (var file in textFiles)
            {
                var result = FileValidator.IsTextFile(file);
                result.ShouldBeTrue($"{Path.GetExtension(file)} should be recognized as text");
            }
        }

        [Test]
        public void IsTextFile_BinaryExtensions_ReturnsFalse()
        {
            // Arrange
            var binaryFiles = new[]
            {
                CreateBinaryFile("font.ttf", 100),
                CreateBinaryFile("font.otf", 100),
                CreateBinaryFile("font.woff", 100),
                CreateBinaryFile("font.woff2", 100),
                CreateBinaryFile("image.png", 100),
                CreateBinaryFile("photo.jpg", 100),
                CreateBinaryFile("video.mp4", 100),
                CreateBinaryFile("archive.zip", 100),
                CreateBinaryFile("lib.dll", 100),
                CreateBinaryFile("app.exe", 100),
            };

            // Act & Assert
            foreach (var file in binaryFiles)
            {
                var result = FileValidator.IsTextFile(file);
                result.ShouldBeFalse($"{Path.GetExtension(file)} should be recognized as binary");
            }
        }

        [Test]
        public void ShouldIncludeInExport_EmptyFile_ReturnsFalse()
        {
            // Arrange
            var emptyFile = CreateTestFile("Empty.cs", "");

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
            FileValidator.ShouldIncludeInExport("", config).ShouldBeFalse();
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
        public void IsTextFile_ConfigFilesAndDocumentation_ReturnsTrue()
        {
            // Arrange - Files commonly used in development
            var devFiles = new[]
            {
                CreateTestFile(".gitignore", "bin/\nobj/"),
                CreateTestFile(".editorconfig", "root = true"),
                CreateTestFile("README.md", "# Project"),
                CreateTestFile("CHANGELOG.md", "## v1.0"),
                CreateTestFile("package.json", "{}"),
                CreateTestFile("appsettings.json", "{}"),
                CreateTestFile("NLog.config", "<nlog/>"),
                CreateTestFile("build.bat", "@echo off"),
                CreateTestFile("deploy.sh", "#!/bin/bash"),
            };

            // Act & Assert
            foreach (var file in devFiles)
            {
                var result = FileValidator.IsTextFile(file);
                result.ShouldBeTrue($"{Path.GetFileName(file)} should be recognized as text");
            }
        }

        [Test]
        public void ShouldIncludeInExport_ConsistencyWithMDHandlerAndSummaryHandler()
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
                CreateBinaryFile("Font.ttf", 1024),
                CreateBinaryFile("Image.png", 2048),
                CreateBinaryFile("Archive.zip", 4096),
            };

            // Act & Assert - These results should be identical for MDHandler and FileSizeSummaryHandler
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

        // Helper methods
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
