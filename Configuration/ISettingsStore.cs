// Path: Configuration/UiStateConfig.cs
// NOTE: Consolidated legacy ISettingsStore-backed getters/setters with the JSON-backed UI state.
// Imports
namespace VecTool.Configuration
{
    /// <summary>
    /// Abstraction to read/write application settings for testability.
    /// </summary>
    public interface ISettingsStore
    {
        void Set(string key, string? value);
        string? Get(string key);
    }
}

