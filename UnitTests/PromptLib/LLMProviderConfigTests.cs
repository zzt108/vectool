using NUnit.Framework;
using Shouldly;
using VecTool.Core.AI;

namespace VecTool.UnitTests;

[TestFixture]
public class LLMProviderConfigTests
{
    private string tempConfigPath = null!;

    [SetUp]
    public void Setup()
    {
        tempConfigPath = Path.Combine(Path.GetTempPath(), $"llm-test-{Guid.NewGuid()}.json");
    }

    [TearDown]
    public void Cleanup()
    {
        if (File.Exists(tempConfigPath))
            File.Delete(tempConfigPath);
    }

    [Test]
    public void Load_ShouldThrowWhenPathIsNull()
    {
        // Arrange & Act
        Action act = () => LLMProviderConfig.Load(null!);

        // Assert
        act.ShouldThrow<ArgumentException>();
    }

    [Test]
    public void Load_ShouldThrowWhenFileDoesNotExist()
    {
        // Arrange & Act
        Action act = () => LLMProviderConfig.Load("nonexistent.json");

        // Assert
        act.ShouldThrow<FileNotFoundException>();
    }

    [Test]
    public void Load_ShouldThrowOnInvalidJson()
    {
        // Arrange
        File.WriteAllText(tempConfigPath, "{ invalid json ]");

        // Act
        Action act = () => LLMProviderConfig.Load(tempConfigPath);

        // Assert
        act.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain("JSON");
    }

    [Test]
    public void Load_ShouldDeserializeValidJson()
    {
        // Arrange
        var json = @"
{
    ""defaultProvider"": ""perplexity"",
    ""providers"": {
        ""perplexity"": {
            ""enabled"": true,
            ""apiKey"": ""test-key-123"",
            ""model"": ""pplx-7b-online"",
            ""timeout"": 30
        }
    },
    ""features"": {
        ""autoCategorizationOnImport"": true,
        ""generateCommitMessages"": false,
        ""maxTokensPerRequest"": 500
    }
}";
        File.WriteAllText(tempConfigPath, json);

        // Act
        var config = LLMProviderConfig.Load(tempConfigPath);

        // Assert
        config.DefaultProvider.ShouldBe("perplexity");
        config.Providers.ShouldContainKey("perplexity");
        config.Providers["perplexity"].Enabled.ShouldBeTrue();
        config.Providers["perplexity"].ApiKey.ShouldBe("test-key-123");
        config.Providers["perplexity"].Model.ShouldBe("pplx-7b-online");
        config.Providers["perplexity"].Timeout.ShouldBe(30);
        config.Features.AutoCategorizationOnImport.ShouldBeTrue();
        config.Features.GenerateCommitMessages.ShouldBeFalse();
        config.Features.MaxTokensPerRequest.ShouldBe(500);
    }

    [Test]
    public void Load_ShouldSubstituteEnvironmentVariables()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TEST_API_KEY", "secret-from-env");
        var json = @"
{
    ""defaultProvider"": ""test"",
    ""providers"": {
        ""test"": {
            ""enabled"": true,
            ""apiKey"": ""${TEST_API_KEY}"",
            ""model"": ""test-model"",
            ""timeout"": 10
        }
    }
}";
        File.WriteAllText(tempConfigPath, json);

        // Act
        var config = LLMProviderConfig.Load(tempConfigPath);

        // Assert
        config.Providers["test"].ApiKey.ShouldBe("secret-from-env");

        // Cleanup
        Environment.SetEnvironmentVariable("TEST_API_KEY", null);
    }

    [Test]
    public void Load_ShouldKeepPatternWhenEnvVarUndefined()
    {
        // Arrange
        var json = @"
{
    ""defaultProvider"": ""test"",
    ""providers"": {
        ""test"": {
            ""enabled"": true,
            ""apiKey"": ""${UNDEFINED_VAR}"",
            ""model"": ""test-model"",
            ""timeout"": 10
        }
    }
}";
        File.WriteAllText(tempConfigPath, json);

        // Act
        var config = LLMProviderConfig.Load(tempConfigPath);

        // Assert
        config.Providers["test"].ApiKey.ShouldBe("${UNDEFINED_VAR}");
    }
}
