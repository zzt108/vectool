# Plan to call repomix on the selected vectorstore in vectool and add the output xml file to the recent file list. add a help section about how to install repomix if it is not found.

Adding Repomix integration requires a new menu handler that executes external process (`npx repomix`), handles installation errors gracefully with a help dialog, saves the XML output to a dated location, and registers it in the recent files system with proper vector store linking.

## Change Summary

| \# | File | Method/Area | Type | Why |
| :-- | :-- | :-- | :-- | :-- |
| 1 | `RecentFileType.cs` | Add `RepomixXml` enum | ✅ NEW | New file type for Repomix XML exports |
| 2 | `RepomixHandler.cs` | `RunRepomixAsync` | ✅ NEW | Orchestrates repomix execution and output handling |
| 3 | `MainForm.Designer.cs` | Add menu item | ✅ NEW | Add "Export to Repomix" under Actions menu |
| 4 | `MainForm.MenuActions.cs` | `exportToRepomixToolStripMenuItemClick` | ✅ NEW | Handler for new menu item |
| 5 | `MainForm.Core.cs` | Wire event | 🔄 MODIFY | Connect new menu item to handler |
| 6 | `RepomixInstallHelpForm.cs` | Help dialog | ✅ NEW | Instructions for installing repomix if not found |
| 7 | `ProcessRunner.cs` | Verify exists | 🔄 MODIFY | May need enhancement for exit code handling |


***

## Individual Changes

### 1️⃣ Add RepomixXml to RecentFileType enum

**File:** `VecTool.RecentFiles/RecentFileType.cs`

**Search for:**

```csharp
    /// <summary>Markdown file with test execution results.</summary>
    TestResultsMd = 5,

    /// <summary>File type could not be determined.</summary>
    Unknown = 0
}
```

**Change:**

```csharp
    /// <summary>Markdown file with test execution results.</summary>
    TestResultsMd = 5,

    // ✅ NEW - Repomix XML export
    /// <summary>Repomix XML codebase export for AI consumption.</summary>
    RepomixXml = 6,

    /// <summary>File type could not be determined.</summary>
    Unknown = 0
}
```

**Why:** Enables type-safe categorization and filtering of Repomix exports in recent files grid.

***

### 2️⃣ Create RepomixHandler class

**File:** `VecTool.Handlers/RepomixHandler.cs` ✅ NEW FILE

**Full Implementation:**

```csharp
// ✅ FULL FILE VERSION
#error "⚠️ NEW FILE - VecTool.Handlers/RepomixHandler.cs"

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLogShared;
using VecTool.Configuration;
using VecTool.Core;
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
```

**Why:** Encapsulates all repomix execution logic with proper error handling, installation detection, and recent files integration.

***

### 3️⃣ Add Repomix menu item to Actions menu

**File:** `OaiUI/MainForm.Designer.cs`

**Search for:**

```csharp
actionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 
    convertToMdToolStripMenuItem, 
    toolStripSeparator1, 
    getGitChangesToolStripMenuItem, 
    fileSizeSummaryToolStripMenuItem, 
    runTestsToolStripMenuItem });
```

**Change:**

```csharp
// 🔄 MODIFY - Add exportToRepomixToolStripMenuItem
actionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { 
    convertToMdToolStripMenuItem, 
    toolStripSeparator1, 
    getGitChangesToolStripMenuItem, 
    fileSizeSummaryToolStripMenuItem, 
    runTestsToolStripMenuItem,
    exportToRepomixToolStripMenuItem }); // ✅ NEW
```

**Search for (field declarations):**

```csharp
private ToolStripMenuItem runTestsToolStripMenuItem;
private ToolStripMenuItem helpToolStripMenuItem;
private ToolStripMenuItem aboutToolStripMenuItem;
```

**Change:**

```csharp
private ToolStripMenuItem runTestsToolStripMenuItem;
// ✅ NEW
private ToolStripMenuItem exportToRepomixToolStripMenuItem;
private ToolStripMenuItem helpToolStripMenuItem;
private ToolStripMenuItem aboutToolStripMenuItem;
```

**Search for (initialization section):**

```csharp
runTestsToolStripMenuItem = new ToolStripMenuItem();
helpToolStripMenuItem = new ToolStripMenuItem();
aboutToolStripMenuItem = new ToolStripMenuItem();
```

**Change:**

```csharp
runTestsToolStripMenuItem = new ToolStripMenuItem();
// ✅ NEW
exportToRepomixToolStripMenuItem = new ToolStripMenuItem();
helpToolStripMenuItem = new ToolStripMenuItem();
aboutToolStripMenuItem = new ToolStripMenuItem();
```

**Search for (runTestsToolStripMenuItem configuration):**

```csharp
// runTestsToolStripMenuItem
runTestsToolStripMenuItem.Name = "runTestsToolStripMenuItem";
runTestsToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.T;
runTestsToolStripMenuItem.Size = new Size(209, 22);
runTestsToolStripMenuItem.Text = "Run Tests";
```

