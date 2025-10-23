// ✅ FULL FILE VERSION
using NLogShared;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VecTool.Core
{
    /// <summary>
    /// Core execution engine for GitRunner - handles process lifecycle, timeouts, and error handling.
    /// </summary>
    public sealed partial class GitRunner
    {
        private static readonly CtxLogger log = new();
        private readonly string workingDirectory;

        public GitRunner(string workingDirectory)
        {
            this.workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
        }

        /// <summary>
        /// Runs an arbitrary git command in the configured working directory with a timeout.
        /// Throws TimeoutException on timeout and InvalidOperationException on non-zero exit.
        /// </summary>
        public async Task<string> RunGitCommandAsync(string command, int timeoutSeconds = 60)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ArgumentException("Git command is required.", nameof(command));
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = command,
                WorkingDirectory = workingDirectory,
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

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            log.Info($"Running git command: git {command} in {workingDirectory}");

            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start git process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
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
                catch { /* ignored */ }

                log.Warn($"Git command timed out after {timeoutSeconds}s.");
                throw new TimeoutException("Git command timed out.");
            }

            // Ensure async reads complete
            process.WaitForExit();

            var exitCode = process.ExitCode;
            var stdOut = outputBuilder.ToString().Trim();
            var stdErr = errorBuilder.ToString().Trim();

            if (exitCode == 0)
            {
                log.Info("Git command finished successfully.");
                return stdOut;
            }

            log.Warn($"Git command failed with exit code {exitCode}. Error: {stdErr}");
            throw new InvalidOperationException($"Git command failed: {stdErr}");
        }
    }
}
