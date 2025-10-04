// File: OaiUI/MainForm.SettingsTab.cs
using oaiUI.Config;
using VecTool.Configuration;
using VecTool.Handlers;

namespace Vectool.OaiUI
{
    public partial class MainForm:Form
    {
        private void SettingsTab_InitializeData()
        {
            try
            {
                var all = VectorStoreConfig.LoadAll();
                var names = all.Keys.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
                cmbSettingsVectorStore.Items.Clear();
                cmbSettingsVectorStore.Items.AddRange(names.Cast<object>().ToArray());
            }
            catch
            {
                // Defensive: ignore load failures to not block UI
            }
        }

        private void SettingsTab_LoadSelection(string? name)
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
            var vm = PerVectorStoreSettings.From(name!, global, per);

            chkInheritExcludedFiles.Checked = !vm.UseCustomExcludedFiles;
            chkInheritExcludedFolders.Checked = !vm.UseCustomExcludedFolders;

            txtExcludedFiles.Text = string.Join(Environment.NewLine, vm.CustomExcludedFiles);
            txtExcludedFolders.Text = string.Join(Environment.NewLine, vm.CustomExcludedFolders);

            txtExcludedFiles.Enabled = vm.UseCustomExcludedFiles;
            txtExcludedFolders.Enabled = vm.UseCustomExcludedFolders;
        }

        private void chkInheritExcludedFiles_CheckedChanged(object? sender, EventArgs e)
        {
            txtExcludedFiles.Enabled = !chkInheritExcludedFiles.Checked;
        }

        private void chkInheritExcludedFolders_CheckedChanged(object? sender, EventArgs e)
        {
            txtExcludedFolders.Enabled = !chkInheritExcludedFolders.Checked;
        }

        private void btnSaveVsSettings_Click(object? sender, EventArgs e)
        {
            var name = (cmbSettingsVectorStore.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter or select a vector store name.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            MessageBox.Show("Settings saved.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnResetVsSettings_Click(object? sender, EventArgs e)
        {
            chkInheritExcludedFiles.Checked = true;
            chkInheritExcludedFolders.Checked = true;
            var global = VectorStoreConfig.FromAppConfig();
            txtExcludedFiles.Text = string.Join(Environment.NewLine, global.ExcludedFiles ?? new List<string>());
            txtExcludedFolders.Text = string.Join(Environment.NewLine, global.ExcludedFolders ?? new List<string>());
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
            // Wire up once UI is created
            SettingsTab_InitializeData();
            cmbSettingsVectorStore.SelectedIndexChanged += cmbSettingsVectorStore_SelectedIndexChanged;
            chkInheritExcludedFiles.CheckedChanged += chkInheritExcludedFiles_CheckedChanged;
            chkInheritExcludedFolders.CheckedChanged += chkInheritExcludedFolders_CheckedChanged;
        }
    }
}
