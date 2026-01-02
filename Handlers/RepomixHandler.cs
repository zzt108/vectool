using LogCtxShared;
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
            using var _ = LogCtx.Set();

            if (!Directory.Exists(targetDirectory))
            {
                log.Warn($"Target directory does not exist: {targetDirectory}");
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
                var args = BuildRepomixArguments(targetDirectory, outputPath, vectorStoreConfig, command);
                log.Debug($"Repomix args: {args}");

                // ✅ Step 3: Execute repomix
                var result = await processRunner.RunAsync(
                    command,
                    args,
                    targetDirectory,
                    cancellationToken);

                if (result.ExitCode != 0)
                {
                    log.Warn($"Repomix failed with exit code {result.ExitCode}:\n{result.StandardError}");
                    userInterface.ShowMessage(
                        $"Repomix execution failed:\n{result.StandardError}",
                        "Repomix Error",
                        MessageType.Error);
                    return null;
                }

                // ✅ Step 4: Verify output file was created
                if (!File.Exists(outputPath))
                {
                    log.Warn($"Repomix completed but output file not found: {outputPath}");
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
                    fileType: RecentFileType.Repomix_Xml,
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
        /// Checks if repomix is available via global install or npx.
        /// Returns (isAvailable, command to use).
        /// </summary>
        /// <remarks>
        // Try global install first (more reliable on Windows).
        /// Fallback to npx if global install not found.
        /// Uses full path resolution to handle PATH inconsistencies.
        /// </remarks>
        private async Task<(bool isAvailable, string command)> IsRepomixAvailableAsync(
            CancellationToken cancellationToken)
        {
            using var _ = LogCtx.Set(new Props().Add("Method", "IsRepomixAvailableAsync"));

            // ✅ Step 1: Try global repomix install first (more reliable on Windows)
            log.Debug("Checking for global 'repomix' installation...");
            var repomixPath = DetermineExecutablePath("repomix");

            if (!string.IsNullOrEmpty(repomixPath))
            {
                try
                {
                    var repomixResult = await processRunner.RunAsync(
                        repomixPath,
                        "--version",
                        Directory.GetCurrentDirectory(),
                        cancellationToken);

                    if (repomixResult.ExitCode == 0)
                    {
                        log.Info($"Global 'repomix' found at: {repomixPath}");
                        return (true, repomixPath);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"Global 'repomix' found but --version check failed: {repomixPath}");
                }
            }
            else
            {
                log.Debug("Global 'repomix' not found in PATH");
            }

            // ✅ Step 2: Try npx as fallback
            log.Debug("Checking for 'npx' availability...");
            var npxPath = DetermineExecutablePath("npx");

            if (!string.IsNullOrEmpty(npxPath))
            {
                try
                {
                    var npxResult = await processRunner.RunAsync(
                        npxPath,
                        "--version",
                        Directory.GetCurrentDirectory(),
                        cancellationToken);

                    if (npxResult.ExitCode == 0)
                    {
                        log.Info($"npx found at: {npxPath}");
                        return (true, npxPath);
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex, $"npx found but --version check failed: {npxPath}");
                }
            }
            else
            {
                log.Debug("npx not found in PATH");
            }

            log.Warn("Neither 'repomix' nor 'npx' found in PATH");
            return (false, string.Empty);
        }

        /// <summary>
        /// Resolves the full path of an executable in the system PATH.
        /// </summary>
        /// <param name="executableName">Name of executable (e.g., "repomix", "npx")</param>
        /// <returns>Full path if found, or null if not in PATH</returns>
        /// <remarks>
        // - Handles Windows .exe/.cmd extensions and Unix executables.
        /// Uses 'where' (Windows) or 'which' (Unix) to resolve paths.
        /// </remarks>
        private string? DetermineExecutablePath(string executableName)
        {
            using var _ = LogCtx.Set(new Props().Add("Executable", executableName));

            try
            {
                var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

                if (isWindows)
                {
                    // On Windows, try common extensions
                    var extensions = new[] { ".cmd", ".exe", "" };

                    foreach (var ext in extensions)
                    {
                        var fullName = executableName + ext;
                        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                        var paths = pathValue.Split(Path.PathSeparator);

                        foreach (var dir in paths)
                        {
                            if (string.IsNullOrWhiteSpace(dir)) continue;

                            var candidatePath = Path.Combine(dir, fullName);
                            if (File.Exists(candidatePath))
                            {
                                log.Debug($"Found executable: {candidatePath}");
                                return candidatePath;
                            }
                        }
                    }
                }
                else
                {
                    // On Unix, use 'which' command
                    var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                    var paths = pathValue.Split(Path.PathSeparator);

                    foreach (var dir in paths)
                    {
                        if (string.IsNullOrWhiteSpace(dir)) continue;

                        var candidatePath = Path.Combine(dir, executableName);
                        if (File.Exists(candidatePath))
                        {
                            log.Debug($"Found executable: {candidatePath}");
                            return candidatePath;
                        }
                    }
                }

                log.Debug($"Executable '{executableName}' not found in PATH");
                return null;
            }
            catch (Exception ex)
            {
                log.Error(ex, $"Error resolving executable path for '{executableName}'");
                return null;
            }
        }

        /// <summary>
        /// Builds repomix CLI arguments with proper escaping and command-aware prefix.
        /// </summary>
        /// <param name="command">The command being used (npx path or repomix path)</param>
        /// <remarks>
        /// 🔄 MODIFY - Only include "repomix" subcommand when using npx.
        /// For direct repomix executable, skip the subcommand prefix.
        /// </remarks>
        private string BuildRepomixArguments(
            string targetDirectory,
            string outputPath,
            VectorStoreConfig? config,
            string command)
        {
            using var _ = LogCtx.Set(new Props()
                .Add("Command", command)
                .Add("TargetDirectory", targetDirectory)
                .Add("OutputPath", outputPath));

            // Only include "repomix" subcommand for npx
            var isNpx = command.Contains("npx", StringComparison.OrdinalIgnoreCase);
            var args = isNpx ? "repomix " : "";

            // ✅ Explicit XML output style (default, but be explicit)
            args += "--style xml ";

            // ✅ Output file - use forward slashes for cross-platform compatibility
            var normalizedOutputPath = outputPath.Replace("\\", "/");
            args += $"--output \"{normalizedOutputPath}\" ";

            // ✅ Target directory - use forward slashes
            var normalizedTargetDir = targetDirectory.Replace("\\", "/");
            args += $"\"{normalizedTargetDir}\"";

            // TODO: Future enhancement - map VectorStoreConfig exclusions to repomix --ignore patterns

            log.Debug($"Built args: {args}");
            return args.Trim();
        }

        /// <summary>
        /// Shows installation help dialog to the user.
        /// </summary>
        private void ShowInstallationHelp()
        {
            var msg =
                "Repomix was not found.\n\n" +
                "Options:\n" +
                "1) NPX (recommended): ensure Node.js 18+ is installed, then run via 'npx repomix'.\n" +
                "2) Global: 'npm install -g repomix' then 'repomix --version'.\n" +
                "3) macOS/Linux: 'brew install repomix'.\n\n" +
                "Troubleshooting: verify 'npx --version' or 'repomix --version' in a new terminal, and ensure PATH is updated.";
            userInterface.ShowMessage(msg, "Repomix Not Found", MessageType.Warning);
        }
    }
}