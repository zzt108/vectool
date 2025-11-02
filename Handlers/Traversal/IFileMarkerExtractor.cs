namespace VecTool.Handlers.Traversal
{
    /// <summary>
    /// Extracts file-level exclusion markers from file headers.
    /// Language-agnostic implementation supporting comment syntax of any language.
    /// </summary>
    public interface IFileMarkerExtractor
    {
        /// <summary>
        /// Extracts [VECTOOL:EXCLUDE:...] marker from file header (first 1500 bytes, first 50 lines).
        /// </summary>
        /// <param name="filePath">Full path to file to analyze</param>
        /// <returns>FileMarkerPattern if marker found; null otherwise</returns>
        /// <remarks>
        /// - Returns null if file doesn't exist (no exception)
        /// - Returns null if file read fails (logs warning)
        /// - Returns null if no marker in first 50 lines
        /// - Thread-safe when called concurrently
        /// - Regex timeout: 100ms (prevents catastrophic backtracking)
        /// </remarks>
        FileMarkerPattern? ExtractMarker(string filePath);
    }
}
