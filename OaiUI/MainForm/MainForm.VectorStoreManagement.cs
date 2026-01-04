// ✅ FULL FILE VERSION
// File: OaiUI/MainForm.VectorStoreManagement.cs

using VecTool.Configuration;
using VecTool.Handlers;

namespace Vectool.OaiUI
{
    /// <summary>
    /// MainForm partial: Vector store management (load, create, select, save).
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// Loads all vector stores into the combo box and restores last selection.
        /// </summary>
        private void LoadVectorStoresIntoComboBox()
        {
            try
            {
                allVectorStoreConfigs = VectorStoreConfig.LoadAll() ?? new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);

                var names = allVectorStoreConfigs.Keys
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                comboBoxVectorStores.Items.Clear();

                if (names.Any())
                {
                    comboBoxVectorStores.Items.AddRange(names.Cast<object>().ToArray());

                    // ✅ Restore last selected vector store if present
                    var last = lastSelection.GetLastSelectedVectorStore();
                    if (!string.IsNullOrWhiteSpace(last))
                    {
                        var idx = names.FindIndex(n => string.Equals(n, last, StringComparison.Ordinal));
                        if (idx >= 0)
                        {
                            comboBoxVectorStores.SelectedIndex = idx;
                        }
                        else
                        {
                            comboBoxVectorStores.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        comboBoxVectorStores.SelectedIndex = 0;
                    }
                }
                else
                {
                    comboBoxVectorStores.Items.Clear();
                    selectedFolders.Clear();
                    listBoxSelectedFolders.Items.Clear();
                    UpdateFormTitle();
                }

                UpdateFormTitle();
            }
            catch (Exception ex)
            {
                userInterface.ShowMessage($"Failed to load vector stores: {ex.Message}", "Warning", MessageType.Warning);
            }
        }

        /// <summary>
        /// Handles vector store selection change in combo box.
        /// </summary>
        private void comboBoxVectorStoresSelectedIndexChanged(object? sender, EventArgs e)
        {
            var selectedName = comboBoxVectorStores.SelectedItem?.ToString();
            UpdateFormTitle();

            if (string.IsNullOrEmpty(selectedName) || !allVectorStoreConfigs.ContainsKey(selectedName))
            {
                selectedFolders.Clear();
                listBoxSelectedFolders.Items.Clear();
                // ✅ Persist cleared selection
                lastSelection.SetLastSelectedVectorStore(null);
                return;
            }

            var config = allVectorStoreConfigs[selectedName];
            selectedFolders.Clear();
            selectedFolders.AddRange(config.FolderPaths ?? Enumerable.Empty<string>());

            listBoxSelectedFolders.Items.Clear();
            listBoxSelectedFolders.Items.AddRange(selectedFolders.Cast<object>().ToArray());

            // ✅ Persist valid selection
            lastSelection.SetLastSelectedVectorStore(selectedName);
        }

        /// <summary>
        /// Handles "Create" button click to create a new vector store.
        /// </summary>
        private void btnCreateNewVectorStoreClick(object? sender, EventArgs e)
        {
            var newName = txtNewVectorStoreName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                userInterface.ShowMessage("Please enter a name for the new vector store.", "Input Required", MessageType.Warning);
                return;
            }

            if (allVectorStoreConfigs.ContainsKey(newName))
            {
                userInterface.ShowMessage($"A vector store named '{newName}' already exists.", "Duplicate Name", MessageType.Warning);
                return;
            }

            var newConfig = VectorStoreConfig.FromAppConfig();
            newConfig.FolderPaths = new List<string>();
            allVectorStoreConfigs[newName] = newConfig;

            VectorStoreConfig.SaveAll(allVectorStoreConfigs);
            LoadVectorStoresIntoComboBox();

            comboBoxVectorStores.SelectedItem = newName;
            txtNewVectorStoreName.Clear();

            userInterface.ShowMessage($"Vector store '{newName}' created.", "Success", MessageType.Information);
            UpdateFormTitle();
        }

        /// <summary>
        /// Handles "Select Folders..." button click to add folders to current vector store.
        /// </summary>
        private void btnSelectFoldersClick(object? sender, EventArgs e)
        {
            using var folderBrowserDialog = new FolderBrowserDialog
            {
                Description = "Select a folder to add",
                ShowNewFolderButton = true
            };

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                var selectedPath = folderBrowserDialog.SelectedPath;
                if (!selectedFolders.Contains(selectedPath))
                {
                    selectedFolders.Add(selectedPath);
                    listBoxSelectedFolders.Items.Add(selectedPath);
                    SaveChangesToCurrentVectorStore();
                }
            }
        }

        /// <summary>
        /// Saves current folder selection to the currently selected vector store.
        /// </summary>
        private void SaveChangesToCurrentVectorStore()
        {
            var currentVsName = comboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(currentVsName) || !allVectorStoreConfigs.ContainsKey(currentVsName))
                return;

            allVectorStoreConfigs[currentVsName].FolderPaths = selectedFolders.ToList();
            VectorStoreConfig.SaveAll(allVectorStoreConfigs);
        }

        /// <summary>
        /// Gets the currently selected vector store config, or a default config if none selected.
        /// </summary>
        private VectorStoreConfig GetCurrentVectorStoreConfig()
        {
            var selectedName = comboBoxVectorStores.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedName) && allVectorStoreConfigs.TryGetValue(selectedName, out var config))
            {
                return config;
            }

            return VectorStoreConfig.FromAppConfig();
        }
    }
}
