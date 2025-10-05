// ✅ FULL FILE VERSION
// File: Core/Abstractions/IProcessRunner.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VecTool.Core.Abstractions
{
    /// <summary>
    /// Abstraction for launching and managing external processes (e.g., dotnet, git).
    /// </summary>
    public interface IProcessRunner
    {
        /// <summary>
        /// Executes an external process asynchronously and returns the result.
        /// </summary>
        /// <param name="fileName">Executable to run (e.g., "dotnet", "git").</param>
        /// <param name="arguments">Command-line arguments for the executable.</param>
        /// <param name="workingDirectory">Optional working directory for the process.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<ProcessResult> RunAsync(
            string fileName,
            string arguments,
            string? workingDirectory = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of an external process execution.
    /// </summary>
    public sealed class ProcessResult
    {
        /// <summary>
        /// Process exit code. Zero typically indicates success.
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Entire standard output captured from the process.
        /// </summary>
        public string StandardOutput { get; set; } = string.Empty;

        /// <summary>
        /// Entire standard error captured from the process.
        /// </summary>
        public string StandardError { get; set; } = string.Empty;

        /// <summary>
        /// Elapsed time from start to process exit.
        /// </summary>
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Convenience constructor used by existing tests; Duration defaults to zero.
        /// </summary>
        public ProcessResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput ?? string.Empty;
            StandardError = standardError ?? string.Empty;
            Duration = TimeSpan.Zero;
        }

        /// <summary>
        /// Parameterless constructor for property-initializer usage in production paths.
        /// </summary>
        public ProcessResult()
        {
        }
    }
}
