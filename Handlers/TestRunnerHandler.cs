// Path: Handlers/TestRunnerHandler.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LogCtxShared;
using NLogShared;
using VecTool.Core.Abstractions;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    /// <summary>
    /// Handles programmatic execution of dotnet test with build step and result file generation.
    /// Phase 4.3.1 MVP: Build solution first, then run tests with exit-code driven flow.
    /// </summary>
    public sealed class TestRunnerHandler
    {
        private readonly string _solutionPath;
        private readonly IProcessRunner _processRunner;
        private readonly IUserInterface? _ui;
        private readonly IRecentFilesManager _recentFiles;

        /// <summary>
        /// Canonical DI constructor expected by unit tests:
        /// new TestRunnerHandler(string, IProcessRunner, IUserInterface, IRecentFilesManager)
        /// </summary>
        public TestRunnerHandler(
            string solutionPath,
            IProcessRunner processRunner,
            IUserInterface ui,
            IRecentFilesManager recentFiles)
        {
            this._solutionPath = solutionPath ?? throw new ArgumentNullException(nameof(solutionPath));
            this._processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            this._ui = ui;
            this._recentFiles = recentFiles ?? throw new ArgumentNullException(nameof(recentFiles));
        }

        /// <summary>
        /// Builds the solution, then executes tests and returns:
        /// - a file path containing test output on success (exit code 0)
        /// - null on failure (non-zero exit code).
        /// </summary>
        /// <param name="vectorStoreId">Arbitrary id used to tag outputs; tests pass values like "S" or "storeX".</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<string?> RunTestsAsync(string vectorStoreId, CancellationToken ct)
        {
            if (vectorStoreId is null)
                throw new ArgumentNullException(nameof(vectorStoreId));

            using var log = new CtxLogger();
            log.Ctx.Set(new LogCtxShared.Props()
                .Add("Operation", "RunTests")
                .Add("VectorStoreId", vectorStoreId)
                .Add("SolutionPath", _solutionPath));

            // ✅ NEW: First build the solution
            _ui?.UpdateStatus($"Building solution: {Path.GetFileName(_solutionPath)}...");
            log.Info("Starting solution build.");

            var solutionDir = Path.GetDirectoryName(_solutionPath);
            var buildArgs = $"build \"{_solutionPath}\" --configuration Release";
            var buildResult = await _processRunner.RunAsync(
                fileName: "dotnet",
                arguments: buildArgs,
                workingDirectory: solutionDir,
                ct: ct).ConfigureAwait(false);

            if (buildResult.ExitCode != 0)
            {
                var buildMessage = $"Build failed with exit code {buildResult.ExitCode}.";
                _ui?.ShowMessage(buildMessage, "Build Error", MessageType.Error);
                log.Warn($"Build failed. ExitCode={buildResult.ExitCode}");
                return null;
            }

            log.Info("Build succeeded. Starting tests.");
            _ui?.UpdateStatus("Running tests...");

            // ✅ MODIFY: Remove --no-build flag since we just built, and specify the solution
            var testArgs = $"test \"{_solutionPath}\" --no-restore --verbosity minimal";

            var testResult = await _processRunner.RunAsync(
                fileName: "dotnet",
                arguments: testArgs,
                workingDirectory: solutionDir,
                ct: ct).ConfigureAwait(false);

            var message = MapExitCodeToMessage(testResult.ExitCode);

            using var ctx = log.Ctx.Set(new LogCtxShared.Props()
                .Add("ExitCode", testResult.ExitCode)
                .Add("Message", message));

            if (testResult.ExitCode != 0)
            {
                _ui?.ShowMessage(message, "Test Runner", MessageType.Warning);
                log.Warn($"Tests completed with exit code {testResult.ExitCode}. {message}");
                return null;
            }

            // On success, persist output to a temp file and return its path (as asserted by tests).
            var outDir = Path.Combine(Path.GetTempPath(), "VecToolTestResults");
            Directory.CreateDirectory(outDir);

            var fileName = $"test-results-{Sanitize(vectorStoreId)}.md";
            var outPath = Path.Combine(outDir, fileName);

            // Write whatever stdout was captured; tests only assert existence, not content.
            await File.WriteAllTextAsync(outPath, testResult.StandardOutput ?? string.Empty, ct).ConfigureAwait(false);

            log.Info("Tests passed successfully.");
            _ui?.UpdateStatus("Tests completed successfully.");
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
