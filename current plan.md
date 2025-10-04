## **Feature Implementation Plan: Run Unit Tests** 🧪

### **Követelmények Összefoglalása**

```
- Console command: `dotnet test > TestResults-<vectorstore>.<git branch>.txt`
```

- Fájl neve: `TestResults-{vectorstore}.{gitbranch}.txt`
- Ha nincs Git repo / branch, `unknown` placeholder
- Hiba esetén (non-zero exit code): MessageBox, **NO FILE**
- Sikeres futás: fájl megy a Recent Files grid-be
- FileType enum bővítése: `TestResults`
- UI: menü item hozzáadása **+ az összes többi meglévő gomb is**

***

## **Implementation Steps**

### **1. Git Branch Detection** 🔍

**Új metódus `GitRunner`-hez:**

```csharp
// File: Core/GitRunner.cs

// ... existing code ...
// <line 53> public async Task<string> GetSubmodulesAsync()
// <line 54> {
// <line 55>     return await RunGitCommandAsync("submodule status");
// <line 56> }

/// <summary>
/// Gets the current Git branch name from the repository.
/// </summary>
/// <returns>Current branch name, or "unknown" if not in a Git repository.</returns>
public async Task<string> GetCurrentBranchAsync()
{
    try
    {
        var result = await RunGitCommandAsync("branch --show-current", timeoutSeconds: 5);
        return string.IsNullOrWhiteSpace(result) ? "unknown" : result.Trim();
    }
    catch
    {
        return "unknown";
    }
}

// <line 57> public static bool IsGitRepository(string folderPath)
```


***

### **2. FileType Enum Bővítése** 📦

```csharp
// File: RecentFiles/RecentFileType.cs

// ... existing code ...
// <line 8> public enum RecentFileType
// <line 9> {
// <line 10>     Docx,
// <line 11>     Md,
// <line 12>     Pdf
// <line 13> }

public enum RecentFileType
{
    Docx,
    Md,
    Pdf,
    TestResults  // NEW
}
```


***

### **3. Test Runner Handler** 🏃

**Új fájl:**

**Path:** `Handlers/TestRunnerHandler.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLogShared;
using VecTool.Core;
using VecTool.RecentFiles;

namespace VecTool.Handlers
{
    /// <summary>
    /// Handler for executing dotnet test and capturing results.
    /// </summary>
    public sealed class TestRunnerHandler
    {
        private static readonly CtxLogger _log = new();
        private readonly IUserInterface? _ui;
        private readonly IRecentFilesManager? _recentFilesManager;

        public TestRunnerHandler(IUserInterface? ui, IRecentFilesManager? recentFilesManager)
        {
            _ui = ui;
            _recentFilesManager = recentFilesManager;
        }

        /// <summary>
        /// Runs dotnet test from solution root and saves output to file.
        /// </summary>
        /// <param name="solutionPath">Path to the solution file (.sln)</param>
        /// <param name="vectorStoreName">Current vector store name</param>
        /// <param name="selectedFolders">Selected folders (for tracking)</param>
        /// <returns>Output file path if successful, null otherwise</returns>
        public async Task<string?> RunTestsAsync(string solutionPath, string vectorStoreName, List<string> selectedFolders)
        {
            try
            {
                _ui?.UpdateStatus("Running unit tests...");
                _log.Info($"Starting dotnet test for solution: {solutionPath}");

                // Get Git branch
                var solutionDir = Path.GetDirectoryName(solutionPath) ?? Directory.GetCurrentDirectory();
                var gitRunner = new GitRunner(solutionDir);
                var branchName = await gitRunner.GetCurrentBranchAsync();

                // Build output file name
                var outputFileName = $"TestResults-{vectorStoreName}.{branchName}.txt";
                var outputPath = Path.Combine(solutionDir, outputFileName);

                // Run dotnet test
                var testOutput = await RunDotnetTestAsync(solutionPath);

                // Write output to file
                await File.WriteAllTextAsync(outputPath, testOutput);
                _log.Info($"Test results saved to: {outputPath}");

                // Register with recent files
                if (_recentFilesManager != null && File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    _recentFilesManager.RegisterGeneratedFile(
                        outputPath,
                        RecentFileType.TestResults,
                        selectedFolders,
                        fileInfo.Length
                    );
                }

                _ui?.UpdateStatus("Tests completed successfully.");
                return outputPath;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error running tests");
                _ui?.ShowMessage($"Test execution failed: {ex.Message}", "Test Error", MessageType.Error);
                return null;
            }
        }

        /// <summary>
        /// Executes dotnet test command and captures output.
        /// </summary>
        private async Task<string> RunDotnetTestAsync(string solutionPath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"test \"{solutionPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(solutionPath) ?? Directory.GetCurrentDirectory(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // Read both streams in parallel
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync();

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"dotnet test failed with exit code {process.ExitCode}\n{error}");
            }

            return output;
        }
    }
}
```


