// ✅ FULL FILE VERSION
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using VecTool.Core.RecentFiles;

namespace Vectool.UI.RecentFiles
{
    /// <summary>
    /// Displays and filters recent files in a DataGridView with a right-click context menu.
    /// Emits events for filter changes, file selection, and remove requests so the host form can orchestrate actions.
    /// </summary>
    public sealed class RecentFilesControl : UserControl
    {
        private readonly DataGridView grid = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        private readonly ComboBox cbFilter = new()
        {
            Dock = DockStyle.Top,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        private readonly TextBox tbStoreId = new()
        {
            Dock = DockStyle.Top,
            Visible = false,
            PlaceholderText = "Store ID (for SpecificStore filter)"
        };

        // Context menu fields
        private readonly ContextMenuStrip contextMenu;
        private readonly ToolStripMenuItem menuOpenFile;
        private readonly ToolStripMenuItem menuOpenFolder;
        private readonly ToolStripMenuItem menuCopyPath;
        private readonly ToolStripSeparator menuSeparator;
        private readonly ToolStripMenuItem menuDelete;

        // TODO: implement VectorFilterChanged
        public Action<VectorStoreLinkFilter, string?> VectorFilterChanged { get; internal set; }

        // Public events
        public event Action<VectorStoreLinkFilter, string?>? FilterChanged;
        public event Action<string>? FileSelected;
        public event Action<string>? RemoveRequested;

        public RecentFilesControl()
        {
            // Columns
            grid.AutoGenerateColumns = false;
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(RecentFileItem.Path),
                HeaderText = "Path",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            // Removed non-existent LastAccessUtc column.

            // Events
            grid.CellDoubleClick += OnGridCellDoubleClick;
            grid.CellMouseDown += OnGridCellMouseDown;
            grid.MouseDown += OnGridMouseDown;
            grid.MouseUp += OnGridMouseUpRightClick; 

            // Context menu
            contextMenu = new ContextMenuStrip();
            menuOpenFile = new ToolStripMenuItem("Open File", null, OnOpenFile);
            menuOpenFolder = new ToolStripMenuItem("Open Containing Folder", null, OnOpenFolder);
            menuCopyPath = new ToolStripMenuItem("Copy Path", null, OnCopyPath);
            menuSeparator = new ToolStripSeparator();
            menuDelete = new ToolStripMenuItem("Remove from List", null, OnDelete);
            contextMenu.Items.Clear();
            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                menuOpenFile,
                menuOpenFolder,
                menuCopyPath,
                menuSeparator,
                menuDelete
            });
            // This order is critical:
            contextMenu.Opening += OnContextMenuOpening;

            // MUST come after the above
            grid.ContextMenuStrip = contextMenu;

            // Fallback: Explicitly show context menu on right-click if automatic doesn't work
            grid.CellMouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                {
                    // This ensures the context menu shows even if ContextMenuStrip fails
                    var mousePos = grid.PointToClient(Cursor.Position);
                    contextMenu.Show(grid, mousePos);
                }
            };

            // Filter UI
            cbFilter.Items.AddRange(new object[]
            {
                VectorStoreLinkFilter.All,
                VectorStoreLinkFilter.Linked,
                VectorStoreLinkFilter.Unlinked,
                VectorStoreLinkFilter.SpecificStore
            });
            cbFilter.SelectedIndex = 0;
            cbFilter.SelectedIndexChanged += (_, _) => RaiseFilterChanged();
            tbStoreId.TextChanged += (_, _) => RaiseFilterChanged();

