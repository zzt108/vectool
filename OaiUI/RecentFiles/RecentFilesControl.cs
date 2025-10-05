using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using VecTool.Core.RecentFiles;

namespace VecTool.UI.RecentFiles
{
    // TODO: BUG: list view context menu is not working
    public sealed class RecentFilesControl : UserControl
    {
        private readonly ComboBox _cbFilter = new() { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox _tbStoreId = new() { Dock = DockStyle.Top, PlaceholderText = "Vector Store ID (when SpecificStore)" };
        private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false };

        public event Action<VectorStoreLinkFilter, string?>? VectorFilterChanged;
        public event Action<string>? FileSelected;

        public RecentFilesControl()
        {
            Controls.Add(_grid);
            Controls.Add(_tbStoreId);
            Controls.Add(_cbFilter);

            _cbFilter.Items.AddRange(new object[]
            {
                VectorStoreLinkFilter.All,
                VectorStoreLinkFilter.Linked,
                VectorStoreLinkFilter.Unlinked,
                VectorStoreLinkFilter.SpecificStore
            });
            _cbFilter.SelectedIndex = 0;

            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Path", HeaderText = "Path", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0 && _grid.Rows[e.RowIndex].DataBoundItem is RecentFileItem item)
                    FileSelected?.Invoke(item.Path);
            };

            _cbFilter.SelectedIndexChanged += (_, __) => RaiseFilterChanged();
            _tbStoreId.TextChanged += (_, __) => RaiseFilterChanged();
        }

        public void SetFilter(VectorStoreLinkFilter filter, string? storeId)
        {
            _cbFilter.SelectedItem = filter;
            _tbStoreId.Text = storeId ?? string.Empty;
        }

        public void Bind(IEnumerable<RecentFileItem> items)
        {
            _grid.DataSource = items.ToList();
        }

        private void RaiseFilterChanged()
        {
            var filter = (VectorStoreLinkFilter)_cbFilter.SelectedItem!;
            var storeId = filter == VectorStoreLinkFilter.SpecificStore ? _tbStoreId.Text?.Trim() : null;
            VectorFilterChanged?.Invoke(filter, string.IsNullOrWhiteSpace(storeId) ? null : storeId);
        }
    }
}
