#nullable enable
using NUnit.Framework;
using Shouldly;
using VecTool.Core.Helpers;

namespace UnitTests.Core.Helpers
{
    [TestFixture]
    public sealed class FavoritesManagerTests
    {
        private string tempDir = null!;
        private FavoritesManager manager = null!;

        [SetUp]
        public void Setup()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "FavoritesManagerTests", Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            manager = new FavoritesManager();
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
        public void LoadFavorites_FileDoesNotExist_ReturnsEmptyList()
        {
            // Arrange
            var configPath = Path.Combine(tempDir, "missing.json");

            // Act
            var result = manager.LoadFavorites(configPath);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Test]
        public void LoadFavorites_ValidJson_ReturnsCorrectList()
        {
            // Arrange
            var configPath = Path.Combine(tempDir, "favorites.json");
            var json = @"{
                ""favorites"": [
                    { ""path"": ""C:/prompts/PROMPT-1.0-analyzer.md"", ""label"": ""Analyzer"", ""rank"": 1 },
                    { ""path"": ""C:/prompts/GUIDE-1.5-convention.md"", ""label"": ""Convention"", ""rank"": 2 }
                ]
            }";
            File.WriteAllText(configPath, json);

            // Act
            var result = manager.LoadFavorites(configPath);

            // Assert
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
            result[0].ShouldBe("C:/prompts/PROMPT-1.0-analyzer.md");
            result[1].ShouldBe("C:/prompts/GUIDE-1.5-convention.md");
        }

        [Test]
        public void LoadFavorites_InvalidJson_ReturnsEmptyList()
        {
            // Arrange
            var configPath = Path.Combine(tempDir, "invalid.json");
            File.WriteAllText(configPath, "{ invalid json }");

            // Act
            var result = manager.LoadFavorites(configPath);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Test]
        public void LoadFavorites_NullPath_ReturnsEmptyList()
        {
            // Act
            var result = manager.LoadFavorites(null!);

            // Assert
            result.ShouldNotBeNull();
            result.ShouldBeEmpty();
        }

        [Test]
        public void SaveFavorites_ValidList_CreatesJsonFile()
        {
            // Arrange
            var configPath = Path.Combine(tempDir, "favorites.json");
            var favorites = new List<string>
            {
                "C:/prompts/PROMPT-1.0-analyzer.md",
                "C:/prompts/GUIDE-1.5-convention.md"
            };

            // Act
            manager.SaveFavorites(configPath, favorites);

            // Assert
            File.Exists(configPath).ShouldBeTrue();
            var json = File.ReadAllText(configPath);
            json.ShouldContain("PROMPT-1.0-analyzer.md");
            json.ShouldContain("GUIDE-1.5-convention.md");
        }

        [Test]
        public void SaveFavorites_EmptyList_CreatesEmptyJson()
        {
            // Arrange
            var configPath = Path.Combine(tempDir, "empty.json");
            var favorites = new List<string>();

            // Act
            manager.SaveFavorites(configPath, favorites);

            // Assert
            File.Exists(configPath).ShouldBeTrue();
            var json = File.ReadAllText(configPath);
            json.ShouldContain("\"favorites\": []");
        }

        [Test]
        public void SaveFavorites_NullPath_ThrowsArgumentException()
        {
            // Arrange
            var favorites = new List<string> { "test.md" };

            // Act & Assert
            Should.Throw<ArgumentException>(() => manager.SaveFavorites(null!, favorites));
        }

        [Test]
        public void SaveFavorites_NullList_ThrowsArgumentNullException()
        {
            // Arrange
            var configPath = Path.Combine(tempDir, "test.json");

            // Act & Assert
            Should.Throw<ArgumentNullException>(() => manager.SaveFavorites(configPath, null!));
        }

        [Test]
        public void SaveAndLoad_RoundTrip_Succeeds()
        {
            // Arrange
            var configPath = Path.Combine(tempDir, "roundtrip.json");
            var original = new List<string>
            {
                "C:/prompts/PROMPT-1.0-test.md",
                "C:/prompts/GUIDE-2.0-example.md"
            };

            // Act
            manager.SaveFavorites(configPath, original);
            var loaded = manager.LoadFavorites(configPath);

            // Assert
            loaded.ShouldNotBeNull();
            loaded.Count.ShouldBe(2);
            loaded[0].ShouldBe(original[0]);
            loaded[1].ShouldBe(original[1]);
        }

        [Test]
        public void SaveFavorites_CreatesDirectoryIfMissing()
        {
            // Arrange
            var subDir = Path.Combine(tempDir, "nested", "folders");
            var configPath = Path.Combine(subDir, "favorites.json");
            var favorites = new List<string> { "test.md" };

            // Act
            manager.SaveFavorites(configPath, favorites);

            // Assert
            Directory.Exists(subDir).ShouldBeTrue();
            File.Exists(configPath).ShouldBeTrue();
        }
    }
}
