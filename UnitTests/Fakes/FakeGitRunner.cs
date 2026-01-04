// ✅ FULL FILE VERSION
using VecTool.Core.Abstractions;

namespace UnitTests.Fakes
{
    public sealed class FakeGitRunner : IGitRunner
    {
        private readonly string branch;

        public FakeGitRunner(string branch = "unknown")
        {
            this.branch = string.IsNullOrWhiteSpace(branch) ? "unknown" : branch;
        }

        public Task<string> GetCurrentBranchAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(branch);

        public Task<string> GetCurrentBranchAsync(string workingDirectory, CancellationToken cancellationToken = default)
            => Task.FromResult(branch);
    }
}
