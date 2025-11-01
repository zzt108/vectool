// ✅ FULL FILE VERSION - CORRECTED

namespace VecTool.Handlers.Traversal
{
    using System;
    using VecTool.Configuration.Exclusion;
    /// <summary>
    /// ✅ NEW: Fallback matcher used when Layer 1 (pattern matching) fails to initialize.
    /// Returns false for all queries - only Layer 2 (legacy config) filtering applies.
    /// Ensures degraded but functional file filtering.
    /// </summary>
    internal sealed class LegacyOnlyIgnoreMatcher : IIgnorePatternMatcher
    {
        private bool disposedValue;

        /// <summary>
        /// Always returns false - defers to legacy config filtering (Layer 2).
        /// </summary>
        public bool IsIgnored(string path, bool isDirectory)
        {
            // ✅ Fallback: don't filter by pattern, let legacy config handle it
            return false;
        }

        /// <summary>
        /// Adds pattern - no-op for fallback matcher.
        /// </summary>
        public void AddPattern(string pattern)
        {
            // No-op - fallback only
        }

        /// <summary>
        /// Clears patterns - no-op for fallback matcher.
        /// </summary>
        public void ClearPatterns()
        {
            // No-op - fallback only
        }

        public void LoadFromRoot(string rootPath)
        {
            throw new NotImplementedException();
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~LegacyOnlyIgnoreMatcher()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}