**Add after:**

```csharp
// ✅ NEW - exportToRepomixToolStripMenuItem
exportToRepomixToolStripMenuItem.Name = "exportToRepomixToolStripMenuItem";
exportToRepomixToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.R;
exportToRepomixToolStripMenuItem.Size = new Size(209, 22);
exportToRepomixToolStripMenuItem.Text = "Export to Repomix";
```

**Why:** Adds UI entry point for Repomix export feature with Ctrl+R shortcut.

***

### 4️⃣ Add event handler for Repomix menu item

**File:** `OaiUI/MainForm.MenuActions.cs`

**Search for:**

```csharp
    /// <summary>Handler for Exit menu item (Alt+F4).</summary>
    private void exitToolStripMenuItemClick(object? sender, EventArgs e)
    {
        Application.Exit();
    }
}
```

**Add before closing brace:**

```csharp
    /// <summary>Handler for Export to Repomix menu item (Ctrl+R).</summary>
    private async void exportToRepomixToolStripMenuItemClick(object? sender, EventArgs e)
    {
        if (selectedFolders.Count == 0)
        {
            userInterface.ShowMessage(
                "Please select one or more folders first.",
                "No Folders Selected",
                MessageType.Warning);
            return;
        }

        var vsName = SanitizeFileName(comboBoxVectorStores.SelectedItem?.ToString() ?? "default");
        var branchName = SanitizeFileName(await GetCurrentBranchNameAsync().ConfigureAwait(true));

        var defaultFileName = RecentFilesOutputManager.Factory.BuildOutputPath(
            $"{vsName}_{branchName}",
            RecentFileType.RepomixXml);

        // ✅ Repomix expects to run in the target directory, so use first selected folder as target
        var targetDirectory = selectedFolders.First();

        using var saveFileDialog = new SaveFileDialog
        {
            Title = "Save Repomix Export As...",
            Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
            FileName = defaultFileName
        };

        if (saveFileDialog.ShowDialog() != DialogResult.OK)
            return;

        var outputPath = saveFileDialog.FileName;
        var config = GetCurrentVectorStoreConfig();

        try
        {
            var handler = new RepomixHandler(userInterface, recentFilesManager);
            var result = await handler.RunRepomixAsync(
                targetDirectory,
                outputPath,
                config,
                CancellationToken.None).ConfigureAwait(true);

            if (result != null)
            {
                userInterface.ShowMessage(
                    $"Successfully generated Repomix export at:\n{result}",
                    "Success",
                    MessageType.Information);

                // ✅ Refresh recent files panel
                recentFilesPanel.RefreshList();
            }
        }
        catch (Exception ex)
        {
            userInterface.ShowMessage(
                $"An error occurred: {ex.Message}",
                "Error",
                MessageType.Error);
        }
    }
```

**Why:** Orchestrates Repomix export workflow with proper error handling and UI feedback.

***

### 5️⃣ Wire Repomix menu item event

**File:** `OaiUI/MainForm.Core.cs`

**Search for:**

```csharp
    /// <summary>Wires up all event handlers for menu items and form controls.</summary>
    private void WireUpEvents()
    {
        // Menu items
        convertToMdToolStripMenuItem.Click += convertToMdToolStripMenuItemClick;
        getGitChangesToolStripMenuItem.Click += getGitChangesToolStripMenuItemClick;
        fileSizeSummaryToolStripMenuItem.Click += fileSizeSummaryToolStripMenuItemClick;
        runTestsToolStripMenuItem.Click += runTestsToolStripMenuItemClick;
        exitToolStripMenuItem.Click += exitToolStripMenuItemClick;
```

**Change:**

```csharp
    /// <summary>Wires up all event handlers for menu items and form controls.</summary>
    private void WireUpEvents()
    {
        // Menu items
        convertToMdToolStripMenuItem.Click += convertToMdToolStripMenuItemClick;
        getGitChangesToolStripMenuItem.Click += getGitChangesToolStripMenuItemClick;
        fileSizeSummaryToolStripMenuItem.Click += fileSizeSummaryToolStripMenuItemClick;
        runTestsToolStripMenuItem.Click += runTestsToolStripMenuItemClick;
        // ✅ NEW
        exportToRepomixToolStripMenuItem.Click += exportToRepomixToolStripMenuItemClick;
        exitToolStripMenuItem.Click += exitToolStripMenuItemClick;
```

**Why:** Connects UI menu item to handler for event-driven architecture.

***

### 6️⃣ Create Repomix installation help form

**File:** `OaiUI/RepomixInstallHelpForm.cs` ✅ NEW FILE

**Full Implementation:**

