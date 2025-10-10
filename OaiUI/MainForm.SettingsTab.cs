// ✅ FULL FILE VERSION
// Path: OaiUI/MainForm.SettingsTab.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using VecTool.Configuration;
using Vectool.UI.Config;

namespace Vectool.UI
{
    public partial class MainForm
    {
        // Wire up in designer: cmbSettingsVectorStore, chkInheritExcludedFiles, chkInheritExcludedFolders,
        // txtExcludedFiles, txtExcludedFolders, btnSaveVsSettings

        private void SettingsTab_Initialize()
        {
            // Optional: call this from constructor after InitializeComponent()
            // to ensure events are hooked once.
            cmbSettingsVectorStore.SelectedIndexChanged -= cmbSettingsVectorStore_SelectedIndexChanged;
            cmbSettingsVectorStore.SelectedIndexChanged += cmbSettingsVectorStore_SelectedIndexChanged;

            chkInheritExcludedFiles.CheckedChanged -= chkInheritExcludedFiles_CheckedChanged;
            chkInheritExcludedFiles.CheckedChanged += chkInheritExcludedFiles_CheckedChanged;

            chkInheritExcludedFolders.CheckedChanged -= chkInheritExcludedFolders_CheckedChanged;
            chkInheritExcludedFolders.CheckedChanged += chkInheritExcludedFolders_CheckedChanged;

            // If needed, initial population can happen elsewhere after configs are loaded.
        }

        private void cmbSettingsVectorStore_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var selected = cmbSettingsVectorStore?.SelectedItem?.ToString();
            SettingsTabLoadSelection(selected);
        }

        private void chkInheritExcludedFiles_CheckedChanged(object? sender, EventArgs e)
        {
            // When inheriting, disable the custom list editor.
            txtExcludedFiles.Enabled = !chkInheritExcludedFiles.Checked;
        }

        private void chkInheritExcludedFolders_CheckedChanged(object? sender, EventArgs e)
        {
            // When inheriting, disable the custom list editor.
            txtExcludedFolders.Enabled = !chkInheritExcludedFolders.Checked;
        }

        private void SettingsTabLoadSelection(string? name)
        {
            var selected = (name ?? string.Empty).Trim();
            var global = VectorStoreConfig.FromAppConfig();

            allVectorStoreConfigs.TryGetValue(selected, out var perConfigOrNull);

            var vm = PerVectorStoreSettings.From(
                selected,
                global,
                perConfigOrNull);

            // Bind to controls
            if (!string.Equals(cmbSettingsVectorStore.Text, selected, StringComparison.Ordinal))
            {
                cmbSettingsVectorStore.Text = selected;
            }

            chkInheritExcludedFiles.Checked = !vm.UseCustomExcludedFiles;
            chkInheritExcludedFolders.Checked = !vm.UseCustomExcludedFolders;

            txtExcludedFiles.Text = string.Join(Environment.NewLine, vm.CustomExcludedFiles ?? Enumerable.Empty<string>());
            txtExcludedFolders.Text = string.Join(Environment.NewLine, vm.CustomExcludedFolders ?? Enumerable.Empty<string>());

            // Respect enabled state based on inheritance toggles
            txtExcludedFiles.Enabled = !chkInheritExcludedFiles.Checked;
            txtExcludedFolders.Enabled = !chkInheritExcludedFolders.Checked;
        }

        private void btnSaveVsSettings_Click(object? sender, EventArgs e)
        {
            var name = (cmbSettingsVectorStore?.Text ?? string.Empty).Trim();

            var vm = new PerVectorStoreSettings(
                name: name, // fixed named parameter from 'vectorStoreName' -> 'name'
                useCustomExcludedFiles: !chkInheritExcludedFiles.Checked,
                useCustomExcludedFolders: !chkInheritExcludedFolders.Checked,
                customExcludedFiles: SplitLines(txtExcludedFiles.Text),
                customExcludedFolders: SplitLines(txtExcludedFolders.Text));

            var global = VectorStoreConfig.FromAppConfig();

            // Persist in-memory and to disk
            PerVectorStoreSettings.Save(allVectorStoreConfigs, vm, global);
            VectorStoreConfig.SaveAll(allVectorStoreConfigs);

            // Refresh UI sources and selection
            ReloadAllVectorStoreConfigs();
            SettingsTabLoadSelection(name);
        }

        private static IEnumerable<string>? SplitLines(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            return text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
