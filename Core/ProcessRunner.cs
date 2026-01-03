// File: Core/ProcessRunner.cs

using LogCtxShared;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger logger;

        public ProcessRunner(ILogger logger)
        {
            this.logger = logger;
        }

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

            try
            {
                logger?.LogDebug($"Starting process: FileName='{fileName}', Args='{arguments}', WorkDir='{workingDirectory}'");

                try
                {
                    process.Start(); // Line 47
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    logger?.LogError(ex, $"Failed to start process '{fileName}'. LogError code: {ex.NativeErrorCode}. " +
                                   $"Possible causes: (1) File not found in PATH, (2) Invalid executable, (3) Permissions issue.");
                    throw; // Re-throw with logged context
                }

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
            catch (Exception ex)
            {
                using var _ = logger.SetContext(new Props()
                    .Add("FileName", fileName)
                    .Add("Arguments", arguments)
                    .Add("WorkingDirectory", workingDirectory)
                    //.Add("ExitCode", process.ExitCode)
                    .Add("Duration", (DateTime.UtcNow - startedAt).TotalMilliseconds));
                logger?.LogError(ex, $"ProcessRunner.RunAsync failed. Command: '{fileName}', Args: '{arguments}', WorkDir: '{workingDirectory}'");
                throw; // Re-throw with logged context
            }
            finally
            {
            }
        }
    }
}