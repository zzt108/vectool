#nullable enable

using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using VecTool.Core.AI;

namespace VecTool.UnitTests.AI
{
    [TestFixture]
    public class LlmProviderFactoryTests
    {
        [Test]
        public void Create_ShouldThrowWhenConfigNull()
        {
            // Act & Assert
            Should.Throw<ArgumentNullException>(() => LlmProviderFactory.Create(null!));
        }

        [Test]
        public void Create_ShouldThrowWhenDefaultProviderEmpty()
        {
            // Arrange
            var config = new LLMProviderConfig
            {
                DefaultProvider = string.Empty,
                Providers = new Dictionary<string, ProviderSettings>()
            };

            // Act & Assert
            Should.Throw<InvalidOperationException>(() => LlmProviderFactory.Create(config))
                .Message.ShouldContain("Default provider not specified");
        }

        [Test]
        public void Create_ShouldThrowWhenProviderNotFound()
        {
            // Arrange
            var config = new LLMProviderConfig
            {
                DefaultProvider = "nonexistent",
                Providers = new Dictionary<string, ProviderSettings>()
            };

            // Act & Assert
            Should.Throw<InvalidOperationException>(() => LlmProviderFactory.Create(config))
                .Message.ShouldContain("not found");
        }

        [Test]
        public void Create_ShouldThrowWhenProviderDisabled()
        {
            // Arrange
            var config = new LLMProviderConfig
            {
                DefaultProvider = "perplexity",
                Providers = new Dictionary<string, ProviderSettings>
                {
                    ["perplexity"] = new ProviderSettings
                    {
                        Enabled = false,
                        ApiKey = "test",
                        Model = "pplx-7b-online",
                        Timeout = 30
                    }
                }
            };

            // Act & Assert
            Should.Throw<InvalidOperationException>(() => LlmProviderFactory.Create(config))
                .Message.ShouldContain("disabled");
        }

        [Test]
        public void Create_ShouldReturnPerplexityProvider()
        {
            // Arrange
            var config = new LLMProviderConfig
            {
                DefaultProvider = "perplexity",
                Providers = new Dictionary<string, ProviderSettings>
                {
                    ["perplexity"] = new ProviderSettings
                    {
                        Enabled = true,
                        ApiKey = "test-key",
                        Model = "pplx-7b-online",
                        Timeout = 30
                    }
                }
            };

            // Act
            var provider = LlmProviderFactory.Create(config);

            // Assert
            provider.ShouldNotBeNull();
            provider.GetProviderName().ShouldBe("Perplexity");
        }

        [Test]
        public void Create_ShouldReturnOpenAIStub()
        {
            // Arrange
            var config = new LLMProviderConfig
            {
                DefaultProvider = "openai",
                Providers = new Dictionary<string, ProviderSettings>
                {
                    ["openai"] = new ProviderSettings
                    {
                        Enabled = true,
                        ApiKey = "test-key",
                        Model = "gpt-4",
                        Timeout = 30
                    }
                }
            };

            // Act
            var provider = LlmProviderFactory.Create(config);

            // Assert
            provider.ShouldNotBeNull();
            provider.GetProviderName().ShouldContain("OpenAI");
        }

        [Test]
        public void Create_ShouldThrowForUnknownProviderType()
        {
            // Arrange
            var config = new LLMProviderConfig
            {
                DefaultProvider = "anthropic",
                Providers = new Dictionary<string, ProviderSettings>
                {
                    ["anthropic"] = new ProviderSettings
                    {
                        Enabled = true,
                        ApiKey = "test-key",
                        Model = "claude-3",
                        Timeout = 30
                    }
                }
            };

            // Act & Assert
            Should.Throw<InvalidOperationException>(() => LlmProviderFactory.Create(config))
                .Message.ShouldContain("Unknown provider type");
        }
    }
}
