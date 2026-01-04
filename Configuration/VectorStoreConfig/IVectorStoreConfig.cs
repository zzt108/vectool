namespace VecTool.Configuration
{
    public interface IVectorStoreConfig
    {
        /// <summary>
        /// Add a folder path if it doesn't exist.
        /// </summary>
        bool AddFolderPath(string folderPath);

        /// <summary>
        /// Clear all folder paths.
        /// </summary>
        void ClearFolderPaths();

        /// <summary>
        /// Create a deep copy of this configuration.
        /// </summary>
        VectorStoreConfig Clone();

        /// <summary>
        /// Check if a file should be excluded based on patterns.
        /// </summary>
        bool IsFileExcluded(string fileName);

        /// <summary>
        /// Check if a folder should be excluded.
        /// </summary>
        bool IsFolderExcluded(string folderName);

        /// <summary>
        /// Load excluded files from app.config.
        /// </summary>
        void LoadExcludedFilesConfig();

        /// <summary>
        /// Load excluded folders from app.config.
        /// </summary>
        void LoadExcludedFoldersConfig();

        /// <summary>
        /// Remove a folder path.
        /// </summary>
        bool RemoveFolderPath(string folderPath);

        /// <summary>
        /// Common root; prefers ancestor containing .git or dot-folders when multiple valid roots exist
        /// </summary>
        /// <returns></returns>
        public string? GetRootPath();

        List<string> ExcludedFiles { get; set; }
        List<string> ExcludedFolders { get; set; }
        List<string> FolderPaths { get; set; }
    }
}