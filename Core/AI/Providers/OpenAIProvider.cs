#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using LogCtxShared;
using NLogShared;

namespace VecTool.Core.AI.Providers
{
    /// <summary>
    /// OpenAI provider stub (not implemented in Phase 4.6.1.3).
    /// </summary>
    public sealed class OpenAIProvider : ILlmProvider
    {
        private static readonly CtxLogger log = new();

        public OpenAIProvider(ProviderSettings settings)
        {
            log.Warn("OpenAIProvider is a stub and not yet implemented");
        }

        public string GetProviderName() => "OpenAI (Stub)";

        public Task<string> RequestAsync(string prompt, CancellationToken ct = default)
        {
            var ex = new NotImplementedException("OpenAI provider not yet implemented");
            log.Error(ex, "OpenAI RequestAsync called but not implemented");
            throw ex;
        }

        public Task<bool> ValidateConfigAsync(CancellationToken ct = default)
        {
            log.Warn("OpenAI ValidateConfigAsync called (stub)");
            return Task.FromResult(false);
        }
    }
}
