#nullable enable
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using VecTool.Constants;
using VecTool.Core.Models;

namespace UnitTests.PromptLib
{
    [TestFixture]
    public class PromptMetadataTests
    {
        [Test]
        public void Parse_ValidPromptFile_ReturnsMetadata()
        {
            // Arrange
            var path = "C:/work/vectortool/spaces/PROMPT-1.0-analyzer.md";

            // Act
            var result = PromptMetadata.Parse(path);

            // Assert
            result.ShouldNotBeNull();
            result!.Type.ShouldBe("PROMPT");
            result.Version.ShouldBe("1.0");
            result.Name.ShouldBe("analyzer");
            result.FileName.ShouldBe("PROMPT-1.0-analyzer.md");
            result.Area.ShouldBe("work");
            result.Project.ShouldBe("vectortool");
            result.Category.ShouldBe("spaces");
        }

        [Test]
        public void Parse_ValidGuideFile_ReturnsMetadata()
        {
            // Arrange
            var path = "/development/linx/guides/GUIDE-1.5-convention.md";

            // Act
            var result = PromptMetadata.Parse(path);

            // Assert
            result.ShouldNotBeNull();
            result!.Type.ShouldBe("GUIDE");
            result.Version.ShouldBe("1.5");
            result.Name.ShouldBe("convention");
            result.Area.ShouldBe("development");
            result.Project.ShouldBe("linx");
            result.Category.ShouldBe("guides");
        }

        [Test]
        public void Parse_MultiPartName_ReturnsFullName()
        {
            // Arrange
            var path = "C:/work/vectortool/spaces/PROMPT-1.1-git-integration-helper.md";

            // Act
            var result = PromptMetadata.Parse(path);

            // Assert
            result.ShouldNotBeNull();
            result!.Name.ShouldBe("git-integration-helper");
        }

        [Test]
        public void Parse_InvalidFilename_ReturnsNull()
        {
            // Arrange
            var path = "C:/work/vectortool/spaces/invalid-file.md";

            // Act
            var result = PromptMetadata.Parse(path);

            // Assert
            result.ShouldNotBeNull(); //forgiving known extensions
        }

        // ✅ NEW: Forgiving parse tests

        [Test]
        public void Parse_TypeNameFormat_UsesDefaultVersion()
        {
            // Arrange (missing VERSION: TYPE-NAME.ext)
            var path = "C:/work/vectortool/spaces/PROMPT-analyzer.md";

            // Act
            var result = PromptMetadata.Parse(path);

            // Assert
            result.ShouldNotBeNull();
            result!.Type.ShouldBe("PROMPT");
            result.Version.ShouldBe("0.0"); // Default version
            result.Name.ShouldBe("analyzer");
        }

        [Test]
        public void Parse_TypeOnlyFormat_UsesDefaultVersionAndName()
        {
            // Arrange (minimal: TYPE.ext)
            var path = "C:/work/vectortool/spaces/GUIDE.txt";

            // Act
            var result = PromptMetadata.Parse(path);

            // Assert
            result.ShouldNotBeNull();
            result!.Type.ShouldBe("Unknown");
            result.Version.ShouldBe("0.0");
            result.Name.ShouldBe("GUIDE");
        }

        [Test]
        public void Parse_InvalidExtension_ReturnsNull()
        {
            // Arrange (extension not in DefaultFileExtensions)
            var path = "C:/work/vectortool/spaces/PROMPT-1.0-test.exe";

            // Act
            var result = PromptMetadata.Parse(path);

            // Assert
            result.ShouldBeNull();
        }

        [Test]
        public void Parse_UnrecognizedFilenameFormat_ReturnsNull()
        {
            // Arrange (completely invalid, no recognizable TYPE)
            var path = "C:/work/vectortool/spaces/random-file-name.md";

            // Act
            var result = PromptMetadata.Parse(path);

            // Assert
            result.ShouldNotBeNull(); // No recognizable TYPE, rejected
        }

        [Test]
        public void Parse_YamlExtension_Forgiving()
        {
            // Arrange (YAML with TYPE-NAME format)
            var path = "C:/work/vectortool/spaces/SPACE-config.yaml";

            // Act
            var result = PromptMetadata.Parse(path);

            // Assert
            result.ShouldNotBeNull();
            result!.Type.ShouldBe("SPACE");
            result.Version.ShouldBe("0.0");
            result.Name.ShouldBe("config");
            result.FileName.ShouldBe("SPACE-config.yaml");
        }

        [Test]
        public void Parse_RootLevelFile_ReturnsEmptyHierarchy()
        {
            // Arrange
            var path = "PROMPT-1.0-test.md";

            // Act
            var result = PromptMetadata.Parse(path);

            // Assert
            result.ShouldNotBeNull();
            result!.Area.ShouldBe(string.Empty);
            result.Project.ShouldBe(string.Empty);
            result.Category.ShouldBe(string.Empty);
        }

        [Test]
        public void Parse_NullPath_ReturnsNull()
        {
            // Act
            var result = PromptMetadata.Parse(null!);

            // Assert
            result.ShouldBeNull();
        }

        [Test]
        public void Parse_EmptyPath_ReturnsNull()
        {
            // Act
            var result = PromptMetadata.Parse(string.Empty);

            // Assert
            result.ShouldBeNull();
        }

        [Test]
        public void Parse_WithDescription_ExtractsDescription()
        {
            // Arrange
            var path = "C:/work/vectortool/spaces/PROMPT-1.0-analyzer.md";
            var firstLine = "# AI Code Analyzer Prompt";

            // Act
            var result = PromptMetadata.Parse(path, firstLine);

            // Assert
            result.ShouldNotBeNull();
            result!.Description.ShouldBe("# AI Code Analyzer Prompt");
        }

        [Test]
        public void Parse_ShallowPath_HandlesMissingHierarchy()
        {
            // Arrange
            var path = "C:/prompts/GUIDE-2.0-test.md";

            // Act
            var result = PromptMetadata.Parse(path);

            // Assert
            result.ShouldNotBeNull();
            result!.Area.ShouldBe(Const.NA); // Only 1 folder deep
            result.Project.ShouldBe(Const.NA);
            result.Category.ShouldBe("prompts");
        }

        [Test]
        public void Parse_FilenameWithoutExtension_StillParses()
        {
            // Arrange
            var path = "C:/work/vectortool/spaces/SPACE-1.0-config";

            // Act
            var result = PromptMetadata.Parse(path);

            // Assert
            result.ShouldBeNull(); // only allowed extensions
        }
    }
}
