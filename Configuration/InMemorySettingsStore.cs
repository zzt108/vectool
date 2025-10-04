// File: Configuration/InMemorySettingsStore.cs
// Project: VecTool.Configuration

using System;
using System.Collections.Concurrent;

namespace VecTool.Configuration
{
    /// <summary>
    /// Simple in-memory settings store for tests and in-process usage.
    /// Keys are case-sensitive and null values delete the key.
    /// </summary>
    public sealed class InMemorySettingsStore : ISettingsStore
    {
        private readonly ConcurrentDictionary<string, string?> _values =
            new(StringComparer.Ordinal);

        /// <summary>
        /// Persist a value for the given key; passing null clears the key.
        /// </summary>
        public void Set(string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            if (value is null)
            {
                _values.TryRemove(key, out _);
                return;
            }

            _values[key] = value;
        }

        /// <summary>
        /// Get the value for the given key, or null if missing.
        /// </summary>
        public string? Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            return _values.TryGetValue(key, out var value) ? value : null;
        }
    }
}
