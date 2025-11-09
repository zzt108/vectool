using NUnit.Framework;
using Shouldly;
using System;
using VecTool.Core.Models;

namespace VecTool.UnitTests;

[TestFixture]
public class PromptsConfigTests
{
    [Test]
    public void Constructor_ShouldValidateRepositoryPath()
    {
        // Arrange & Act
        Action act = () => new PromptsConfig("", ".md", "config.json", "fav.json");

        // Assert
        act.ShouldThrow<ArgumentException>()
            .Message.ShouldContain("RepositoryPath");
    }

    [Test]
    public void Constructor_ShouldValidateFileExtensions()
    {
        // Arrange & Act
        Action act = () => new PromptsConfig("C:\\prompts", "", "config.json", "fav.json");

        // Assert
        act.ShouldThrow<ArgumentException>()
            .Message.ShouldContain("FileExtensions");
    }

    [Test]
    public void Constructor_ShouldValidateLLMConfigPath()
    {
        // Arrange & Act
        Action act = () => new PromptsConfig("C:\\prompts", ".md", "", "fav.json");

        // Assert
        act.ShouldThrow<ArgumentException>()
            .Message.ShouldContain("LLMConfigPath");
    }

    [Test]
    public void Constructor_ShouldValidateFavoritesPath()
    {
        // Arrange & Act
        Action act = () => new PromptsConfig("C:\\prompts", ".md", "config.json", "");

        // Assert
        act.ShouldThrow<ArgumentException>()
            .Message.ShouldContain("FavoritesConfigPath");
    }

    [Test]
    public void Constructor_ShouldAcceptValidInputs()
    {
        // Arrange & Act
        var config = new PromptsConfig("C:\\prompts", ".md,.txt", "config.json", "fav.json");

        // Assert
        config.RepositoryPath.ShouldBe("C:\\prompts");
        config.FileExtensions.ShouldBe(".md,.txt");
        config.LLMConfigPath.ShouldBe("config.json");
        config.FavoritesConfigPath.ShouldBe("fav.json");
    }

    [Test]
    public void FromAppConfig_ShouldThrowWhenRepositoryPathMissing()
    {
        // Arrange
        var reader = new InMemoryAppSettingsReader();

        // Act
        Action act = () => PromptsConfig.FromAppConfig(reader);

        // Assert
        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("promptsRepositoryPath");
    }

    [Test]
    public void FromAppConfig_ShouldThrowWhenLLMConfigPathMissing()
    {
        // Arrange
        var reader = new InMemoryAppSettingsReader();
        reader.Set("promptsRepositoryPath", "C:\\prompts");

        // Act
        Action act = () => PromptsConfig.FromAppConfig(reader);

        // Assert
        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("llmProviderConfig");
    }

    [Test]
    public void FromAppConfig_ShouldLoadValidConfiguration()
    {
        // Arrange
        var reader = new InMemoryAppSettingsReader();
        reader.Set("promptsRepositoryPath", "C:\\prompts");
        reader.Set("promptsFileExtensions", ".md,.yaml");
        reader.Set("llmProviderConfig", "C:\\prompts\\config.json");
        reader.Set("favoritesConfigPath", "C:\\prompts\\.fav.json");

        // Act
        var config = PromptsConfig.FromAppConfig(reader);

        // Assert
        config.RepositoryPath.ShouldBe("C:\\prompts");
        config.FileExtensions.ShouldBe(".md,.yaml");
        config.LLMConfigPath.ShouldBe("C:\\prompts\\config.json");
        config.FavoritesConfigPath.ShouldBe("C:\\prompts\\.fav.json");
    }
}

// Helper for in-memory testing
internal sealed class InMemoryAppSettingsReader : IAppSettingsReader
{
    private readonly Dictionary<string, string?> settings = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string key, string? value) => settings[key] = value;

    public string? Get(string key) => settings.TryGetValue(key, out var value) ? value : null;
}