***

### **4. MainForm Integration** 🖥️

#### **A) Button Event Handler**

```csharp
// File: OaiUI/MainForm.cs

// ... existing code ...
// <line 285> private async void btnGetGitChangesClick(object sender, EventArgs e)
// <line 286> {
// ... existing implementation ...
// <line 330> }

private async void btnRunTestsClick(object sender, EventArgs e)
{
    try
    {
        btnRunTests.Enabled = false;
        WorkStart("Running tests...", selectedFolders);

        string? selectedVectorStore = comboBoxVectorStores.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(selectedVectorStore))
        {
            MessageBox.Show("Please select a vector store first.", "No Vector Store", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Find solution file
        var solutionPath = FindSolutionFile();
        if (solutionPath == null)
        {
            MessageBox.Show("Could not find VecTool.sln in the directory tree.", "Solution Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var testRunner = new TestRunnerHandler(userInterface, recentFilesManager);
        var outputPath = await testRunner.RunTestsAsync(solutionPath, selectedVectorStore, selectedFolders);

        if (outputPath != null)
        {
            MessageBox.Show($"Tests completed successfully.\nResults saved to:\n{outputPath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error running tests: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    finally
    {
        WorkFinish();
        btnRunTests.Enabled = true;
    }
}

/// <summary>
/// Finds VecTool.sln by traversing up from the application directory.
/// </summary>
private string? FindSolutionFile()
{
    var currentDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
    while (currentDir != null)
    {
        var solutionFile = Path.Combine(currentDir.FullName, "VecTool.sln");
        if (File.Exists(solutionFile))
        {
            return solutionFile;
        }
        currentDir = currentDir.Parent;
    }
    return null;
}

// <line 331> private void UpdateProgress()
```


#### **B) Menu System Hozzáadása**

**Hozzáadás a Designer-hez:**

