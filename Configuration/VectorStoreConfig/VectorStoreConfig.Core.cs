using System;
using System.Collections.Generic;
using System.Linq;

namespace VecTool.Configuration
{
    /// <summary>
    /// Core properties and basic operations for VectorStoreConfig.
    /// </summary>
    public partial class VectorStoreConfig : IVectorStoreConfig
    {
        public List<string> FolderPaths { get; set; } = new List<string>();
        public List<string> ExcludedFiles { get; set; } = new List<string>();
        public List<string> ExcludedFolders { get; set; } = new List<string>();

        /// <summary>
        /// Add a folder path if it doesn't exist.
        /// </summary>
        public bool AddFolderPath(string folderPath)
        {
            if (!FolderPaths.Contains(folderPath))
            {
                FolderPaths.Add(folderPath);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove a folder path.
        /// </summary>
        public bool RemoveFolderPath(string folderPath)
        {
            return FolderPaths.Remove(folderPath);
        }

        /// <summary>
        /// Clear all folder paths.
        /// </summary>
        public void ClearFolderPaths()
        {
            FolderPaths.Clear();
        }

        /// <summary>
        /// Create a deep copy of this configuration.
        /// </summary>
        public VectorStoreConfig Clone()
        {
            return new VectorStoreConfig
            {
                FolderPaths = new List<string>(FolderPaths),
                ExcludedFiles = new List<string>(ExcludedFiles),
                ExcludedFolders = new List<string>(ExcludedFolders)
            };
        }
    }
}
