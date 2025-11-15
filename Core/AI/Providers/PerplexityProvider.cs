#nullable enable

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LogCtxShared;
using NLogShared;

namespace VecTool.Core.AI.Providers
{
    /// <summary>
    /// Perplexity API provider implementation.
    /// Endpoint: https://api.perplexity.ai/chat/completions
    /// </summary>
    public sealed class PerplexityProvider : ILlmProvider, IDisposable
    {
        private static readonly CtxLogger log = new();
        private readonly HttpClient httpClient;
        private readonly string apiKey;
        private readonly string model;
        private readonly int timeoutSeconds;
        private const string ApiBaseUrl = "https://api.perplexity.ai/chat/completions";

        public PerplexityProvider(ProviderSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
                throw new ArgumentException("Perplexity API key is required", nameof(settings));

            if (string.IsNullOrWhiteSpace(settings.Model))
                throw new ArgumentException("Perplexity model is required", nameof(settings));

            apiKey = settings.ApiKey;
            model = settings.Model;
            timeoutSeconds = settings.Timeout > 0 ? settings.Timeout : 30;

            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            log.Debug($"PerplexityProvider initialized: model={model}, timeout={timeoutSeconds}s");
        }

        public string GetProviderName() => "Perplexity";

        public async Task<string> RequestAsync(string prompt, CancellationToken ct = default)
        {
            using var ctx = LogCtx.Set(new Props()
                .Add("provider", "Perplexity")
                .Add("model", model)
                .Add("promptLength", prompt?.Length ?? 0));

            if (string.IsNullOrWhiteSpace(prompt))
            {
                var ex = new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
                log.Error(ex, "Empty prompt submitted");
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

                log.Debug($"Sending request to Perplexity API: {ApiBaseUrl}");

                var response = await httpClient.PostAsync(ApiBaseUrl, content, ct).ConfigureAwait(false);

                var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

                using var responseCtx = LogCtx.Set(new Props()
                    .Add("statusCode", (int)response.StatusCode)
                    .Add("responseLength", responseBody?.Length ?? 0));

                if (!response.IsSuccessStatusCode)
                {
                    var ex = new HttpRequestException(
                        $"Perplexity API request failed: {response.StatusCode} - {responseBody}");
                    log.Error(ex, "API request failed");
                    throw ex;
                }

                var result = JsonSerializer.Deserialize<PerplexityResponse>(responseBody);

                if (result?.Choices == null || result.Choices.Length == 0)
                {
                    var ex = new InvalidOperationException("Perplexity API returned empty response");
                    log.Error(ex, "Empty API response");
                    throw ex;
                }

                var responseText = result.Choices[0].Message?.Content ?? string.Empty;

                log.Info($"Perplexity request succeeded: {responseText.Length} chars");
                return responseText;
            }
            catch (OperationCanceledException ex)
            {
                log.Error(ex, "Perplexity request cancelled or timed out");
                throw;
            }
            catch (HttpRequestException ex)
            {
                log.Error(ex, "HTTP error during Perplexity request");
                throw;
            }
            catch (Exception ex)
            {
                log.Error(ex, "Unexpected error during Perplexity request");
                throw;
            }
        }

        public async Task<bool> ValidateConfigAsync(CancellationToken ct = default)
        {
            using var ctx = LogCtx.Set(new Props()
                .Add("provider", "Perplexity")
                .Add("operation", "ValidateConfig"));

            try
            {
                // Simple validation: send minimal request
                var testPrompt = "Test";
                await RequestAsync(testPrompt, ct).ConfigureAwait(false);
                log.Info("Perplexity config validation succeeded");
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex, "Perplexity config validation failed");
                return false;
            }
        }

        public void Dispose()
        {
            httpClient?.Dispose();
            log.Debug("PerplexityProvider disposed");
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
