using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using oaiUI.Config;
using VecTool.Configuration;

namespace Vectool.OaiUI
{
    /// <summary>
    /// Main form partial: Settings tab logic for per-vector-store exclusions.
    /// </summary>
    public partial class MainForm : Form
    {
        // Initializes the Settings tab combo items with known vector store names.
        private void SettingsTabInitializeData()
        {
            try
            {
                var all = VectorStoreConfig.LoadAll();
                var names = all?.Keys?
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>();

                cmbSettingsVectorStore.Items.Clear();
                if (names.Count > 0)
                {
                    cmbSettingsVectorStore.Items.AddRange(names.Cast<object>().ToArray());
                }
            }
            catch
            {
                // Defensive: ignore load failures to avoid blocking the UI.
            }
        }

        // Loads the effective settings for a given vector store name into the UI.
        private void SettingsTabLoadSelection(string? name)
        {
            var global = VectorStoreConfig.FromAppConfig();
            var all = VectorStoreConfig.LoadAll();

            if (string.IsNullOrWhiteSpace(name))
            {
                txtExcludedFiles.Text = string.Empty;
                txtExcludedFolders.Text = string.Empty;

                chkInheritExcludedFiles.Checked = true;
                chkInheritExcludedFolders.Checked = true;

                txtExcludedFiles.Enabled = false;
                txtExcludedFolders.Enabled = false;
                return;
            }

            all.TryGetValue(name, out var per);
            var vm = PerVectorStoreSettings.From(name, global, per);

            // Inherit checkboxes are inverse of "use custom".
            chkInheritExcludedFiles.Checked = !vm.UseCustomExcludedFiles;
            chkInheritExcludedFolders.Checked = !vm.UseCustomExcludedFolders;

            txtExcludedFiles.Text = string.Join(Environment.NewLine, vm.CustomExcludedFiles);
            txtExcludedFolders.Text = string.Join(Environment.NewLine, vm.CustomExcludedFolders);

            txtExcludedFiles.Enabled = vm.UseCustomExcludedFiles;
            txtExcludedFolders.Enabled = vm.UseCustomExcludedFolders;
        }

        private void chkInheritExcludedFilesCheckedChanged(object? sender, EventArgs e)
        {
            txtExcludedFiles.Enabled = !chkInheritExcludedFiles.Checked;
        }

        private void chkInheritExcludedFoldersCheckedChanged(object? sender, EventArgs e)
        {
            txtExcludedFolders.Enabled = !chkInheritExcludedFolders.Checked;
        }

        // Implemented: load settings for the selected vector store.
        private void cmbSettingsVectorStoreSelectedIndexChanged(object? sender, EventArgs e)
        {
            var selectedName = cmbSettingsVectorStore.SelectedItem?.ToString();
            SettingsTabLoadSelection(selectedName);
        }

        private void btnSaveVsSettingsClick(object? sender, EventArgs e)
        {
            var name = (cmbSettingsVectorStore.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    "Please enter or select a vector store name.",
                    "Settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var global = VectorStoreConfig.FromAppConfig();
            var all = VectorStoreConfig.LoadAll();

            var files = SplitLines(txtExcludedFiles.Text);
            var folders = SplitLines(txtExcludedFolders.Text);

            var vm = new PerVectorStoreSettings(
                name,
                useCustomExcludedFiles: !chkInheritExcludedFiles.Checked,
                useCustomExcludedFolders: !chkInheritExcludedFolders.Checked,
                customExcludedFiles: files,
                customExcludedFolders: folders
            );

            PerVectorStoreSettings.Save(all, vm, global);
            VectorStoreConfig.SaveAll(all);

            MessageBox.Show(
                "Settings saved.",
                "Settings",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnResetVsSettingsClick(object? sender, EventArgs e)
        {
            chkInheritExcludedFiles.Checked = true;
            chkInheritExcludedFolders.Checked = true;

            var global = VectorStoreConfig.FromAppConfig();

            txtExcludedFiles.Text = string.Join(
                Environment.NewLine,
                global.ExcludedFiles ?? new List<string>());

            txtExcludedFolders.Text = string.Join(
                Environment.NewLine,
                global.ExcludedFolders ?? new List<string>());

            txtExcludedFiles.Enabled = false;
            txtExcludedFolders.Enabled = false;
        }

        private static List<string> SplitLines(string text)
        {
            return (text ?? string.Empty)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Wire up Settings tab once UI is created.
            SettingsTabInitializeData();

            cmbSettingsVectorStore.SelectedIndexChanged += cmbSettingsVectorStoreSelectedIndexChanged;
            chkInheritExcludedFiles.CheckedChanged += chkInheritExcludedFilesCheckedChanged;
            chkInheritExcludedFolders.CheckedChanged += chkInheritExcludedFoldersCheckedChanged;
        }
    }
}
