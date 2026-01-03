#nullable enable

using LogCtxShared;
using Microsoft.Extensions.Logging;
using VecTool.Core.AI.Providers;

namespace VecTool.Core.AI
{
    /// <summary>
    /// Factory for creating ILlmProvider instances based on configuration.
    /// </summary>
    public static class LlmProviderFactory
    {
        private static readonly ILogger<PerplexityProvider> logger;

        /// <summary>
        /// Create an LLM provider instance based on configuration.
        /// </summary>
        /// <param name="config">LLM provider configuration.</param>
        /// <returns>Configured provider instance.</returns>
        /// <exception cref="ArgumentNullException">If config is null.</exception>
        /// <exception cref="InvalidOperationException">If provider is disabled or unknown.</exception>
        public static ILlmProvider Create(LLMProviderConfig config)
        {
            if (config == null)
            {
                var ex = new ArgumentNullException(nameof(config));
                logger.LogError(ex, "LLM provider config is null");
                throw ex;
            }

            using var ctx = logger.SetContext(new Props()
                .Add("defaultProvider", config.DefaultProvider));

            var providerKey = config.DefaultProvider?.ToLowerInvariant() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(providerKey))
            {
                var ex = new InvalidOperationException("Default provider not specified in config");
                logger.LogError(ex, "Default provider is null or empty");
                throw ex;
            }

            if (!config.Providers.ContainsKey(providerKey))
            {
                var ex = new InvalidOperationException($"Provider '{providerKey}' not found in config");
                logger.LogError(ex, $"Provider key '{providerKey}' missing from Providers dictionary");
                throw ex;
            }

            var providerSettings = config.Providers[providerKey];

            if (!providerSettings.Enabled)
            {
                var ex = new InvalidOperationException($"Provider '{providerKey}' is disabled in config");
                logger.LogError(ex, $"Provider '{providerKey}' has Enabled=false");
                throw ex;
            }

            ILlmProvider provider = providerKey switch
            {
                "perplexity" => new PerplexityProvider(logger, providerSettings),
                "openai" => new OpenAIProvider(providerSettings), // Stub
                _ => throw new InvalidOperationException($"Unknown provider type: {providerKey}")
            };

            logger.LogInformation($"Created LLM provider: {provider.GetProviderName()}");
            return provider;
        }
    }
}