            // Layout
            Controls.Add(grid);
            Controls.Add(tbStoreId);
            Controls.Add(cbFilter);
        }

        private void OnGridMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            var hit = grid.HitTest(e.X, e.Y);
            if (hit.RowIndex >= 0 && hit.RowIndex < grid.Rows.Count)
            {
                if (!grid.Rows[hit.RowIndex].Selected)
                {
                    grid.ClearSelection();
                    grid.Rows[hit.RowIndex].Selected = true;
                }

                var col = Math.Max(0, hit.ColumnIndex);
                if (col >= 0 && col < grid.Columns.Count)
                    grid.CurrentCell = grid.Rows[hit.RowIndex].Cells[col];
            }
        }
        private void OnGridMouseUpRightClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            var hit = grid.HitTest(e.X, e.Y);
            // Only show when over a valid row, avoids header/empty area surprises
            if (hit.RowIndex >= 0 && !contextMenu.Visible)
            {
                var clientPos = new System.Drawing.Point(e.X, e.Y);
                contextMenu.Show(grid, clientPos);
            }
        }

        public void SetFilter(VectorStoreLinkFilter filter, string? storeId)
        {
            cbFilter.SelectedItem = filter;
            tbStoreId.Text = storeId ?? string.Empty;
            tbStoreId.Visible = filter == VectorStoreLinkFilter.SpecificStore;
        }

        public void Bind(IReadOnlyList<RecentFileItem> items)
        {
            grid.DataSource = items?.ToList() ?? new List<RecentFileItem>();
        }

        private void RaiseFilterChanged()
        {
            var filter = cbFilter.SelectedItem is VectorStoreLinkFilter f ? f : VectorStoreLinkFilter.All;
            var storeId = filter == VectorStoreLinkFilter.SpecificStore ? NormalizeStoreId(tbStoreId.Text) : null;
            tbStoreId.Visible = filter == VectorStoreLinkFilter.SpecificStore;
            FilterChanged?.Invoke(filter, storeId);
        }

        private static string? NormalizeStoreId(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var trimmed = input.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        private void OnGridCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (grid.Rows[e.RowIndex].DataBoundItem is RecentFileItem item && !string.IsNullOrWhiteSpace(item.Path))
            {
                FileSelected?.Invoke(item.Path);
            }
        }

        private void OnGridCellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            if (e.RowIndex < 0) return; // ignore header
            if (e.RowIndex >= grid.Rows.Count)
            {
                grid.ClearSelection();
                return;
            }

            if (!grid.Rows[e.RowIndex].Selected)
            {
                grid.ClearSelection();
                grid.Rows[e.RowIndex].Selected = true;
            }

            var col = Math.Max(0, e.ColumnIndex);
            if (col >= 0 && col < grid.Columns.Count)
                grid.CurrentCell = grid.Rows[e.RowIndex].Cells[col];
        }

        private void OnContextMenuOpening(object? sender, CancelEventArgs e)
        {
            var sel = grid.SelectedRows;
            var hasSelection = sel != null && sel.Count > 0;

            // If nothing selected, cancel to avoid an empty menu appearing
            if (!hasSelection)
            {
                e.Cancel = true;
                return;
            }

            var item = GetSelectedItem();
            var fileExists = item != null && !string.IsNullOrWhiteSpace(item.Path) && File.Exists(item.Path);

            menuOpenFile.Enabled = fileExists;
            menuOpenFolder.Enabled = hasSelection && !string.IsNullOrWhiteSpace(item?.Path);
            menuCopyPath.Enabled = hasSelection;
            menuDelete.Enabled = hasSelection;
        }

        private void OnOpenFile(object? sender, EventArgs e)
        {
            var item = GetSelectedItem();
            if (item == null || string.IsNullOrWhiteSpace(item.Path) || !File.Exists(item.Path)) return;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = item.Path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnOpenFolder(object? sender, EventArgs e)
        {
            var item = GetSelectedItem();
            if (item == null || string.IsNullOrWhiteSpace(item.Path)) return;

            try
            {
                var directory = Path.GetDirectoryName(item.Path);
                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = directory,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCopyPath(object? sender, EventArgs e)
        {
            var item = GetSelectedItem();
            if (item == null || string.IsNullOrWhiteSpace(item.Path)) return;

            try
            {
                Clipboard.SetText(item.Path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy path: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnDelete(object? sender, EventArgs e)
        {
            var item = GetSelectedItem();
            if (item == null) return;

            var result = MessageBox.Show(
                $"Remove '{Path.GetFileName(item.Path)}' from the Recent Files list?",
                "Confirm Remove",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                RemoveRequested?.Invoke(item.Path);
            }
        }

        private RecentFileItem? GetSelectedItem()
        {
            if (grid.SelectedRows.Count == 0) return null;
            return grid.SelectedRows[0].DataBoundItem as RecentFileItem;
        }
    }
}
