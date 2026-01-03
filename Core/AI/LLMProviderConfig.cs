#nullable enable

using LogCtxShared;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace VecTool.Core.AI;

/// <summary>
/// LLM provider configuration loaded from external JSON with environment variable substitution.
/// Supports pattern: ${VAR_NAME} → Environment.GetEnvironmentVariable("VAR_NAME").
/// </summary>
public sealed class LLMProviderConfig
{
    private static readonly ILogger logger =
        LoggerFactory.Create(b => b.AddNLog()).CreateLogger<LLMProviderConfig>();

    private static readonly Regex EnvVarPattern = new(@"\$\{([A-Z_][A-Z0-9_]*)\}", RegexOptions.Compiled);

    public string DefaultProvider { get; set; } = "perplexity";
    public Dictionary<string, ProviderSettings> Providers { get; set; } = new();
    public FeatureFlags Features { get; set; } = new();

    /// <summary>
    /// Load configuration from JSON file with environment variable substitution.
    /// </summary>
    public static LLMProviderConfig Load(string configPath)
    {
        using var ctx = logger.SetContext(new Props().Add("configPath", configPath));

        if (string.IsNullOrWhiteSpace(configPath))
        {
            var ex = new ArgumentException("Config path is required.", nameof(configPath));
            logger.LogError(ex, "LLM config path is null or empty");
            throw ex;
        }

        if (!File.Exists(configPath))
        {
            var ex = new FileNotFoundException($"LLM config file not found: {configPath}");
            logger.LogError(ex, "LLM config file missing");
            throw ex;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<LLMProviderConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            if (config == null)
            {
                var ex = new InvalidOperationException("Failed to deserialize LLM config (null result)");
                logger.LogError(ex, "JSON deserialization returned null");
                throw ex;
            }

            // Substitute environment variables in all string properties
            config.SubstituteEnvironmentVariables();

            logger.LogInformation($"LLM config loaded: provider={config.DefaultProvider}, providers={config.Providers.Count}");
            return config;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, $"Invalid JSON in LLM config file: {ex.Message}");
            throw new InvalidOperationException($"Failed to parse LLM config JSON: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Unexpected error loading LLM config: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Recursively substitute ${VAR_NAME} patterns with environment variable values.
    /// </summary>
    private void SubstituteEnvironmentVariables()
    {
        foreach (var provider in Providers.Values)
        {
            provider.ApiKey = ResolveEnvVar(provider.ApiKey);
            provider.Model = ResolveEnvVar(provider.Model);
        }
    }

    /// <summary>
    /// Resolve ${VAR_NAME} pattern to environment variable value.
    /// Returns original string if pattern not found or variable undefined.
    /// </summary>
    private static string ResolveEnvVar(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input ?? string.Empty;

        return EnvVarPattern.Replace(input, match =>
        {
            var varName = match.Groups[1].Value;
            var value = Environment.GetEnvironmentVariable(varName);

            if (string.IsNullOrEmpty(value))
            {
                logger.LogWarning($"Environment variable not found: {varName} (keeping original pattern)");
                return match.Value; // Keep ${VAR_NAME} if undefined
            }

            logger.LogDebug($"Resolved env var: {varName} → [REDACTED]");
            return value;
        });
    }
}

/// <summary>
/// Feature flags for AI-assisted workflows.
/// </summary>
public sealed class FeatureFlags
{
    public bool AutoCategorizationOnImport { get; set; }
    public bool GenerateCommitMessages { get; set; }
    public int MaxTokensPerRequest { get; set; } = 500;
}