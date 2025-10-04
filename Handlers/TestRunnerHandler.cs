// File: Handlers/TestRunnerHandler.cs

using NLogShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using VecTool.Core;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    /// <summary>
    /// Handler for executing dotnet test at the solution root and capturing results into a file.
    /// The generated report is registered in Recent Files as TestResults.
    /// </summary>
    public sealed class TestRunnerHandler : FileHandlerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestRunnerHandler"/> class.
        /// </summary>
        /// <param name="_ui">Optional _ui for progress and messaging.</param>
        /// <param name="recentFilesManager">Optional recent files manager for registering outputs.</param>
        public TestRunnerHandler(IUserInterface? _ui, IRecentFilesManager? recentFilesManager)
            : base(_ui, recentFilesManager)
        {
        }

        /// <summary>
        /// Runs dotnet test using the provided solution path and writes the output into a deterministic file named 
        /// TestResults-{vectorStoreName}.{branchName}.txt in the solution directory, then registers it in Recent Files.
        /// </summary>
        /// <param name="solutionPath">Full path to the solution file (.sln).</param>
        /// <param name="vectorStoreName">Current vector store name to embed in the result file name.</param>
        /// <param name="selectedFolders">Source folders to register with the generated file for traceability.</param>
        /// <returns>Output file path if successful, otherwise null.</returns>
        public async Task<string?> RunTestsAsync(string solutionPath, string vectorStoreName, List<string> selectedFolders)
        {
            using var ctx = _log.Ctx.Set();
            try
            {
                if (string.IsNullOrWhiteSpace(solutionPath))
                    throw new ArgumentException("Solution path is req_uired.", nameof(solutionPath));
                if (!File.Exists(solutionPath))
                    throw new FileNotFoundException("Solution file not found.", solutionPath);

                if (string.IsNullOrWhiteSpace(vectorStoreName))
                    vectorStoreName = "unknown";

                _ui?.UpdateStatus("Running unit tests...");
                var solutionDir = Path.GetDirectoryName(solutionPath) ?? Directory.GetCurrentDirectory();

                var gitRunner = new GitRunner(solutionDir);
                var branchName = await gitRunner.GetCurrentBranchAsync().ConfigureAwait(false);
                branchName = string.IsNullOrWhiteSpace(branchName) ? "unknown" : branchName;

                var outputFileName = $"TestResults-{vectorStoreName}.{branchName}.txt";
                var outputPath = Path.Combine(solutionDir, outputFileName);

                _log.Info($"Starting dotnet test for solution {solutionPath}");
                var testOutput = await RunDotnetTestAsync(solutionPath).ConfigureAwait(false);
                await File.WriteAllTextAsync(outputPath, testOutput).ConfigureAwait(false);

                _log.Info($"Test results saved to {outputPath}");
                if (_recentFilesManager != null && File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    _recentFilesManager.RegisterGeneratedFile(
                        filePath: outputPath,
                        fileType: RecentFileType.TestResults,
                        sourceFolders: selectedFolders,
                        fileSizeBytes: fileInfo.Length
                    );
                }

                _ui?.UpdateStatus("Tests completed successfully.");
                return outputPath;
            }
            catch (Exception ex)
            {
                using var err = _log.Ctx.Set();
                err.Add("solutionPath", solutionPath);
                err.Add("vectorStoreName", vectorStoreName);
                _log.Error(ex, "Test execution failed.");

                _ui?.ShowMessage($"Test execution failed: {ex.Message}", "Test Error", MessageType.Error);
                return null;
            }
            finally
            {
                _ui?.WorkFinish();
            }
        }

        /// <summary>
        /// Executes dotnet test for the given solution and returns the captured standard output.
        /// Throws if the process returns a non-zero exit code.
        /// </summary>
        /// <param name="solutionPath">Full path to the solution file (.sln).</param>
        /// <returns>Standard output from the test run.</returns>
        /// <exception cref="InvalidOperationException">Thrown when dotnet test fails.</exception>
        private static async Task<string> RunDotnetTestAsync(string solutionPath)
        {
            var workingDir = Path.GetDirectoryName(solutionPath) ?? Directory.GetCurrentDirectory();
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"test \"{solutionPath}\"",
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            if (!process.Start())
                throw new InvalidOperationException("Failed to start dotnet test process.");

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);

            var output = await outputTask.ConfigureAwait(false);
            var error = await errorTask.ConfigureAwait(false);
            if (process.ExitCode != 0)
            {
                var message = $"dotnet test failed with exit code {process.ExitCode}{Environment.NewLine}{error}";
                throw new InvalidOperationException(message);
            }

            return output;
        }
    }
}
