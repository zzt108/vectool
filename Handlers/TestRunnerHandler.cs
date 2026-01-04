using LogCtxShared;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;
using VecTool.Configuration.Logging;
using VecTool.Core.Abstractions;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    /// <summary>
    /// Handler for building and running unit tests, then persisting results
    /// to a dated output file with structured markdown formatting.
    /// </summary>
    public sealed class TestRunnerHandler
    {
        private readonly string? _outputFile;
        private readonly string _solutionPath;
        private readonly IProcessRunner _processRunner;
        private readonly IUserInterface? _ui;
        private readonly IRecentFilesManager? _recentFilesManager;
        private readonly string _branchName;
        private readonly string _vectorStoreId;

        private readonly ILogger logger = AppLogger.For<TestRunnerHandler>();

        /// <summary>
        /// Canonical DI constructor expected by unit tests.
        /// new TestRunnerHandler(string, string, IProcessRunner, IUserInterface, IRecentFilesManager)
        /// </summary>
        public TestRunnerHandler(
            ILogger logger,
            string solutionPath,
            string? outputFile,
            IProcessRunner processRunner,
            IUserInterface? ui,
            IRecentFilesManager? recentFiles,
            string branchName,
            string vectorStoreId)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _outputFile = outputFile;
            _solutionPath = solutionPath ?? throw new ArgumentNullException(nameof(solutionPath));
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            _ui = ui;
            _recentFilesManager = recentFiles;
            _branchName = branchName;
            _vectorStoreId = vectorStoreId;
        }

        /// <summary>
        /// Runs dotnet test command against the solution and captures results.
        /// </summary>
        public async Task<string?> RunTestsAsync(CancellationToken ct)
        {
            using var _ = logger.SetContext(new Props()
                .Add("Operation", "RunTestsAsync")
                .Add("SolutionPath", _solutionPath));

            try
            {
                var testArgs = $"test \"{_solutionPath}\" --no-restore --verbosity minimal";

                var testResult = await _processRunner.RunAsync(
                    fileName: "dotnet",
                    arguments: testArgs,
                    workingDirectory: Path.GetDirectoryName(_solutionPath),
                    ct: ct).ConfigureAwait(false);

                var message = MapExitCodeToMessage(testResult.ExitCode);

                using var ctx = logger.SetContext(new Props()
                    .Add("ExitCode", testResult.ExitCode)
                    .Add("Message", message));

                switch (testResult.ExitCode)
                {
                    case 0:
                        _ui?.ShowMessage(message, "Test Runner - No Fails", MessageType.Information);
                        logger.LogWarning($"Tests completed with exit code {testResult.ExitCode}. {message}");
                        break;

                    case 1:
                    case 2:
                        _ui?.ShowMessage($"Tests completed with exit code {testResult.ExitCode}. {message}", "Test Runner - With issues", MessageType.Warning);
                        logger.LogWarning($"Tests completed with exit code {testResult.ExitCode}. {message}");
                        break;

                    default:
                        _ui?.ShowMessage($"Tests completed with exit code {testResult.ExitCode}. {message}", "Test Runner - LogError", MessageType.LogError);
                        logger.LogWarning($"Tests completed with exit code {testResult.ExitCode}. {message}");
                        break;
                }

                // Generate output filename with branch name
                if (_outputFile != null)
                {
                    await WriteTestResult(testResult, ct).ConfigureAwait(false);

                    if (_recentFilesManager != null && File.Exists(_outputFile))
                    {
                        var fileInfo = new FileInfo(_outputFile);
                        _recentFilesManager.RegisterGeneratedFile(
                            _outputFile,
                            RecentFileType.TestResults_Md,
                            null,
                            fileInfo.Length
                        );
                    }
                }
                logger.LogInformation("Test run finished successfully.");
                _ui?.UpdateStatus("Test run finished successfully.");
                if (testResult.ExitCode == 0 && _outputFile != null)
                {
                    return _outputFile;
                }
                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Test execution failed");
                throw;
            }
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

        /// <summary>
        /// Writes test results to file as structured markdown with headers, instructions,
        /// test summary, fenced output block, and recommendations.
        /// </summary>
        private async Task WriteTestResult(ProcessResult testResult, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_outputFile))
                return;

            using var _ = logger.SetContext(new Props()
                .Add("Operation", "WriteTestResult")
                .Add("OutputFile", _outputFile)
                .Add("ExitCode", testResult.ExitCode));

            try
            {
                // Extract test metadata from output
                var output = testResult.StandardOutput ?? string.Empty;
                var metadata = ParseTestMetadata(output);

                // Build markdown content with proper structure
                var markdownContent = BuildMarkdownReport(metadata, output, testResult.ExitCode);

                // Write to file asynchronously
                await File.WriteAllTextAsync(_outputFile, markdownContent, ct).ConfigureAwait(false);
                logger.SetContext(new Props().Add("Operation", "WriteTestResult").Add("FileSize", markdownContent.Length));
                logger.LogInformation("Test testResult markdown written successfully");
            }
            catch (Exception ex)
            {
                using var __ = logger.SetContext(new Props()
                    .Add("Operation", "WriteTestResult")
                    .Add("ErrorType", ex.GetType().Name)
                    .Add("OutputFile", _outputFile));
                logger.LogError(ex, "Failed to write test testResult markdown");
                throw;
            }
        }

        /// <summary>
        /// Constructs a professional markdown report with headers, instructions,
        /// test summary, fenced code blocks, reference tables, and recommendations.
        /// </summary>
        private string BuildMarkdownReport(TestMetadata metadata, string testOutput, int exitCode)
        {
            var sb = new StringBuilder();

            // Header with metadata
            sb.AppendLine("# Test Execution Report");
            sb.AppendLine();
            sb.AppendLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**Branch:** {_branchName}");
            sb.AppendLine($"**Vector Store ID:** {_vectorStoreId}");
            sb.AppendLine();

            var instructions = @"
Analyze test results and categorize failures by:
1. Quick fixes (wrong tests, missing files, simple logic)
2. Medium complexity (config issues, property mismatches)
3. Architectural problems (design flaws, WinForms testing)

! Tests aren't sacred, they can be wrong too !

Prioritize low-hanging fruits. Flag suspicious test assertions.
Include time estimates and impact per fix.

Consequently refer to IDs introduced in Test Failure Analysis in following sections

+ Highlight anti-patterns and suggest refactoring opportunities
+ Generate numbered fix list for ""reply with numbers"" workflow

- NO CHARTS!
Attached: [test output file] + [optional: codebase context file]
";
            // Instructions section
            sb.AppendLine("## Instructions");
            sb.AppendLine();
            sb.AppendLine(instructions);
            sb.AppendLine();

            // Test Summary with key metrics
            sb.AppendLine("## Test Summary");
            sb.AppendLine();
            sb.AppendLine($"- **Exit Code:** {exitCode}");
            sb.AppendLine($"- **Total Tests:** {metadata.TotalTests}");
            sb.AppendLine($"- **Passed:** {metadata.PassedTests}");
            sb.AppendLine($"- **Failed:** {metadata.FailedTests}");
            sb.AppendLine($"- **Skipped:** {metadata.SkippedTests}");
            sb.AppendLine($"- **Duration:** {metadata.Duration}");
            sb.AppendLine();

            // Status badge with emoji
            var statusEmoji = exitCode == 0 ? "✅" : "❌";
            var statusText = exitCode == 0 ? "PASSED" : "FAILED";
            sb.AppendLine($"**Status:** {statusEmoji} {statusText}");
            sb.AppendLine();

            // Test Output in properly fenced code block
            sb.AppendLine("## Detailed Test Output");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(testOutput);
            sb.AppendLine("```");
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Parses test metadata from VSTest-format output using regex patterns.
        /// Extracts test counts, duration, and failure information.
        /// </summary>
        private TestMetadata ParseTestMetadata(string output)
        {
            var metadata = new TestMetadata
            {
                TotalTests = 0,
                PassedTests = 0,
                FailedTests = 0,
                SkippedTests = 0,
                Duration = "Unknown"
            };

            if (string.IsNullOrWhiteSpace(output))
                return metadata;

            // Parse test counts from VSTest output format
            var totalMatch = Regex.Match(output, @"Total tests:\s*(\d+)", RegexOptions.IgnoreCase);
            var passedMatch = Regex.Match(output, @"Passed:\s*(\d+)", RegexOptions.IgnoreCase);
            var failedMatch = Regex.Match(output, @"Failed:\s*(\d+)", RegexOptions.IgnoreCase);
            var skippedMatch = Regex.Match(output, @"Skipped:\s*(\d+)", RegexOptions.IgnoreCase);
            var durationMatch = Regex.Match(output, @"Total time:\s*([\d:.]+)", RegexOptions.IgnoreCase);

            if (totalMatch.Success) metadata.TotalTests = int.Parse(totalMatch.Groups[1].Value);
            if (passedMatch.Success) metadata.PassedTests = int.Parse(passedMatch.Groups[1].Value);
            if (failedMatch.Success) metadata.FailedTests = int.Parse(failedMatch.Groups[1].Value);
            if (skippedMatch.Success) metadata.SkippedTests = int.Parse(skippedMatch.Groups[1].Value);
            if (durationMatch.Success) metadata.Duration = durationMatch.Groups[1].Value;

            return metadata;
        }

        /// <summary>
        /// Data structure holding parsed test execution statistics.
        /// </summary>
        internal class TestMetadata
        {
            public int TotalTests { get; set; }
            public int PassedTests { get; set; }
            public int FailedTests { get; set; }
            public int SkippedTests { get; set; }
            public string Duration { get; set; } = "Unknown";
        }
    }
}