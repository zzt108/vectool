#nullable enable

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LogCtxShared;
using Microsoft.Extensions.Logging;
using VecTool.Configuration.Helpers;

namespace VecTool.Core.AI.Providers
{
    /// <summary>
    /// Perplexity API provider implementation.
    /// Endpoint: https://api.perplexity.ai/chat/completions
    /// </summary>
    public sealed class PerplexityProvider : ILlmProvider, IDisposable
    {
        private readonly ILogger logger;
        private readonly HttpClient httpClient;
        private readonly string apiKey;
        private readonly string model;
        private readonly int timeoutSeconds;
        private const string ApiBaseUrl = "https://api.perplexity.ai/chat/completions";

        public PerplexityProvider(ILogger logger, ProviderSettings settings)
        {
            this.logger = logger.ThrowIfNull(nameof(logger), logger);

            settings.ThrowIfNull(nameof(settings), logger);

            apiKey = settings.ApiKey.ThrowIfNullOrWhiteSpace(nameof(settings.ApiKey), logger, "Perplexity API key is required");
            model = settings.Model.ThrowIfNullOrWhiteSpace(nameof(settings.Model), logger, "Perplexity model is required");

            timeoutSeconds = settings.Timeout > 0 ? settings.Timeout : 30;

            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            logger.LogDebug($"PerplexityProvider initialized: model={model}, timeout={timeoutSeconds}s");
        }

        public string GetProviderName() => "Perplexity";

        public async Task<string> RequestAsync(string prompt, CancellationToken ct = default)
        {
            using var ctx = logger.SetContext(new Props()
                .Add("provider", "Perplexity")
                .Add("model", model)
                .Add("promptLength", prompt?.Length ?? 0));

            if (string.IsNullOrWhiteSpace(prompt))
            {
                var ex = new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
                logger.LogError(ex, "Empty prompt submitted");
                throw ex;
            }

            try
            {
                var requestBody = new
                {
                    model = model,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                logger.LogDebug($"Sending request to Perplexity API: {ApiBaseUrl}");

                var response = await httpClient.PostAsync(ApiBaseUrl, content, ct).ConfigureAwait(false);

                var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                using var responseCtx = logger.SetContext(new Props()
                    .Add("statusCode", (int)response.StatusCode)
                    .Add("responseLength", responseBody?.Length ?? 0));

                if (!response.IsSuccessStatusCode)
                {
                    var ex = new HttpRequestException(
                        $"Perplexity API request failed: {response.StatusCode} - {responseBody}");
                    logger.LogError(ex, "API request failed");
                    throw ex;
                }

                var result = JsonSerializer.Deserialize<PerplexityResponse>(responseBody);

                if (result?.Choices == null || result.Choices.Length == 0)
                {
                    var ex = new InvalidOperationException("Perplexity API returned empty response");
                    logger.LogError(ex, "Empty API response");
                    throw ex;
                }

                var responseText = result.Choices[0].Message?.Content ?? string.Empty;

                logger.LogInformation($"Perplexity request succeeded: {responseText.Length} chars");
                return responseText;
            }
            catch (OperationCanceledException ex)
            {
                logger.LogError(ex, "Perplexity request cancelled or timed out");
                throw;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "HTTP error during Perplexity request");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during Perplexity request");
                throw;
            }
        }

        public async Task<bool> ValidateConfigAsync(CancellationToken ct = default)
        {
            using var ctx = logger.SetContext(new Props()
                .Add("provider", "Perplexity")
                .Add("operation", "ValidateConfig"));

            try
            {
                // Simple validation: send minimal request
                var testPrompt = "Test";
                await RequestAsync(testPrompt, ct).ConfigureAwait(false);
                logger.LogInformation("Perplexity config validation succeeded");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Perplexity config validation failed");
                return false;
            }
        }

        public void Dispose()
        {
            httpClient?.Dispose();
            logger.LogDebug("PerplexityProvider disposed");
        }

        // Response models for JSON deserialization
        private class PerplexityResponse
        {
            public Choice[]? Choices { get; set; }
        }

        private class Choice
        {
            public Message? Message { get; set; }
        }

        private class Message
        {
            public string? Content { get; set; }
        }
    }
}