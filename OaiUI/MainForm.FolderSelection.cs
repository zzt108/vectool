// ✅ FULL FILE VERSION
// File: OaiUI/MainForm.FileOperations.cs

using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VecTool.RecentFiles;
// Optional structured logging if available in the solution
// using LogCtx;

namespace Vectool.UI
{
    public partial class MainForm : Form
    {
        // NOTE:
        // - This partial relies on the shared fields declared in MainForm.Fields.cs:
        //   selectedFolders (List<string>), userInterface (dynamic), recentFilesManager (dynamic)
        // - Controls like comboBoxVectorStores must be defined in the Designer partial.

        // -------------------------
        // Helpers
        // -------------------------

        // Builds a safe file name stem from any nullable/dirty input.
        private static string BuildSafeFileStem(string? name, string fallback = "unknown")
        {
            var coalesced = string.IsNullOrWhiteSpace(name) ? fallback : name;
            var sanitized = SanitizeFileName(coalesced, "-");
            return string.IsNullOrWhiteSpace(sanitized) ? fallback : sanitized;
        }

        // Makes a string safe for use as a file name by replacing invalid characters.
        private static string SanitizeFileName(string input, string replacement)
        {
            if (string.IsNullOrEmpty(replacement))
                throw new ArgumentException("Replacement must be a non-empty string.", nameof(replacement));

            var replChar = replacement[0];
            var value = input ?? string.Empty;

            foreach (var ch in Path.GetInvalidFileNameChars())
                value = value.Replace(ch, replChar);

            foreach (var ch in new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' })
                value = value.Replace(ch, replChar);

            var doubleRepl = new string(replChar, 2);
            var singleRepl = new string(replChar, 1);
            while (value.Contains(doubleRepl, StringComparison.Ordinal))
                value = value.Replace(doubleRepl, singleRepl, StringComparison.Ordinal);

            value = value.Trim(replChar, '.');

            return value;
        }

        private async Task<string> BuildDefaultFileStemAsync()
        {
            // Vector store name from combo box, branch from Git
            string vsName;
            try
            {
                vsName = BuildSafeFileStem(this.comboBoxVectorStores?.SelectedItem?.ToString());
            }
            catch
            {
                vsName = "vectorstore";
            }

            string branch = "branch";
            try
            {
                // Assumes another partial defines this method
                var b = await GetCurrentBranchNameAsync().ConfigureAwait(true);
                branch = BuildSafeFileStem(b);
            }
            catch
            {
                // Best-effort fallback
            }

            return $"{vsName}.{branch}";
        }

        // -------------------------
        // Menu/Event Handlers
        // -------------------------

        // Example: Generate Git changes and save as Markdown
        private async void getGitChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedFolders == null || selectedFolders.Count == 0)
            {
                try { userInterface?.ShowMessage("Please select at least one folder before generating Git changes.", "Info"); } catch { /* ignore */ }
                return;
            }

            // using (LogCtx.Ctx.Set(new Props().Add("operation", "GetGitChanges"))) // optional
            try
            {
                var stem = await BuildDefaultFileStemAsync().ConfigureAwait(true);
                var defaultFileName = $"{stem}.changes.md";

                // Ask for save path via UI abstraction if available, else use SaveFileDialog
                string? savePath = null;
                try
                {
                    savePath = userInterface?.PromptSaveFileName("Save Git changes", defaultFileName, "Markdown|*.md");
                }
                catch
                {
                    using var dlg = new SaveFileDialog
                    {
                        Title = "Save Git changes",
                        FileName = defaultFileName,
                        Filter = "Markdown|*.md|All files|*.*",
                        AddExtension = true,
                        OverwritePrompt = true
                    };
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        savePath = dlg.FileName;
                }

                if (string.IsNullOrWhiteSpace(savePath))
                    return;

                try { userInterface?.SetBusy(true); } catch { /* ignore */ }
                try { userInterface?.ShowStatus($"Generating Git changes for {selectedFolders.Count} folder(s)..."); } catch { /* ignore */ }

                // TODO: Replace with real implementation that inspects the selected folders and builds a change log
                var content = BuildPlaceholderGitChanges(selectedFolders);

                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                await File.WriteAllTextAsync(savePath, content).ConfigureAwait(true);

                try { recentFilesManager?.AddRecentFile(savePath, selectedFolders.ToArray()); } catch { /* ignore */ }
                try { userInterface?.ShowMessage($"Saved changes to:\n{savePath}", "Success"); } catch { /* ignore */ }
            }
            catch (Exception ex)
            {
                try { userInterface?.ShowError($"Failed to generate Git changes: {ex.Message}", "Error"); } catch { /* ignore */ }
            }
            finally
            {
                try { userInterface?.ShowStatus("Idle"); } catch { /* ignore */ }
                try { userInterface?.SetBusy(false); } catch { /* ignore */ }
            }
        }

