// ✅ FULL FILE VERSION
// Path: src/VecTool.UI/OaiUI/MainForm.VectorStoreManagement.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using VecTool.Configuration;
using VecTool.Handlers;

namespace Vectool.UI
{
    public partial class MainForm : Form
    {
        // Moves from MainForm.cs: vector store CRUD, selection, and title updates.
        // No functional changes; method bodies are preserved.

        // Loads all vector store configs into the combo box and restores last selection.
        private void LoadVectorStoresIntoComboBox()
        {
            try
            {
                allVectorStoreConfigs = VectorStoreConfig.LoadAll()
                    ?? new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);

                var names = allVectorStoreConfigs.Keys
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                comboBoxVectorStores.Items.Clear();

                if (names.Any())
                {
                    comboBoxVectorStores.Items.AddRange(names.Cast<object>().ToArray());

                    // restore last selected vector store if present
                    var last = lastSelection.GetLastSelectedVectorStore();
                    if (!string.IsNullOrWhiteSpace(last))
                    {
                        var idx = names.FindIndex(n => string.Equals(n, last, StringComparison.Ordinal));
                        comboBoxVectorStores.SelectedIndex = idx >= 0 ? idx : 0;
                    }
                    else
                    {
                        comboBoxVectorStores.SelectedIndex = 0;
                    }
                }
                else
                {
                    // No vector stores available; clear UI lists and title
                    comboBoxVectorStores.Items.Clear();
                    selectedFolders.Clear();
                    listBoxSelectedFolders.Items.Clear();
                    UpdateFormTitle();
                }
            }
            catch (Exception ex)
            {
                // Defensive: do not crash UI on load
                userInterface.ShowMessage($"Failed to load vector stores: {ex.Message}", "Warning", MessageType.Warning);
            }
        }

        // Saves the currently selected vector store's folder list to disk.
        private void SaveChangesToCurrentVectorStore()
        {
            var currentVsName = comboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(currentVsName) || !allVectorStoreConfigs.ContainsKey(currentVsName))
            {
                return;
            }

            allVectorStoreConfigs[currentVsName].FolderPaths = selectedFolders.ToList();
            VectorStoreConfig.SaveAll(allVectorStoreConfigs);
        }

        // Handles combo selection changes: updates title, list, and persists the last selection.
        private void comboBoxVectorStoresSelectedIndexChanged(object? sender, EventArgs e)
        {
            var selectedName = comboBoxVectorStores.SelectedItem?.ToString();
            UpdateFormTitle();

            if (string.IsNullOrEmpty(selectedName) || !allVectorStoreConfigs.ContainsKey(selectedName))
            {
                // Clear selection if invalid; persist cleared state
                selectedFolders.Clear();
                listBoxSelectedFolders.Items.Clear();
                lastSelection.SetLastSelectedVectorStore(null);
                return;
            }

            var config = allVectorStoreConfigs[selectedName];

            selectedFolders.Clear();
            selectedFolders.AddRange(config.FolderPaths ?? Enumerable.Empty<string>());

            listBoxSelectedFolders.Items.Clear();
            listBoxSelectedFolders.Items.AddRange(selectedFolders.Cast<object>().ToArray());

            // Persist valid selection
            lastSelection.SetLastSelectedVectorStore(selectedName);
        }

        // Creates a new empty vector store from global defaults and selects it.
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
                userInterface.ShowMessage($"A vector store named {newName} already exists.", "Duplicate Name", MessageType.Warning);
                return;
            }

            var newConfig = VectorStoreConfig.FromAppConfig();
            newConfig.FolderPaths = new List<string>();

            allVectorStoreConfigs[newName] = newConfig;
            VectorStoreConfig.SaveAll(allVectorStoreConfigs);

            // Refresh and select the new store
            LoadVectorStoresIntoComboBox();
            comboBoxVectorStores.SelectedItem = newName;
            txtNewVectorStoreName.Clear();

            userInterface.ShowMessage($"Vector store {newName} created.", "Success", MessageType.Information);
            UpdateFormTitle();
        }

        // Returns the active vector store config or falls back to application defaults.
        private VectorStoreConfig GetCurrentVectorStoreConfig()
        {
            var selectedName = comboBoxVectorStores.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(selectedName) &&
                allVectorStoreConfigs.TryGetValue(selectedName, out var config))
            {
                return config;
            }

            return VectorStoreConfig.FromAppConfig();
        }

        // Updates the window title to include the current vector store.
        private void UpdateFormTitle()
        {
            var selectedName = comboBoxVectorStores.SelectedItem?.ToString();
            this.Text = string.IsNullOrEmpty(selectedName) ? "VecTool" : $"VecTool - {selectedName}";
        }
    }
}
