#nullable enable

using LogCtxShared;
using NLogShared;
using VecTool.Core.Models;
using System.Globalization; 

namespace VecTool.UI.Panels
{
    /// <summary>
    /// Simple editor to rename a prompt file using the standard naming convention.
    /// Lets the user edit Type / Version / Name and shows the resulting filename.
    /// </summary>
    public sealed class PromptRenameForm : Form
    {
        private static readonly CtxLogger log = new();

        private readonly PromptFile promptFile;
        private readonly string originalFullPath;
        private readonly string originalExtension;

        private readonly ComboBox cmbType;
        private readonly NumericUpDown nudVersion;
        private readonly TextBox txtName;
        private readonly TextBox txtPreview;
        private readonly TextBox txtCurrentPath;
        private readonly TextBox txtOriginalFileName;

        private readonly Button btnRename;
        private readonly Button btnCancel;

        public bool WasRenamed { get; private set; }
        public string? NewFullPath { get; private set; }

        //public PromptRenameForm(PromptFile promptFile)
        //{
        //    this.promptFile = promptFile ?? throw new ArgumentNullException(nameof(promptFile));
        //    originalFullPath = promptFile.FullPath;
        //    originalExtension = Path.GetExtension(promptFile.FullPath) ?? string.Empty;

        //    Text = "Rename Prompt File";
        //    StartPosition = FormStartPosition.CenterParent;
        //    FormBorderStyle = FormBorderStyle.FixedDialog;
        //    MaximizeBox = false;
        //    MinimizeBox = false;
        //    ClientSize = new Size(640, 260);

        //    using var ctx = LogCtx.Set(new Props()
        //        .Add("FullPath", originalFullPath)
        //        .Add("FileName", promptFile.Metadata.FileName));

        //    var table = new TableLayoutPanel
        //    {
        //        Dock = DockStyle.Fill,
        //        ColumnCount = 2,
        //        RowCount = 5,
        //        Padding = new Padding(10),
        //        AutoSize = false
        //    };
        //    table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        //    table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        //    // Current path (read-only)
        //    var lblCurrent = new Label
        //    {
        //        Text = "Current path:",
        //        AutoSize = true,
        //        TextAlign = ContentAlignment.MiddleLeft,
        //        Dock = DockStyle.Fill
        //    };
        //    txtCurrentPath = new TextBox
        //    {
        //        Dock = DockStyle.Fill,
        //        ReadOnly = true,
        //        Text = originalFullPath
        //    };

        //    // Type
        //    var lblType = new Label
        //    {
        //        Text = "Type:",
        //        AutoSize = true,
        //        TextAlign = ContentAlignment.MiddleLeft,
        //        Dock = DockStyle.Fill
        //    };
        //    txtType = new TextBox
        //    {
        //        Dock = DockStyle.Fill,
        //        Text = promptFile.Metadata.Type
        //    };

        //    // Version
        //    var lblVersion = new Label
        //    {
        //        Text = "Version:",
        //        AutoSize = true,
        //        TextAlign = ContentAlignment.MiddleLeft,
        //        Dock = DockStyle.Fill
        //    };
        //    txtVersion = new TextBox
        //    {
        //        Dock = DockStyle.Fill,
        //        Text = promptFile.Metadata.Version
        //    };

        //    // Name
        //    var lblName = new Label
        //    {
        //        Text = "Name:",
        //        AutoSize = true,
        //        TextAlign = ContentAlignment.MiddleLeft,
        //        Dock = DockStyle.Fill
        //    };
        //    txtName = new TextBox
        //    {
        //        Dock = DockStyle.Fill,
        //        Text = promptFile.Metadata.Name
        //    };

        //    // Preview
        //    var lblPreview = new Label
        //    {
        //        Text = "New filename:",
        //        AutoSize = true,
        //        TextAlign = ContentAlignment.MiddleLeft,
        //        Dock = DockStyle.Fill
        //    };
        //    txtPreview = new TextBox
        //    {
        //        Dock = DockStyle.Fill,
        //        ReadOnly = true
        //    };

