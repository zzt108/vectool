// ✅ FULL FILE VERSION
// Path: OaiUI/MainForm.VectorStoreManagement.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using VecTool.Configuration;

namespace Vectool.UI
{
    public partial class MainForm
    {
        /// <summary>
        /// Reloads all vector store configurations from disk, refreshes combo boxes,
        /// and attempts to preserve the current selections. This is the missing piece
        /// that caused the CS0103 error.
        /// </summary>
        private void ReloadAllVectorStoreConfigs()
        {
            // 1. Remember what was selected before we nuke the lists.
            var priorMainSelection = comboBoxVectorStores?.SelectedItem?.ToString();
            var priorSettingsSelection = cmbSettingsVectorStore?.Text?.Trim();

            // 2. Load the latest and greatest from the JSON file.
            // If the file is gone or empty, we start with a fresh dictionary.
            var loadedConfigs = VectorStoreConfig.LoadAll()
                                ?? new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);

            // 3. Replace the shared dictionary with the new data.
            allVectorStoreConfigs = new Dictionary<string, VectorStoreConfig>(loadedConfigs, StringComparer.OrdinalIgnoreCase);

            // 4. Get a sorted list of names for a consistent UI.
            var allNamesSorted = allVectorStoreConfigs.Keys
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // 5. Rebind the main vector store combo box.
            RebindComboBox(comboBoxVectorStores, allNamesSorted, priorMainSelection);

            // 6. Rebind the settings tab's vector store combo box.
            RebindComboBox(cmbSettingsVectorStore, allNamesSorted, priorSettingsSelection);
        }

        /// <summary>
        /// A DRY helper to avoid repeating this logic. Clears, repopulates, and
        /// tries to restore the selection of a ComboBox.
        /// </summary>
        private void RebindComboBox(ComboBox? comboBox, List<string> items, string? priorSelection)
        {
            if (comboBox == null) return;

            comboBox.Items.Clear();
            comboBox.Items.AddRange(items.Cast<object>().ToArray());

            if (!string.IsNullOrEmpty(priorSelection) && items.Contains(priorSelection))
            {
                comboBox.SelectedItem = priorSelection;
            }
            else if (items.Count > 0)
            {
                // Fallback to the first item if the old one is gone.
                comboBox.SelectedIndex = 0;
            }
        }
    }
}