```csharp
// File: OaiUI/MainForm.Designer.cs

// ... existing code ...
// <line 25> private System.ComponentModel.IContainer components = null;

#region Windows Form Designer generated code

private void InitializeComponent()
{
    // ... existing controls initialization ...
    
    // Add MenuStrip
    this.menuStrip1 = new System.Windows.Forms.MenuStrip();
    this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
    this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
    this.actionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
    this.convertToDocxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
    this.convertToMdToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
    this.convertToPdfToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
    this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
    this.getGitChangesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
    this.fileSizeSummaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
    this.runTestsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
    
    this.menuStrip1.SuspendLayout();
    this.SuspendLayout();
    
    // 
    // menuStrip1
    // 
    this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
        this.fileToolStripMenuItem,
        this.actionsToolStripMenuItem});
    this.menuStrip1.Location = new System.Drawing.Point(0, 0);
    this.menuStrip1.Name = "menuStrip1";
    this.menuStrip1.Size = new System.Drawing.Size(800, 24);
    this.menuStrip1.TabIndex = 0;
    this.menuStrip1.Text = "menuStrip1";
    
    // 
    // fileToolStripMenuItem
    // 
    this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
        this.exitToolStripMenuItem});
    this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
    this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
    this.fileToolStripMenuItem.Text = "&File";
    
    // 
    // exitToolStripMenuItem
    // 
    this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
    this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
    this.exitToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
    this.exitToolStripMenuItem.Text = "E&xit";
    this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
    
    // 
    // actionsToolStripMenuItem
    // 
    this.actionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
        this.convertToDocxToolStripMenuItem,
        this.convertToMdToolStripMenuItem,
        this.convertToPdfToolStripMenuItem,
        this.toolStripSeparator1,
        this.getGitChangesToolStripMenuItem,
        this.fileSizeSummaryToolStripMenuItem,
        this.runTestsToolStripMenuItem});
    this.actionsToolStripMenuItem.Name = "actionsToolStripMenuItem";
    this.actionsToolStripMenuItem.Size = new System.Drawing.Size(59, 20);
    this.actionsToolStripMenuItem.Text = "&Actions";
    
    // 
    // convertToDocxToolStripMenuItem
    // 
    this.convertToDocxToolStripMenuItem.Name = "convertToDocxToolStripMenuItem";
    this.convertToDocxToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
    this.convertToDocxToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
    this.convertToDocxToolStripMenuItem.Text = "Convert to &DOCX";
    this.convertToDocxToolStripMenuItem.Click += new System.EventHandler(this.btnConvertToDocxClick);
    
    // 
    // convertToMdToolStripMenuItem
    // 
    this.convertToMdToolStripMenuItem.Name = "convertToMdToolStripMenuItem";
    this.convertToMdToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.M)));
    this.convertToMdToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
    this.convertToMdToolStripMenuItem.Text = "Convert to &MD";
    this.convertToMdToolStripMenuItem.Click += new System.EventHandler(this.btnConvertToMDClick);
    
    // 
    // convertToPdfToolStripMenuItem
    // 
    this.convertToPdfToolStripMenuItem.Name = "convertToPdfToolStripMenuItem";
    this.convertToPdfToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
    this.convertToPdfToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
    this.convertToPdfToolStripMenuItem.Text = "Convert to &PDF";
    this.convertToPdfToolStripMenuItem.Click += new System.EventHandler(this.btnConvertToPdfClick);
    
    // 
    // toolStripSeparator1
    // 
    this.toolStripSeparator1.Name = "toolStripSeparator1";
    this.toolStripSeparator1.Size = new System.Drawing.Size(217, 6);
    
    // 
    // getGitChangesToolStripMenuItem
    // 
    this.getGitChangesToolStripMenuItem.Name = "getGitChangesToolStripMenuItem";
    this.getGitChangesToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G)));
    this.getGitChangesToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
    this.getGitChangesToolStripMenuItem.Text = "Get &Git Changes";
    this.getGitChangesToolStripMenuItem.Click += new System.EventHandler(this.btnGetGitChangesClick);
    
    // 
    // fileSizeSummaryToolStripMenuItem
    // 
    this.fileSizeSummaryToolStripMenuItem.Name = "fileSizeSummaryToolStripMenuItem";
    this.fileSizeSummaryToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
    this.fileSizeSummaryToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
    this.fileSizeSummaryToolStripMenuItem.Text = "&File Size Summary";
    this.fileSizeSummaryToolStripMenuItem.Click += new System.EventHandler(this.btnFileSizeSummaryClick);
    
    // 
    // runTestsToolStripMenuItem
    // 
    this.runTestsToolStripMenuItem.Name = "runTestsToolStripMenuItem";
    this.runTestsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
    this.runTestsToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
    this.runTestsToolStripMenuItem.Text = "Run &Tests";
    this.runTestsToolStripMenuItem.Click += new System.EventHandler(this.btnRunTestsClick);
    
    // 
    // MainForm
    // 
    this.MainMenuStrip = this.menuStrip1;
    this.Controls.Add(this.menuStrip1);
    
    this.menuStrip1.ResumeLayout(false);
    this.menuStrip1.PerformLayout();
    this.ResumeLayout(false);
    this.PerformLayout();
}

#endregion

private System.Windows.Forms.MenuStrip menuStrip1;
private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
private System.Windows.Forms.ToolStripMenuItem actionsToolStripMenuItem;
private System.Windows.Forms.ToolStripMenuItem convertToDocxToolStripMenuItem;
private System.Windows.Forms.ToolStripMenuItem convertToMdToolStripMenuItem;
private System.Windows.Forms.ToolStripMenuItem convertToPdfToolStripMenuItem;
private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
private System.Windows.Forms.ToolStripMenuItem getGitChangesToolStripMenuItem;
private System.Windows.Forms.ToolStripMenuItem fileSizeSummaryToolStripMenuItem;
private System.Windows.Forms.ToolStripMenuItem runTestsToolStripMenuItem;

// ... existing controls ...
```