        //    table.Controls.Add(lblCurrent, 0, 0);
        //    table.Controls.Add(txtCurrentPath, 1, 0);
        //    table.Controls.Add(lblType, 0, 1);
        //    table.Controls.Add(txtType, 1, 1);
        //    table.Controls.Add(lblVersion, 0, 2);
        //    table.Controls.Add(txtVersion, 1, 2);
        //    table.Controls.Add(lblName, 0, 3);
        //    table.Controls.Add(txtName, 1, 3);
        //    table.Controls.Add(lblPreview, 0, 4);
        //    table.Controls.Add(txtPreview, 1, 4);

        //    // Buttons
        //    var buttonPanel = new FlowLayoutPanel
        //    {
        //        Dock = DockStyle.Bottom,
        //        FlowDirection = FlowDirection.RightToLeft,
        //        Height = 40,
        //        Padding = new Padding(10)
        //    };

        //    btnRename = new Button
        //    {
        //        Text = "Rename",
        //        DialogResult = DialogResult.None,
        //        Width = 100,
        //        Height = 28
        //    };
        //    btnRename.Click += BtnRenameClick;

        //    btnCancel = new Button
        //    {
        //        Text = "Cancel",
        //        DialogResult = DialogResult.Cancel,
        //        Width = 100,
        //        Height = 28
        //    };
        //    btnCancel.Click += (_, _) => Close();

        //    buttonPanel.Controls.Add(btnRename);
        //    buttonPanel.Controls.Add(btnCancel);

        //    Controls.Add(table);
        //    Controls.Add(buttonPanel);

        //    AcceptButton = btnRename;
        //    CancelButton = btnCancel;

        //    txtType.TextChanged += (_, _) => UpdatePreview();
        //    txtVersion.TextChanged += (_, _) => UpdatePreview();
        //    txtName.TextChanged += (_, _) => UpdatePreview();

