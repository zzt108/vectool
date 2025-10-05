using NLogShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VecTool.Core;

namespace VecTool.Handlers
{
    /// <summary>
    /// Responsibility: Handles recursive discovery of Git repositories.
    /// </summary>
    public partial class GitChangesHandler
    {
        private static readonly CtxLogger _log = new();

        /// <summary>
        /// Finds all nested Git repositories within a root directory, skipping common excluded folders.
        /// This implementation uses an iterative approach to avoid stack overflow exceptions on deep directory trees.
        /// </summary>
        /// <param name="rootDirectory">The starting directory for the search.</param>
        /// <returns>A collection of paths to the Git repositories found.</returns>
        private async Task<IEnumerable<string>> FindGitRepositoriesRecursivelyAsync(string rootDirectory)
        {
            var foundRepositories = new List<string>();
            if (!Directory.Exists(rootDirectory))
            {
                _log.Warn($"Root directory for search does not exist: '{rootDirectory}'");
                return foundRepositories;
            }

            var directoriesToScan = new Stack<string>();
            directoriesToScan.Push(rootDirectory);

            var excludedDirNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bin",
                "obj",
                "node_modules",
                "packages",
                ".vs",
                ".idea"
            };

            while (directoriesToScan.Any())
            {
                var currentDir = directoriesToScan.Pop();

                // If the current directory is a repository, add it and stop traversing deeper.
                // Submodules inside will be handled by the submodule processor.
                if (GitRunner.IsGitRepository(currentDir))
                {
                    foundRepositories.Add(currentDir);
                    continue;
                }

                try
                {
                    foreach (var subDir in Directory.EnumerateDirectories(currentDir))
                    {
                        var dirName = Path.GetFileName(subDir);
                        // Skip hidden folders and commonly excluded ones
                        if (!dirName.StartsWith(".") && !excludedDirNames.Contains(dirName))
                        {
                            directoriesToScan.Push(subDir);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    _log.Trace($"Access denied to directory: '{currentDir}'. Skipping.");
                }
                catch (Exception ex)
                {
                    _log.Error(ex, $"Could not scan directory '{currentDir}' for repositories. It might be a permissions issue.");
                }
            }

            // The operation is I/O-bound but implemented synchronously for simplicity.
            // We return a completed task to match the async signature.
            return await Task.FromResult(foundRepositories);
        }
    }
}
