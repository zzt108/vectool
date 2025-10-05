// ✅ FULL FILE VERSION
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VecTool.Core.Abstractions;

namespace VecTool.Core
{
    /// <summary>
    /// Executes external processes and returns a rich <see cref="ProcessResult"/>.
    /// </summary>
    public sealed class ProcessRunner : IProcessRunner
    {
        /// <inheritdoc />
        public async Task<ProcessResult> RunAsync(
            string fileName,
            string arguments,
            string? workingDirectory = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Executable file name is required.", nameof(fileName));
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                    ? Environment.CurrentDirectory
                    : workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = false
            };

            var stopwatch = Stopwatch.StartNew();

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start process.");
            }

            // Read entire streams asynchronously to avoid deadlocks.
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            try
            {
#if NET8_0_OR_GREATER
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
#else
                // Fallback for older frameworks
                while (!process.HasExited)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(25, cancellationToken).ConfigureAwait(false);
                }
#endif
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Ignore kill errors during cancellation path.
                }

                throw;
            }
            finally
            {
                stopwatch.Stop();
            }

            var stdOut = await stdoutTask.ConfigureAwait(false);
            var stdErr = await stderrTask.ConfigureAwait(false);

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = stdOut,
                StandardError = stdErr,
                Duration = stopwatch.Elapsed
            };
        }
    }
}
