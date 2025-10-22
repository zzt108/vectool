// Path: Handlers/TestRunnerHandler.cs

using LogCtxShared;
using NLogShared;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VecTool.Core.Abstractions;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    /// <summary>
    /// Handler for building and running unit tests, then persisting results to a dated output file.
    /// </summary>
    public sealed class TestRunnerHandler
    {
        private readonly string? _outputFile;
        private readonly string _solutionPath;
        private readonly IProcessRunner _processRunner;
        private readonly IUserInterface? _ui;
        private readonly IRecentFilesManager? _recentFilesManager;

        /// <summary>
        /// Canonical DI constructor expected by unit tests:
        /// new TestRunnerHandler(string, string, IProcessRunner, IUserInterface, IRecentFilesManager)
        /// </summary>
        public TestRunnerHandler(
            string solutionPath,
            string? outputFile,
            IProcessRunner processRunner,
            IUserInterface? ui,
            IRecentFilesManager? recentFiles)
        {
            this._outputFile = outputFile;
            this._solutionPath = solutionPath ?? throw new ArgumentNullException(nameof(solutionPath));
            this._processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            this._ui = ui;
            this._recentFilesManager = recentFiles;
        }

        /// <summary>
        /// Builds the solution, then executes tests and returns:
        /// - a file path containing test output on success (exit code 0)
        /// - null on failure (non-zero exit code).
        /// </summary>
        /// <param name="vectorStoreId">Arbitrary id used to tag outputs; tests pass values like "S" or "storeX".</param>
        /// <param name="branchName">Git branch name to include in the output filename.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<string?> RunTestsAsync(string vectorStoreId, string branchName, CancellationToken ct)
        {
            if (vectorStoreId is null)
                throw new ArgumentNullException(nameof(vectorStoreId));
            if (branchName is null)
                throw new ArgumentNullException(nameof(branchName));

            using var log = new CtxLogger();
            log.Ctx.Set(new Props()
                .Add("Operation", "RunTests")
                .Add("VectorStoreId", vectorStoreId)
                .Add("BranchName", branchName)
                .Add("SolutionPath", _solutionPath));

            // ✅ First build the solution
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

            var testArgs = $"test \"{_solutionPath}\" --no-restore --verbosity minimal";

            var testResult = await _processRunner.RunAsync(
                fileName: "dotnet",
                arguments: testArgs,
                workingDirectory: solutionDir,
                ct: ct).ConfigureAwait(false);

            var message = MapExitCodeToMessage(testResult.ExitCode);

            using var ctx = log.Ctx.Set(new Props()
                .Add("ExitCode", testResult.ExitCode)
                .Add("Message", message));

            if (testResult.ExitCode == 0)
            {
                _ui?.ShowMessage(message, "Test Runner - No Fails", MessageType.Information);
                log.Warn($"Tests completed with exit code {testResult.ExitCode}. {message}");
            }
            else
            {
                log.Warn($"Tests completed with exit code {testResult.ExitCode}. {message}");
            }

            // 🔄 MODIFY - Generate output filename with branch name
            if (_outputFile != null)
            {
                await File.WriteAllTextAsync(_outputFile, testResult.StandardOutput ?? string.Empty, ct).ConfigureAwait(false);

                if (_recentFilesManager != null && File.Exists(_outputFile))
                {
                    var fileInfo = new FileInfo(_outputFile);
                    _recentFilesManager.RegisterGeneratedFile(
                        _outputFile,
                        RecentFileType.TestResults,
                        null,
                        fileInfo.Length
                    );
                }
            }

            log.Info("Test run finished successfully.");
            _ui?.UpdateStatus("Test run finished successfully.");
            return message;
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

        private static string Sanitize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "unknown";
            return Regex.Replace(raw, @"[^\w\-]", "_");
        }
    }
}
