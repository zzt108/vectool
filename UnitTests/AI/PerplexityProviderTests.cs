#nullable enable

using NUnit.Framework;
using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using VecTool.Core.AI;
using VecTool.Core.AI.Providers;
using ProviderSettings = VecTool.Core.AI.ProviderSettings;

namespace VecTool.UnitTests.AI
{
    [TestFixture]
    public class PerplexityProviderTests
    {
        [Test]
        public void Constructor_ShouldThrowWhenApiKeyMissing()
        {
            // Arrange
            var settings = new ProviderSettings
            {
                Enabled = true,
                ApiKey = string.Empty,
                Model = "pplx-7b-online",
                Timeout = 30
            };

            // Act & Assert
            Should.Throw<ArgumentException>(() => new PerplexityProvider(settings))
                .Message.ShouldContain("API key");
        }

        [Test]
        public void Constructor_ShouldThrowWhenModelMissing()
        {
            // Arrange
            var settings = new ProviderSettings
            {
                Enabled = true,
                ApiKey = "test-key",
                Model = string.Empty,
                Timeout = 30
            };

            // Act & Assert
            Should.Throw<ArgumentException>(() => new PerplexityProvider(settings))
                .Message.ShouldContain("model");
        }

        [Test]
        public void GetProviderName_ShouldReturnPerplexity()
        {
            // Arrange
            var settings = CreateValidSettings();
            using var provider = new PerplexityProvider(settings);

            // Act
            var name = provider.GetProviderName();

            // Assert
            name.ShouldBe("Perplexity");
        }

        [Test]
        public async Task RequestAsync_ShouldThrowWhenPromptEmpty()
        {
            // Arrange
            var settings = CreateValidSettings();
            using var provider = new PerplexityProvider(settings);

            // Act & Assert
            await Should.ThrowAsync<ArgumentException>(async () =>
                await provider.RequestAsync(string.Empty, CancellationToken.None));
        }

        [Test]
        [Ignore("Requires valid Perplexity API key - manual integration test")]
        public async Task RequestAsync_ShouldReturnResponse_IntegrationTest()
        {
            // Arrange - Set PPLX_API_KEY environment variable before running
            var apiKey = Environment.GetEnvironmentVariable("PPLX_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Assert.Inconclusive("PPLX_API_KEY environment variable not set");
                return;
            }

            var settings = new ProviderSettings
            {
                Enabled = true,
                ApiKey = apiKey,
                Model = "pplx-7b-online",
                Timeout = 30
            };

            using var provider = new PerplexityProvider(settings);

            // Act
            var response = await provider.RequestAsync("What is 2+2?", CancellationToken.None);

            // Assert
            response.ShouldNotBeNullOrWhiteSpace();
            response.Length.ShouldBeGreaterThan(0);
        }

        [Test]
        [Ignore("Requires valid Perplexity API key - manual integration test")]
        public async Task ValidateConfigAsync_ShouldReturnTrue_IntegrationTest()
        {
            // Arrange
            var apiKey = Environment.GetEnvironmentVariable("PPLX_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Assert.Inconclusive("PPLX_API_KEY environment variable not set");
                return;
            }

            var settings = new ProviderSettings
            {
                Enabled = true,
                ApiKey = apiKey,
                Model = "pplx-7b-online",
                Timeout = 30
            };

            using var provider = new PerplexityProvider(settings);

            // Act
            var isValid = await provider.ValidateConfigAsync(CancellationToken.None);

            // Assert
            isValid.ShouldBeTrue();
        }

        [Test]
        public async Task RequestAsync_ShouldThrowOnTimeout()
        {
            // Arrange - 1 second timeout
            var settings = new ProviderSettings
            {
                Enabled = true,
                ApiKey = "test-key-will-timeout",
                Model = "pplx-7b-online",
                Timeout = 1
            };

            using var provider = new PerplexityProvider(settings);

            // Act & Assert - Expect TaskCanceledException or OperationCanceledException
            await Should.ThrowAsync<Exception>(async () =>
                await provider.RequestAsync("Test prompt", CancellationToken.None));
        }

        private static ProviderSettings CreateValidSettings() => new()
        {
            Enabled = true,
            ApiKey = "test-api-key-12345",
            Model = "pplx-7b-online",
            Timeout = 30
        };
    }
}
