// ✅ FULL FILE VERSION
// Path: Vectool.UI/MainForm.SettingsTab.cs
// File: MainForm.SettingsTab.cs

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Vectool.UI.Config;
using VecTool.Configuration;
// using LogCtx; // Uncomment if LogCtx is initialized in the hosting app

namespace Vectool.UI
{
    public partial class MainForm : Form
    {
        // NOTE:
        // - This file assumes the following members exist elsewhere in the partial class:
        //   - private Dictionary<string, VectorStoreConfig> allVectorStoreConfigs;
        //   - private void ReloadAllVectorStoreConfigs();
        //   - ComboBox cmbSettingsVectorStore;
        //   - TextBox txtExcludedFiles, txtExcludedFolders;
        //   - CheckBox chkInheritExcludedFiles, chkInheritExcludedFolders;
        //   - Button btnSaveVsSettings, btnResetVsSettings;

        // ============================================
        // Settings Tab: Load selection into the editor
        // ============================================
        private void SettingsTabLoadSelection(string? name)
        {
            // Detach handlers while performing programmatic updates to avoid reentrancy and clobbering
            chkInheritExcludedFiles.CheckedChanged -= chkInheritExcludedFilesCheckedChanged;
            chkInheritExcludedFolders.CheckedChanged -= chkInheritExcludedFoldersCheckedChanged;

            try
            {
                var selected = (name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(selected))
                {
                    // Nothing selected – reset UI to inherited state
                    chkInheritExcludedFiles.Checked = true;
                    chkInheritExcludedFolders.Checked = true;

                    txtExcludedFiles.Text = string.Empty;
                    txtExcludedFolders.Text = string.Empty;

                    txtExcludedFiles.Enabled = false;
                    txtExcludedFolders.Enabled = false;

                    return;
                }

                var global = VectorStoreConfig.FromAppConfig();
                allVectorStoreConfigs.TryGetValue(selected, out var per);

                var vm = PerVectorStoreSettings.From(selected, global, per);

                // Reflect inheritance toggles
                chkInheritExcludedFiles.Checked = !vm.UseCustomExcludedFiles;
                chkInheritExcludedFolders.Checked = !vm.UseCustomExcludedFolders;

                // Reflect custom lists
                txtExcludedFiles.Text = string.Join(Environment.NewLine, vm.CustomExcludedFiles ?? Enumerable.Empty<string>());
                txtExcludedFolders.Text = string.Join(Environment.NewLine, vm.CustomExcludedFolders ?? Enumerable.Empty<string>());

                // Enable editors only if custom list is used
                txtExcludedFiles.Enabled = vm.UseCustomExcludedFiles;
                txtExcludedFolders.Enabled = vm.UseCustomExcludedFolders;

                // Log (optional)
                // LogCtx.Log.Info("Settings loaded for {store}", selected);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Failed to load settings: {ex.Message}",
                    "Settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                // Log (optional)
                // LogCtx.Log.Error(ex, "Error in SettingsTabLoadSelection");
            }
            finally
            {
                // Reattach handlers
                chkInheritExcludedFiles.CheckedChanged += chkInheritExcludedFilesCheckedChanged;
                chkInheritExcludedFolders.CheckedChanged += chkInheritExcludedFoldersCheckedChanged;
            }
        }

        // ============================================
        // UI Event Handlers
        // ============================================
        private void cmbSettingsVectorStoreSelectedIndexChanged(object? sender, EventArgs e)
        {
            var name = (cmbSettingsVectorStore.Text ?? string.Empty).Trim();
            SettingsTabLoadSelection(name);
        }

        private void chkInheritExcludedFilesCheckedChanged(object? sender, EventArgs e)
        {
            // When inheritance is ON, editor is disabled; otherwise enabled
            var inherit = chkInheritExcludedFiles.Checked;
            txtExcludedFiles.Enabled = !inherit;

            // If turning inheritance ON, do not clear text – the Save path determines final persisted state
        }

        private void chkInheritExcludedFoldersCheckedChanged(object? sender, EventArgs e)
        {
            // When inheritance is ON, editor is disabled; otherwise enabled
            var inherit = chkInheritExcludedFolders.Checked;
            txtExcludedFolders.Enabled = !inherit;

            // If turning inheritance ON, do not clear text – the Save path determines final persisted state
        }

        private void btnSaveVsSettingsClick(object? sender, EventArgs e)
        {
            var name = (cmbSettingsVectorStore.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    this,
                    "Please select or enter a Vector Store name before saving.",
                    "Settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                var global = VectorStoreConfig.FromAppConfig();

                // Build the ViewModel from current UI state
                var useCustomFiles = !chkInheritExcludedFiles.Checked;
                var useCustomFolders = !chkInheritExcludedFolders.Checked;

                var files = useCustomFiles
                    ? SplitLines(txtExcludedFiles.Text)
                    : new List<string>();

                var folders = useCustomFolders
                    ? SplitLines(txtExcludedFolders.Text)
                    : new List<string>();

                var vm = new PerVectorStoreSettings(
                    vectorStoreName: name,
                    useCustomExcludedFiles: useCustomFiles,
                    useCustomExcludedFolders: useCustomFolders,
                    customExcludedFiles: files,
                    customExcludedFolders: folders);

                // Persist to the shared in-memory dictionary (do NOT create a new local dict)
                PerVectorStoreSettings.Save(allVectorStoreConfigs, vm, global);

                // Persist to disk
                VectorStoreConfig.SaveAll(allVectorStoreConfigs);

                // Align in-memory state with disk to keep all tabs/views consistent
                ReloadAllVectorStoreConfigs();

                MessageBox.Show(
                    this,
                    "Settings saved.",
                    "Settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Log (optional)
                // LogCtx.Log.Info("Settings saved for {store}", name);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    $"Failed to save settings: {ex.Message}",
                    "Settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                // Log (optional)
                // LogCtx.Log.Error(ex, "Error in btnSaveVsSettingsClick for {store}", name);
            }
        }

        private void btnResetVsSettingsClick(object? sender, EventArgs e)
        {
            // Reset UI to inherited defaults (does not persist until Save is pressed)
            chkInheritExcludedFiles.Checked = true;
            chkInheritExcludedFolders.Checked = true;

            txtExcludedFiles.Text = string.Empty;
            txtExcludedFolders.Text = string.Empty;

            txtExcludedFiles.Enabled = false;
            txtExcludedFolders.Enabled = false;

            // Log (optional)
            // LogCtx.Log.Info("Settings reset to inherited state for editor only");
        }

        // ============================================
        // Helpers
        // ============================================
        private static List<string> SplitLines(string? text)
        {
            return (text ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
