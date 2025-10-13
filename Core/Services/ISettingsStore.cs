#nullable enable
namespace VecTool.Services
{
    /// <summary>
    /// Abstraction for key-value settings persistence.
    /// Used by UiStateConfig and testable implementations.
    /// </summary>
    public interface ISettingsStore
    {
        /// <summary>
        /// Gets the value associated with the specified key, or null if not found.
        /// </summary>
        string? Get(string key);

        /// <summary>
        /// Sets or clears (if value is null) the value for the specified key.
        /// </summary>
        void Set(string key, string? value);
    }
}
