// ✅ FULL FILE VERSION
// File: Handlers/TestRunnerHandler.cs

using LogCtxShared;
using NLogShared;
using System;
using System.Threading;
using System.Threading.Tasks;
using VecTool.Core.Abstractions;

namespace VecTool.Handlers
{
    /// <summary>
    /// Runs test suites and returns a single, human-readable message based on the process exit code.
    /// Phase 4.3.1 MVP: exit-code only; no parsing of stdout/stderr. 🚫
    /// </summary>
    public sealed class TestRunnerHandler
    {
        private readonly IProcessRunner processRunner;

        public TestRunnerHandler(IProcessRunner processRunner)
        {
            this.processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        }

        /// <summary>
        /// Executes "dotnet test" for the given solution and returns a friendly status message.
        /// Logs Operation, Solution, ExitCode, and Message via LogCtx for SEQ-friendly diagnostics.
        /// </summary>
        public async Task<string> RunTestsAsync(string solutionPath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(solutionPath))
                throw new ArgumentException("Solution path is required.", nameof(solutionPath));

            using var log = new CtxLogger();
            log.Ctx.Set(new LogCtxShared.Props()
                .Add("Operation", "RunTests")
                .Add("Solution", solutionPath));

            var args = $"test \"{solutionPath}\" --no-build --verbosity minimal";

            var result = await processRunner.RunAsync(
                fileName: "dotnet",
                arguments: args,
                workingDirectory: null,
                ct: ct).ConfigureAwait(false);

            var message = MapExitCodeToMessage(result.ExitCode);

            log.Ctx.Set(new LogCtxShared.Props()
                .Add("Operation", "RunTests")
                .Add("Solution", solutionPath)
                .Add("ExitCode", result.ExitCode)
                .Add("Message", message));

            if (result.ExitCode == 0)
            {
                log.Info("Tests passed successfully.");
            }
            else
            {
                log.Warn($"Tests completed with exit code {result.ExitCode}. {message}");
            }

            return message;
        }

        /// <summary>
        /// Maps known test exit codes to a concise, user-facing message.
        /// </summary>
        internal static string MapExitCodeToMessage(int code) => code switch
        {
            0 => "All tests passed.",
            1 => "One or more tests failed (VSTest).",
            2 => "One or more tests failed (MSTest platform).",
            3 => "Test run aborted.",
            8 => "No tests discovered. Check your test project.",
            10 => "Test adapter/infrastructure failure.",
            _ => $"Unknown exit code {code}. See output for details."
        };
    }
}