        // Example: Convert selected folders content to a single Markdown file
        private async void convertToMdToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedFolders == null || selectedFolders.Count == 0)
            {
                try { userInterface?.ShowMessage("Please select at least one folder before converting to Markdown.", "Info"); } catch { /* ignore */ }
                return;
            }

            try
            {
                var stem = await BuildDefaultFileStemAsync().ConfigureAwait(true);
                var defaultFileName = $"{stem}.md";

                string? savePath = null;
                try
                {
                    savePath = userInterface?.PromptSaveFileName("Save Markdown", defaultFileName, "Markdown|*.md");
                }
                catch
                {
                    using var dlg = new SaveFileDialog
                    {
                        Title = "Save Markdown",
                        FileName = defaultFileName,
                        Filter = "Markdown|*.md|All files|*.*",
                        AddExtension = true,
                        OverwritePrompt = true
                    };
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        savePath = dlg.FileName;
                }

                if (string.IsNullOrWhiteSpace(savePath))
                    return;

                try { userInterface?.SetBusy(true); } catch { /* ignore */ }
                try { userInterface?.ShowStatus($"Converting {selectedFolders.Count} folder(s) to Markdown..."); } catch { /* ignore */ }

                // TODO: Replace with real implementation
                var content = BuildPlaceholderMarkdown(selectedFolders);

                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                await File.WriteAllTextAsync(savePath, content).ConfigureAwait(true);

                try { recentFilesManager?.AddRecentFile(savePath, selectedFolders.ToArray()); } catch { /* ignore */ }
                try { userInterface?.ShowMessage($"Saved Markdown to:\n{savePath}", "Success"); } catch { /* ignore */ }
            }
            catch (Exception ex)
            {
                try { userInterface?.ShowError($"Failed to convert to Markdown: {ex.Message}", "Error"); } catch { /* ignore */ }
            }
            finally
            {
                try { userInterface?.ShowStatus("Idle"); } catch { /* ignore */ }
                try { userInterface?.SetBusy(false); } catch { /* ignore */ }
            }
        }

        // Example: Produce a simple file size summary of selected folders
        private async void fileSizeSummaryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedFolders == null || selectedFolders.Count == 0)
            {
                try { userInterface?.ShowMessage("Please select at least one folder before creating a size summary.", "Info"); } catch { /* ignore */ }
                return;
            }

            try
            {
                var stem = await BuildDefaultFileStemAsync().ConfigureAwait(true);
                var defaultFileName = $"{stem}.summary.txt";

                string? savePath = null;
                try
                {
                    savePath = userInterface?.PromptSaveFileName("Save Size Summary", defaultFileName, "Text|*.txt");
                }
                catch
                {
                    using var dlg = new SaveFileDialog
                    {
                        Title = "Save Size Summary",
                        FileName = defaultFileName,
                        Filter = "Text|*.txt|All files|*.*",
                        AddExtension = true,
                        OverwritePrompt = true
                    };
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        savePath = dlg.FileName;
                }

                if (string.IsNullOrWhiteSpace(savePath))
                    return;

                try { userInterface?.SetBusy(true); } catch { /* ignore */ }
                try { userInterface?.ShowStatus($"Scanning sizes for {selectedFolders.Count} folder(s)..."); } catch { /* ignore */ }

                var lines = new List<string>();
                long total = 0;

                foreach (var folder in selectedFolders.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!Directory.Exists(folder))
                    {
                        lines.Add($"{folder} -> not found");
                        continue;
                    }

                    long size = 0;
                    try
                    {
                        size = await Task.Run(() => CalculateDirectorySize(folder)).ConfigureAwait(true);
                    }
                    catch (Exception exFolder)
                    {
                        lines.Add($"{folder} -> error: {exFolder.Message}");
                        continue;
                    }

                    total += size;
                    lines.Add($"{folder} -> {size:N0} bytes");
                }

                lines.Add(string.Empty);
                lines.Add($"TOTAL -> {total:N0} bytes");

                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
                await File.WriteAllLinesAsync(savePath, lines).ConfigureAwait(true);

                try { recentFilesManager?.AddRecentFile(savePath, selectedFolders.ToArray()); } catch { /* ignore */ }
                try { userInterface?.ShowMessage($"Saved size summary to:\n{savePath}", "Success"); } catch { /* ignore */ }
            }
            catch (Exception ex)
            {
                try { userInterface?.ShowError($"Failed to create size summary: {ex.Message}", "Error"); } catch { /* ignore */ }
            }
            finally
            {
                try { userInterface?.ShowStatus("Idle"); } catch { /* ignore */ }
                try { userInterface?.SetBusy(false); } catch { /* ignore */ }
            }
        }

        // -------------------------
        // Internal utilities
        // -------------------------

        private static string BuildPlaceholderGitChanges(IEnumerable<string> folders)
        {
            var now = DateTime.UtcNow;
            var header = $"# Git Changes Summary ({now:yyyy-MM-dd HH:mm} UTC)\n";
            var body = string.Join("\n", folders.Select(f => $"- {f} -> changes collected"));
            return $"{header}\n{body}\n";
        }

        private static string BuildPlaceholderMarkdown(IEnumerable<string> folders)
        {
            var now = DateTime.UtcNow;
            var header = $"# Combined Markdown Export ({now:yyyy-MM-dd HH:mm} UTC)\n";
            var body = string.Join("\n\n", folders.Select(f => $"## {Path.GetFileName(f)}\nContent exported from: `{f}`"));
            return $"{header}\n{body}\n";
        }

        private static long CalculateDirectorySize(string path)
        {
            long size = 0;

            try
            {
                var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        size += info.Length;
                    }
                    catch
                    {
                        // Ignore individual file issues
                    }
                }
            }
            catch
            {
                // Ignore traversal issues
            }

            return size;
        }
    }
}
