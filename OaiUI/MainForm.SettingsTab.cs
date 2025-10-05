// ✅ FULL FILE VERSION
// Path: OaiUI/MainForm.SettingsTab.cs

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
        /// <summary>
        /// Initializes the Settings tab combo items with known vector store names.
        /// </summary>
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

        /// <summary>
        /// Loads the effective settings for a given vector store name into the UI.
        /// </summary>
        private void SettingsTabLoadSelection(string? name)
        {
            var global = VectorStoreConfig.FromAppConfig();
            var all = VectorStoreConfig.LoadAll();

            if (string.IsNullOrWhiteSpace(name))
            {
                // Show global defaults
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

            // Inherit checkboxes are inverse of "use custom"
            chkInheritExcludedFiles.Checked = !vm.UseCustomExcludedFiles;
            chkInheritExcludedFolders.Checked = !vm.UseCustomExcludedFolders;

            txtExcludedFiles.Text = string.Join(Environment.NewLine, vm.CustomExcludedFiles);
            txtExcludedFolders.Text = string.Join(Environment.NewLine, vm.CustomExcludedFolders);

            txtExcludedFiles.Enabled = vm.UseCustomExcludedFiles;
            txtExcludedFolders.Enabled = vm.UseCustomExcludedFolders;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Wire up Settings tab once UI is created
            SettingsTabInitializeData();
        }

        private static List<string> SplitLines(string text)
        {
            return (text ?? string.Empty)
                .Replace("\r", "", StringComparison.Ordinal)
                .Split('\n')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
