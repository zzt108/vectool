#nullable enable

using Microsoft.Extensions.Logging;
using VecTool.Configuration.Logging;

namespace VecTool.Core.AI.Providers
{
    /// <summary>
    /// OpenAI provider stub (not implemented in Phase 4.6.1.3).
    /// </summary>
    public sealed class OpenAIProvider : ILlmProvider
    {
        private static readonly ILogger logger = AppLogger.For<OpenAIProvider>();

        public OpenAIProvider(ProviderSettings settings)
        {
            logger.LogWarning("OpenAIProvider is a stub and not yet implemented");
        }

        public string GetProviderName() => "OpenAI (Stub)";

        public Task<string> RequestAsync(string prompt, CancellationToken ct = default)
        {
            var ex = new NotImplementedException("OpenAI provider not yet implemented");
            logger.LogError(ex, "OpenAI RequestAsync called but not implemented");
            throw ex;
        }

        public Task<bool> ValidateConfigAsync(CancellationToken ct = default)
        {
            logger.LogWarning("OpenAI ValidateConfigAsync called (stub)");
            return Task.FromResult(false);
        }
    }
}