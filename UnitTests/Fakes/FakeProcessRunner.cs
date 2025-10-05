// ✅ FULL FILE VERSION
using System;
using System.Threading;
using System.Threading.Tasks;
using VecTool.Core.Abstractions;

namespace UnitTests.Fakes
{
    public sealed class FakeProcessRunner : IProcessRunner
    {
        private readonly int exitCode;
        private readonly string stdout;
        private readonly string stderr;
        private readonly TimeSpan duration;

        public FakeProcessRunner(int exitCode = 0, string stdout = "", string stderr = "", TimeSpan? duration = null)
        {
            this.exitCode = exitCode;
            this.stdout = stdout ?? "";
            this.stderr = stderr ?? "";
            this.duration = duration ?? TimeSpan.Zero;
        }

        public Task<ProcessResult> RunAsync(string fileName, string arguments, string? workingDirectory = null, CancellationToken cancellationToken = default)
            => Task.FromResult(new ProcessResult
            {
                ExitCode = exitCode,
                StandardOutput = stdout,
                StandardError = stderr,
                Duration = duration
            });
    }
}
