using NLogShared;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VecTool.Configuration;
using VecTool.Core;
using VecTool.Core.Abstractions;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    /// <summary>
    /// Executes repomix CLI to generate AI-friendly codebase XML exports.
    /// Handles installation detection and provides user guidance.
    /// </summary>
    public sealed class RepomixHandler
    {
        private static readonly CtxLogger log = new();
        private readonly IUserInterface userInterface;
        private readonly IRecentFilesManager recentFilesManager;
        private readonly IProcessRunner processRunner;

        public RepomixHandler(
            IUserInterface userInterface,
            IRecentFilesManager recentFilesManager,
            IProcessRunner? processRunner = null)
        {
            this.userInterface = userInterface ?? throw new ArgumentNullException(nameof(userInterface));
            this.recentFilesManager = recentFilesManager ?? throw new ArgumentNullException(nameof(recentFilesManager));
            this.processRunner = processRunner ?? new ProcessRunner();
        }

        /// <summary>
        /// Runs repomix on the specified target directory and saves output to destination.
        /// </summary>
        /// <param name="targetDirectory">Root directory to export (vector store folder).</param>
        /// <param name="outputPath">Full path for the output XML file.</param>
        /// <param name="vectorStoreConfig">Config for exclusions (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Output file path if successful, null if repomix not found.</returns>
        public async Task<string?> RunRepomixAsync(
            string targetDirectory,
            string outputPath,
            VectorStoreConfig? vectorStoreConfig = null,
            CancellationToken cancellationToken = default)
        {
            using var _ = log.Scope();

            if (!Directory.Exists(targetDirectory))
            {
                log.Error($"Target directory does not exist: {targetDirectory}");
                userInterface.ShowMessage(
                    $"Target directory not found:\n{targetDirectory}",
                    "Directory Not Found",
                    MessageType.Error);
                return null;
            }

            // ✅ Step 1: Check if repomix/npx is available
            var (isAvailable, command) = await IsRepomixAvailableAsync(cancellationToken);
            if (!isAvailable)
            {
                log.Warn("Repomix not found on system");
                ShowInstallationHelp();
                return null;
            }

            log.Info($"Using repomix command: {command}");

            try
            {
                userInterface.WorkStart($"Exporting codebase with Repomix...", new[] { targetDirectory });

                // ✅ Step 2: Build repomix command arguments
                var args = BuildRepomixArguments(targetDirectory, outputPath, vectorStoreConfig);
                log.Debug($"Repomix args: {args}");

                // ✅ Step 3: Execute repomix
                var result = await processRunner.RunProcessAsync(
                    command,
                    args,
                    targetDirectory,
                    cancellationToken);

                if (result.ExitCode != 0)
                {
                    log.Error($"Repomix failed with exit code {result.ExitCode}:\n{result.StdErr}");
                    userInterface.ShowMessage(
                        $"Repomix execution failed:\n{result.StdErr}",
                        "Repomix Error",
                        MessageType.Error);
                    return null;
                }

                // ✅ Step 4: Verify output file was created
                if (!File.Exists(outputPath))
                {
                    log.Error($"Repomix completed but output file not found: {outputPath}");
                    userInterface.ShowMessage(
                        $"Output file was not created:\n{outputPath}",
                        "Output Missing",
                        MessageType.Error);
                    return null;
                }

                var fileInfo = new FileInfo(outputPath);
                log.Info($"Repomix export successful: {fileInfo.Length} bytes");

                // ✅ Step 5: Register in recent files
                recentFilesManager.RegisterGeneratedFile(
                    filePath: outputPath,
                    fileType: RecentFileType.RepomixXml,
                    sourceFolders: new[] { targetDirectory },
                    fileSizeBytes: fileInfo.Length,
                    generatedAtUtc: DateTime.UtcNow);

                recentFilesManager.Save();

                return outputPath;
            }
            catch (Exception ex)
            {
                log.Error(ex, "Repomix execution failed");
                userInterface.ShowMessage(
                    $"An error occurred:\n{ex.Message}",
                    "Error",
                    MessageType.Error);
                return null;
            }
            finally
            {
                userInterface.WorkFinish();
            }
        }

        /// <summary>
        /// Checks if repomix is available via npx or global install.
        /// Returns (isAvailable, command to use).
        /// </summary>
        private async Task<(bool isAvailable, string command)> IsRepomixAvailableAsync(
            CancellationToken cancellationToken)
        {
            // ✅ Try npx repomix first (recommended)
            try
            {
                var npxResult = await processRunner.RunProcessAsync(
                    "npx",
                    "--version",
                    Directory.GetCurrentDirectory(),
                    cancellationToken);

                if (npxResult.ExitCode == 0)
                {
                    log.Debug("npx available, using: npx repomix");
                    return (true, "npx");
                }
            }
            catch
            {
                // npx not available, try global install
            }

            // ✅ Try global repomix install
            try
            {
                var repomixResult = await processRunner.RunProcessAsync(
                    "repomix",
                    "--version",
                    Directory.GetCurrentDirectory(),
                    cancellationToken);

                if (repomixResult.ExitCode == 0)
                {
                    log.Debug("repomix globally installed");
                    return (true, "repomix");
                }
            }
            catch
            {
                // Neither available
            }

            return (false, string.Empty);
        }

        /// <summary>
        /// Builds repomix CLI arguments with proper escaping.
        /// </summary>
        private string BuildRepomixArguments(
            string targetDirectory,
            string outputPath,
            VectorStoreConfig? config)
        {
            var args = "repomix "; // For npx, include subcommand

            // ✅ Explicit XML output style (default, but be explicit)
            args += "--style xml ";

            // ✅ Output file
            args += $"--output \"{outputPath}\" ";

            // ✅ Target directory
            args += $"\"{targetDirectory}\"";

            // TODO: Future enhancement - map VectorStoreConfig exclusions to repomix --ignore patterns

            return args.Trim();
        }

        /// <summary>
        /// Shows installation help dialog to the user.
        /// </summary>
        private void ShowInstallationHelp()
        {
            using var helpForm = new RepomixInstallHelpForm();
            helpForm.ShowDialog();
        }
    }
}
