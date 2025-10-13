// GitRunner.cs — migrate from NLogShared/CtxLogger to NLog
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VecTool.Core.Abstractions;

namespace VecTool.Core
{
    /// <summary>
    /// Executes Git commands against a repository and returns outputs or throws on failures.
    /// </summary>
    public sealed class GitRunner : IGitRunner
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly string workingDirectory;

        public GitRunner(string workingDirectory)
        {
            this.workingDirectory = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));
        }

        public static bool IsGitRepository(string path)
            => !string.IsNullOrWhiteSpace(path) && Directory.Exists(Path.Combine(path, ".git"));

        public async Task<string> RunGitCommandAsync(string command, int timeoutSeconds = 60)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Git command is required.", nameof(command));

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

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = false };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            log.Info("Starting git command {Command} in {WorkingDirectory}", command, workingDirectory);
            try
            {
                if (!process.Start())
                    throw new InvalidOperationException("Failed to start git process.");

                _ = process.StandardOutput.ReadToEndAsync().ContinueWith(t => outputBuilder.Append(t.Result));
                _ = process.StandardError.ReadToEndAsync().ContinueWith(t => errorBuilder.Append(t.Result));

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                await Task.Run(() => process.WaitForExit(), cts.Token).ConfigureAwait(false);

                if (process.ExitCode != 0)
                {
                    var err = errorBuilder.ToString();
                    var ex = new InvalidOperationException($"git {command} failed with exit code {process.ExitCode}: {err}");
                    log.Error(ex, "Git command failed {Command} in {WorkingDirectory}", command, workingDirectory);
                    throw ex;
                }

                var output = outputBuilder.ToString();
                log.Info("Git command completed {Command} in {WorkingDirectory} with {Length} chars", command, workingDirectory, output.Length);
                return output;
            }
            catch (OperationCanceledException oce)
            {
                var ex = new TimeoutException($"git {command} timed out after {timeoutSeconds}s", oce);
                log.Error(ex, "Git command timeout {Command} in {WorkingDirectory} after {TimeoutSeconds}s", command, workingDirectory, timeoutSeconds);
                throw ex;
            }
        }

        public Task<string> GetStatusAsync(CancellationToken ct = default)
            => RunGitCommandAsync("status --porcelain=v1");

        public Task<string> GetDiffAsync(CancellationToken ct = default)
            => RunGitCommandAsync("diff --patch --no-color");

        public async Task<string?> GetCurrentBranchAsync(string path, CancellationToken ct = default)
        {
            var runner = new GitRunner(path);
            var name = await runner.RunGitCommandAsync("rev-parse --abbrev-ref HEAD").ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        }

        public async Task<string> GetSubmodulesAsync(CancellationToken ct = default)
            => await RunGitCommandAsync("submodule status --recursive").ConfigureAwait(false);
    }
}