        //    UpdatePreview();
        //}
        public PromptRenameForm(PromptFile promptFile)
        {
            this.promptFile = promptFile ?? throw new ArgumentNullException(nameof(promptFile));
            originalFullPath = promptFile.FullPath;
            originalExtension = Path.GetExtension(promptFile.FullPath) ?? string.Empty;

            Text = "Rename Prompt File";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(640, 280);

            using var ctx = LogCtx.Set(new Props()
                .Add("FullPath", originalFullPath)
                .Add("FileName", promptFile.Metadata.FileName));

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6, // 🔄 was 5
                Padding = new Padding(10),
                AutoSize = false
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Current path (read-only)
            var lblCurrent = new Label
            {
                Text = "Current path:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            txtCurrentPath = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Text = originalFullPath
            };

            // ✅ NEW: Original file name (read-only)
            var lblOriginalFileName = new Label
            {
                Text = "Original file name:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            txtOriginalFileName = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Text = promptFile.Metadata.FileName
            };

            // Type (dropdown)
            var lblType = new Label
            {
                Text = "Type:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            cmbType = new ComboBox
            {
                Dock = DockStyle.Left,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180
            };

            // Known prompt types from naming convention/tests (PROMPT, GUIDE, SPACE, Unknown)
            var knownTypes = new[] { "PROMPT", "GUIDE", "SPACE", "Unknown" };
            cmbType.Items.AddRange(knownTypes);

            var currentType = promptFile.Metadata.Type?.Trim();
            if (!string.IsNullOrWhiteSpace(currentType))
            {
                if (!cmbType.Items.Contains(currentType))
                {
                    cmbType.Items.Add(currentType);
                }

                cmbType.SelectedItem = currentType;
            }

            if (cmbType.SelectedIndex < 0 && cmbType.Items.Count > 0)
            {
                cmbType.SelectedIndex = 0;
            }

            // Version (incrementable number)
            var lblVersion = new Label
            {
                Text = "Version:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            nudVersion = new NumericUpDown
            {
                Dock = DockStyle.Left,
                Width = 100,
                Minimum = 0M,
                Maximum = 99M,
                DecimalPlaces = 1,
                Increment = 0.1M
            };

            if (decimal.TryParse(promptFile.Metadata.Version,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var parsedVersion))
            {
                if (parsedVersion < nudVersion.Minimum)
                {
                    parsedVersion = nudVersion.Minimum;
                }
                else if (parsedVersion > nudVersion.Maximum)
                {
                    parsedVersion = nudVersion.Maximum;
                }

                nudVersion.Value = parsedVersion;
            }
            else
            {
                nudVersion.Value = 1.0M;
            }

            // Name
            var lblName = new Label
            {
                Text = "Name:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            txtName = new TextBox
            {
                Dock = DockStyle.Fill,
                Text = promptFile.Metadata.Name
            };

            // Preview
            var lblPreview = new Label
            {
                Text = "New filename:",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            };
            txtPreview = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true
            };

            // Row wiring
            table.Controls.Add(lblCurrent, 0, 0);
            table.Controls.Add(txtCurrentPath, 1, 0);

            table.Controls.Add(lblOriginalFileName, 0, 1);
            table.Controls.Add(txtOriginalFileName, 1, 1);

            table.Controls.Add(lblType, 0, 2);
            table.Controls.Add(cmbType, 1, 2);

            table.Controls.Add(lblVersion, 0, 3);
            table.Controls.Add(nudVersion, 1, 3);

            table.Controls.Add(lblName, 0, 4);
            table.Controls.Add(txtName, 1, 4);

            table.Controls.Add(lblPreview, 0, 5);
            table.Controls.Add(txtPreview, 1, 5);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(10)
            };

            btnRename = new Button
            {
                Text = "Rename",
                DialogResult = DialogResult.None,
                Width = 100,
                Height = 28
            };
            btnRename.Click += BtnRenameClick;

            btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 100,
                Height = 28
            };
            btnCancel.Click += (_, _) => Close();

            buttonPanel.Controls.Add(btnRename);
            buttonPanel.Controls.Add(btnCancel);

            Controls.Add(table);
            Controls.Add(buttonPanel);

            AcceptButton = btnRename;
            CancelButton = btnCancel;

            cmbType.SelectedIndexChanged += (_, _) => UpdatePreview();
            nudVersion.ValueChanged += (_, _) => UpdatePreview();
            txtName.TextChanged += (_, _) => UpdatePreview();

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            try
            {
                var type = (cmbType.SelectedItem as string) ?? string.Empty;
                var version = nudVersion.Value.ToString("0.0", CultureInfo.InvariantCulture);
                var name = txtName.Text;

                var candidate = PromptMetadata.BuildFileName(
                    type,
                    version,
                    name,
                    originalExtension);

                txtPreview.Text = candidate;
            }
            catch
            {
                txtPreview.Text = string.Empty;
            }
        }

        private void BtnRenameClick(object? sender, EventArgs e)
        {
            using var ctx = LogCtx.Set(new Props()
                .Add("OriginalPath", originalFullPath));

            try
            {
                var type = (cmbType.SelectedItem as string ?? string.Empty).Trim();
                var version = nudVersion.Value.ToString("0.0", CultureInfo.InvariantCulture);
                var name = (txtName.Text ?? string.Empty).Trim();

                if (type.Length == 0)
                {
                    MessageBox.Show(this, "Type is required.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (name.Length == 0)
                {
                    MessageBox.Show(this, "Name is required.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var invalidChars = Path.GetInvalidFileNameChars();
                if (name.Any(c => invalidChars.Contains(c)))
                {
                    MessageBox.Show(this, "Name contains invalid file name characters.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var newFileName = PromptMetadata.BuildFileName(type, version, name, originalExtension);
                var directory = Path.GetDirectoryName(originalFullPath) ?? ".";
                var newFullPath = Path.Combine(directory, newFileName);

                if (string.Equals(originalFullPath, newFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                    return;
                }

                if (File.Exists(newFullPath))
                {
                    MessageBox.Show(this,
                        "A file with the target name already exists.\nPlease choose a different name.",
                        "Rename blocked",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                File.Move(originalFullPath, newFullPath);

                using var _ = LogCtx.Set()
                    .Add("From", originalFullPath)
                    .Add("To", newFullPath);
                log.Info("Renamed prompt file.");

                WasRenamed = true;
                NewFullPath = newFullPath;

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to rename prompt file.");

                MessageBox.Show(this,
                    $"Failed to rename file: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
