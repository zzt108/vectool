// File: Core/Abstractions/IProcessRunner.cs

using LogCtxShared;
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
        /// Runs a process with the given filename and arguments and returns execution details.
        /// </summary>
        /// <param name="fileName">Executable to run (e.g., "dotnet").</param>
        /// <param name="arguments">Command-line arguments.</param>
        /// <param name="workingDirectory">Optional working directory; null uses current directory.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>ProcessResult with exit code, captured output, and duration.</returns>
        Task<ProcessResult> RunAsync(string fileName, string arguments, string? workingDirectory, CancellationToken ct);
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
        /// Convenience constructor used by tests or simple call sites.
        /// </summary>
        public ProcessResult(int exitCode, string standardOutput, string standardError)
        {
            ExitCode = exitCode;
            StandardOutput = standardOutput ?? string.Empty;
            StandardError = standardError ?? string.Empty;
        }

        /// <summary>
        /// Parameterless constructor for property-initializer usage.
        /// </summary>
        public ProcessResult()
        {
        }
    }
}