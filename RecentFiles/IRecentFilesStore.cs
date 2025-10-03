namespace VecTool.RecentFiles
{
    using System;

    /// <summary>
    /// Pure text persistence interface for recent files data.
    /// </summary>
    public interface IRecentFilesStore
    {
        /// <summary>
        /// Reads JSON data from storage.
        /// </summary>
        string? Read();

        /// <summary>
        /// Writes JSON data to storage.
        /// </summary>
        void Write(string json);
    }
}
