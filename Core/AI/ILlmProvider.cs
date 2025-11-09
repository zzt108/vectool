// ✅ FULL FILE VERSION
#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace VecTool.Core.AI
{
    /// <summary>
    /// Simple interface for LLM provider abstraction.
    /// KISS: No streaming, no retry logic, single model per provider.
    /// </summary>
    public interface ILlmProvider
    {
        /// <summary>
        /// Send a prompt to the LLM and return the response text.
        /// </summary>
        /// <param name="prompt">User prompt text.</param>
        /// <param name="ct">Cancellation token for timeout/cancellation.</param>
        /// <returns>LLM response text.</returns>
        Task<string> RequestAsync(string prompt, CancellationToken ct = default);

        /// <summary>
        /// Validate provider configuration (API key, model, connectivity).
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if configuration is valid and provider is reachable.</returns>
        Task<bool> ValidateConfigAsync(CancellationToken ct = default);

        /// <summary>
        /// Get provider name for logging and diagnostics.
        /// </summary>
        /// <returns>Provider name (e.g., "Perplexity", "OpenAI").</returns>
        string GetProviderName();
    }
}
