// ✅ FULL FILE VERSION
using System;
using System.Linq;
using System.Text.RegularExpressions;
using NLogShared;

namespace VecTool.Configuration
{
    /// <summary>
    /// Filtering operations for VectorStoreConfig (exclusion rules).
    /// </summary>
    public partial class VectorStoreConfig
    {
        /// <summary>
        /// Check if a file should be excluded based on patterns.
        /// </summary>
        public bool IsFileExcluded(string fileName)
        {
            foreach (var pattern in ExcludedFiles)
            {
                string regexPattern = Regex.Escape(pattern).Replace("\\*", ".*");
                if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
                {
                    log.Trace($"File {fileName} excluded by pattern {pattern}");
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if a folder should be excluded.
        /// </summary>
        public bool IsFolderExcluded(string folderName)
        {
            bool isExcluded = ExcludedFolders.Contains(folderName);
            if (isExcluded)
            {
                log.Trace($"Folder {folderName} is in excluded list");
            }
            return isExcluded;
        }
    }
}
