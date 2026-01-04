// ✅ FULL FILE VERSION
// File: Core/Abstractions/IGitRunner.cs
namespace VecTool.Core.Abstractions
{
    /// <summary>
    /// Abstraction for reading Git repository information in a side-effect free manner. 
    /// </summary>
    public interface IGitRunner
    {
        /// <summary>
        /// Returns the current branch name from the configured repository context or "unknown" on errors. 
        /// </summary>
        //Task<string> GetCurrentBranchAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the current branch name for the provided working directory or "unknown" on errors. 
        /// </summary>
        Task<string> GetCurrentBranchAsync(string workingDirectory, CancellationToken cancellationToken = default);
    }
}
