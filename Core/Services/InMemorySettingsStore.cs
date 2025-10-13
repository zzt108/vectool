#nullable enable
namespace VecTool.Services
{
    /// <summary>
    /// In-memory implementation of ISettingsStore for testing and temporary state.
    /// </summary>
    public sealed class InMemorySettingsStore : ISettingsStore
    {
        private readonly Dictionary<string, string> store = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly object syncLock = new object();

        public string? Get(string key)
        {
            lock (syncLock)
            {
                return store.TryGetValue(key, out var value) ? value : null;
            }
        }

        public void Set(string key, string? value)
        {
            lock (syncLock)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    store.Remove(key);
                }
                else
                {
                    store[key] = value;
                }
            }
        }
    }
}
