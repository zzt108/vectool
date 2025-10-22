// ✅ FULL FILE VERSION
// File: UnitTests/Fakes/FakeProcessRunner.cs

using System;
using System.Threading;
using System.Threading.Tasks;
using VecTool.Core.Abstractions;

namespace UnitTests.Fakes
{
    /// <summary>
    /// Test double for IProcessRunner that returns a predetermined result without starting real processes.
    /// </summary>
    public sealed class FakeProcessRunner : IProcessRunner
    {
        private readonly int exitCode;
        private readonly string stdout;
        private readonly string stderr;
        private readonly TimeSpan duration;

        public FakeProcessRunner(
            int exitCode = 0,
            string stdout = "",
            string stderr = "",
            TimeSpan? duration = null)
        {
            this.exitCode = exitCode;
            this.stdout = stdout ?? string.Empty;
            this.stderr = stderr ?? string.Empty;
            this.duration = duration ?? TimeSpan.Zero;
        }

        public Task<ProcessResult> RunAsync(string fileName, string arguments, string? workingDirectory, CancellationToken ct)
        {
            // Ignore parameters in the fake; tests configure the outcome via the constructor.
            var result = new ProcessResult
            {
                ExitCode = exitCode,
                StandardOutput = stdout,
                StandardError = stderr,
                Duration = duration
            };

            return Task.FromResult(result);
        }
    }
}
