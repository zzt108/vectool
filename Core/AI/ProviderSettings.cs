#nullable enable
using System;

namespace VecTool.Core.AI;


/// <summary>
/// Provider-specific settings (API key, model, timeout).
/// </summary>
public sealed class ProviderSettings
{
    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Timeout { get; set; } = 30; // seconds
}