```csharp
// ✅ FULL FILE VERSION
#error "⚠️ NEW FILE - OaiUI/RepomixInstallHelpForm.cs"

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Vectool.OaiUI
{
    /// <summary>
    /// Displays installation instructions for Repomix when it's not found on the system.
    /// </summary>
    public sealed class RepomixInstallHelpForm : Form
    {
        private readonly TextBox txtInstructions;
        private readonly Button btnClose;
        private readonly Button btnOpenDocs;

        public RepomixInstallHelpForm()
        {
            // Form properties
            Text = "Repomix Not Found - Installation Help";
            Size = new Size(650, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Instructions text box
            txtInstructions = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10F),
                Dock = DockStyle.Fill,
                Text = GetInstructionsText()
            };

            // Button panel
            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10)
            };

            btnClose = new Button
            {
                Text = "Close",
                Width = 100,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            btnClose.Click += (s, e) => Close();

            btnOpenDocs = new Button
            {
                Text = "Open Documentation",
                Width = 160,
                Height = 30
            };
            btnOpenDocs.Click += BtnOpenDocs_Click;

            btnPanel.Controls.Add(btnClose);
            btnPanel.Controls.Add(btnOpenDocs);

            // Layout
            Controls.Add(txtInstructions);
            Controls.Add(btnPanel);

            AcceptButton = btnClose;
        }

        private string GetInstructionsText()
        {
            return @"╔══════════════════════════════════════════════════════════════════════╗
║                 REPOMIX NOT FOUND                                    ║
╚══════════════════════════════════════════════════════════════════════╝

Repomix is required to export your codebase to AI-friendly XML format.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📦 INSTALLATION OPTIONS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Option 1: Use NPX (Recommended - No Installation Required)
----------------------------------------------------------
No installation needed! NPX runs Repomix on-demand.

Prerequisites:
  • Node.js 18+ installed (includes NPX)
  • Download from: https://nodejs.org/

Verify NPX is available:
  > npx --version


Option 2: Global Installation via NPM
--------------------------------------
Install Repomix globally for faster execution:

  > npm install -g repomix

Verify installation:
  > repomix --version


Option 3: Homebrew (macOS/Linux)
---------------------------------
  > brew install repomix


━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔍 TROUBLESHOOTING
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

If npx or repomix commands are not found:

1. Ensure Node.js 18+ is installed:
   > node --version

2. Restart your terminal/command prompt after installation

3. On Windows, ensure Node.js is in your PATH:
   - Search for ""Environment Variables""
   - Add Node.js install directory to PATH

4. Try running VecTool as Administrator (Windows)


━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📚 MORE INFORMATION
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Official Documentation: https://repomix.com/guide
GitHub Repository:      https://github.com/yamadashy/repomix

After installation, restart VecTool and try the export again.
";
        }

        private void BtnOpenDocs_Click(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://repomix.com/guide",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open browser:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
```

**Why:** Provides clear, actionable guidance when Repomix isn't installed, improving user experience.

***

### 7️⃣ Update RecentFileType color mapping

**File:** `OaiUI/RecentFiles/RecentFilesPanel.DataBinding.cs`

**Search for:**

```csharp
private static Color GetColorForType(RecentFileType type)
{
    return type switch
    {
        RecentFileType.Plan => Color.Goldenrod,
        RecentFileType.Guide => Color.SteelBlue,
        RecentFileType.GitMd => Color.OrangeRed,
        RecentFileType.TestResultsMd => Color.MediumSeaGreen,
        RecentFileType.CodebaseMd => Color.MediumPurple,
        RecentFileType.CodebaseDocx => Color.LightSkyBlue,
        RecentFileType.CodebasePdf => Color.LightCoral,
        RecentFileType.Unknown => Color.Gainsboro,
        _ => Color.Gainsboro
    };
}
```

**Change:**

```csharp
// 🔄 MODIFY - Add RepomixXml color
private static Color GetColorForType(RecentFileType type)
{
    return type switch
    {
        RecentFileType.Plan => Color.Goldenrod,
        RecentFileType.Guide => Color.SteelBlue,
        RecentFileType.GitMd => Color.OrangeRed,
        RecentFileType.TestResultsMd => Color.MediumSeaGreen,
        RecentFileType.CodebaseMd => Color.MediumPurple,
        RecentFileType.CodebaseDocx => Color.LightSkyBlue,
        RecentFileType.CodebasePdf => Color.LightCoral,
        RecentFileType.RepomixXml => Color.DeepSkyBlue, // ✅ NEW - Bright blue for AI-optimized exports
        RecentFileType.Unknown => Color.Gainsboro,
        _ => Color.Gainsboro
    };
}
```

**Why:** Provides visual distinction for Repomix XML exports in the recent files grid.

***

## 📄 Full File Versions and Artifacts available

Modified files and Artifacts:

1. `RecentFileType.cs` (enum update)
2. `RepomixHandler.cs` (NEW file)
3. `MainForm.Designer.cs` (menu item)
4. `MainForm.MenuActions.cs` (handler)
5. `MainForm.Core.cs` (event wiring)
6. `RepomixInstallHelpForm.cs` (NEW help dialog)
7. `RecentFilesPanel.DataBinding.cs` (color mapping)
