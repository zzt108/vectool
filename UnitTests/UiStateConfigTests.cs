// File: UnitTests/Configuration/UiStateConfigTests.cs

using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using VecTool.Configuration;

namespace UnitTests.Configuration
{
    [TestFixture]
    public class UiStateConfigTests
    {
        [Test]
        public void Save_Then_Load_Should_Roundtrip_LastSelectedVectorStore()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "UiStateConfigTests_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            var expected = "DocsStore";

            // Act
            UiStateConfig.Save(new UiStateConfig { LastSelectedVectorStore = expected }, tempDir);
            var loaded = UiStateConfig.Load(tempDir);

            // Assert
            loaded.ShouldNotBeNull("Load should return a non-null UiStateConfig even if file is new.");
            loaded.LastSelectedVectorStore.ShouldBe(expected);

            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Test]
        public void Load_When_File_Missing_Should_Return_Default_Instance()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "UiStateConfigTests_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            // Act
            var loaded = UiStateConfig.Load(tempDir);

            // Assert
            loaded.ShouldNotBeNull();
            loaded.LastSelectedVectorStore.ShouldBeNull();

            // Cleanup
            Directory.Delete(tempDir, true);
        }

        [Test]
        public void Load_When_File_Corrupt_Should_Not_Throw_And_Return_Default_Instance()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), "UiStateConfigTests_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            var path = Path.Combine(tempDir, "uiState.json");
            File.WriteAllText(path, "{ this is not valid json");

            // Act
            var loaded = UiStateConfig.Load(tempDir);

            // Assert
            loaded.ShouldNotBeNull();
            loaded.LastSelectedVectorStore.ShouldBeNull();

            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }
}
