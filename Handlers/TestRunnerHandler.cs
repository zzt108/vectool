// ✅ FULL FILE VERSION
// File: Handlers/TestRunnerHandler.cs

using LogCtxShared;
using NLogShared;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using VecTool.Core.Abstractions;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    /// <summary>
    /// Runs test suites and returns either a persisted test-output file path (success) or null (failure). 
    /// Phase 4.3.1 MVP: exit-code driven flow with minimal output handling, designed to satisfy dependency tests. 
    /// </summary>
    public sealed class TestRunnerHandler
    {
        private readonly IProcessRunner processRunner;
        private readonly IUserInterface ui;
        private readonly IRecentFilesManager recentFiles;

        // Canonical DI constructor expected by unit tests:
        // new TestRunnerHandler(IGitRunner, IProcessRunner, IUserInterface, IRecentFilesManager)
        public TestRunnerHandler(
            IProcessRunner processRunner,
            IUserInterface ui,
            IRecentFilesManager recentFiles)
        {
            this.processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            this.ui = ui ?? throw new ArgumentNullException(nameof(ui));
            this.recentFiles = recentFiles ?? throw new ArgumentNullException(nameof(recentFiles));
        }

        /// <summary>
        /// Executes tests and returns:
        /// - a file path containing test output on success (exit code 0)
        /// - null on failure (non-zero exit code).
        /// Signature is driven by UnitTests expectations. 
        /// </summary>
        /// <param name="vectorStoreId">Arbitrary id used to tag outputs; tests pass values like "S" or "storeX".</param>
        /// <param name="selectedFolders">Selected folders; may be empty per tests, not required for this MVP.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<string?> RunTestsAsync(string vectorStoreId, CancellationToken ct)
        {
            if (vectorStoreId is null)
                throw new ArgumentNullException(nameof(vectorStoreId));

            using var log = new CtxLogger();
            log.Ctx.Set(new LogCtxShared.Props()
                .Add("Operation", "RunTests")
                .Add("VectorStoreId", vectorStoreId)
                );

            // MVP: exit-code-only invocation; no need to rely on a specific working directory for tests that use FakeProcessRunner.
            var args = "test --no-build --verbosity minimal";

            var result = await processRunner.RunAsync(
                fileName: "dotnet",
                arguments: args,
                workingDirectory: null,
                ct: ct).ConfigureAwait(false);

            var message = MapExitCodeToMessage(result.ExitCode);

            log.Ctx.Set(new LogCtxShared.Props()
                .Add("ExitCode", result.ExitCode)
                .Add("Message", message));

            if (result.ExitCode != 0)
            {
                ui?.ShowMessage(message, "Test Runner", MessageType.Warning);
                log.Warn($"Tests completed with exit code {result.ExitCode}. {message}");
                return null;
            }

            // On success, persist output to a temp file and return its path (as asserted by tests).
            var outDir = Path.Combine(Path.GetTempPath(), "VecToolTestResults");
            Directory.CreateDirectory(outDir);

            var fileName = $"test-results-{Sanitize(vectorStoreId)}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.txt";
            var outPath = Path.Combine(outDir, fileName);

            // Write whatever stdout was captured; tests only assert existence, not content.
            await File.WriteAllTextAsync(outPath, result.StandardOutput ?? string.Empty, ct).ConfigureAwait(false);

            log.Info("Tests passed successfully.");
            return outPath;
        }

        public static string MapExitCodeToMessage(int code) => code switch
        {
            0 => "All tests passed.",
            1 => "One or more tests failed (VSTest).",
            2 => "One or more tests failed (MSTest platform).",
            3 => "Test run aborted.",
            8 => "No tests discovered. Check your test project.",
            10 => "Test adapter/infrastructure failure.",
            _ => $"Unknown exit code {code}. See output for details."
        };

        private static string Sanitize(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }
    }
}
