// ✅ FULL FILE VERSION
// File: Core/ProcessRunner.cs

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using VecTool.Core.Abstractions;

namespace VecTool.Core
{
    /// <summary>
    /// Default implementation of IProcessRunner for launching external tools (e.g., dotnet).
    /// </summary>
    public sealed class ProcessRunner : IProcessRunner
    {
        public async Task<ProcessResult> RunAsync(string fileName, string arguments, string? workingDirectory, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Executable file name is required.", nameof(fileName));

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                    ? Environment.CurrentDirectory
                    : workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = false
            };

            var startedAt = DateTime.UtcNow;

            if (!process.Start())
                throw new InvalidOperationException("Failed to start process.");

            // Start reading before waiting to avoid deadlocks
            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = stdout,
                StandardError = stderr,
                Duration = DateTime.UtcNow - startedAt
            };
        }
    }
}