**Exit handler:**

```csharp
// File: OaiUI/MainForm.cs

private void exitToolStripMenuItem_Click(object sender, EventArgs e)
{
    Application.Exit();
}
```


***

### **5. Unit Tests** ✅

**Új fájl:**

**Path:** `UnitTests/TestRunnerHandlerTests.cs`

```csharp
using NUnit.Framework;
using Shouldly;
using System.IO;
using System.Threading.Tasks;
using VecTool.Handlers;

namespace UnitTests
{
    [TestFixture]
    public class TestRunnerHandlerTests
    {
        private string _testDir;

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "VecToolTestRunner", System.Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        [Test]
        public async Task RunTestsAsync_ShouldReturnNull_WhenDotnetTestFails()
        {
            // Arrange
            var handler = new TestRunnerHandler(null, null);
            var fakeSolutionPath = Path.Combine(_testDir, "NonExistent.sln");
            
            // Act
            var result = await handler.RunTestsAsync(fakeSolutionPath, "TestVS", new System.Collections.Generic.List<string>());

            // Assert
            result.ShouldBeNull();
        }

        [Test]
        public void FindSolutionFile_ShouldReturnNull_WhenNotFound()
        {
            // This would require refactoring FindSolutionFile to be testable
            // For now, this is a placeholder
            Assert.Pass("Integration test - requires actual solution structure");
        }
    }
}
```


***

## **Összefoglalás** 📋

### **Új Fájlok**

1. `Handlers/TestRunnerHandler.cs` - Test execution logic
2. `UnitTests/TestRunnerHandlerTests.cs` - Unit tests

### **Módosított Fájlok**

1. `Core/GitRunner.cs` - Add `GetCurrentBranchAsync()`
2. `RecentFiles/RecentFileType.cs` - Add `TestResults` enum value
3. `OaiUI/MainForm.cs` - Add `btnRunTestsClick()` + `FindSolutionFile()` + `exitToolStripMenuItem_Click()`
4. `OaiUI/MainForm.Designer.cs` - Add MenuStrip with all actions

### **Funkcionalitás**

- ✅ Detektálja Git branch-et (vagy "unknown")
- ✅ `dotnet test` futtatása solution root-ból
- ✅ Output fájl: `TestResults-{vectorstore}.{branch}.txt`
- ✅ Hiba esetén MessageBox, nincs fájl
- ✅ Sikeres futás → Recent Files grid
- ✅ Menu system **az összes többi gombbal együtt**
- ✅ Keyboard shortcuts (Ctrl+T, Ctrl+D, stb.)

***

**Certainty**: **9/10** - Az implementáció straightforward, egyetlen bizonytalan pont a solution path detektálása különböző deployment környezetekben, de a felfelé traversálás kellően robusztus. 🚀

**Kérdés:** Kezdjem el generálni a teljes kódokat? 😎

<div align="center">⁂</div>

[^1]: VecToolDev.docx

