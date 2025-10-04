// File: OaiUI/MainForm.LastSelection.cs

using System;
using System.Linq;
using VecTool.Configuration;

namespace Vectool.OaiUI
{
    // Partial to isolate UI-state concerns without touching existing logic blocks
    public partial class MainForm
    {
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Ensure handlers are attached exactly once after UI is ready
            comboBoxVectorStores.SelectedIndexChanged += (_, __) => PersistCurrentVectorStoreSelection();

            // Apply the last-known selection if present in the list
            TrySelectLastUsedVectorStore();
        }

        private void TrySelectLastUsedVectorStore()
        {
            var state = UiStateConfig.Load();
            var last = state.LastSelectedVectorStore;

            if (string.IsNullOrWhiteSpace(last))
            {
                return;
            }

            // Try exact match first
            var index = comboBoxVectorStores.FindStringExact(last);
            if (index >= 0)
            {
                comboBoxVectorStores.SelectedIndex = index;
                return;
            }

            // If exact not found, try case-insensitive manual search
            for (int i = 0; i < comboBoxVectorStores.Items.Count; i++)
            {
                var itemText = comboBoxVectorStores.Items[i]?.ToString();
                if (string.Equals(itemText, last, StringComparison.OrdinalIgnoreCase))
                {
                    comboBoxVectorStores.SelectedIndex = i;
                    return;
                }
            }

            // Otherwise, keep whatever the loader chose (likely index 0)
        }

        private void PersistCurrentVectorStoreSelection()
        {
            var selectedName = comboBoxVectorStores.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedName))
            {
                return;
            }

            UiStateConfig.Save(new UiStateConfig
            {
                LastSelectedVectorStore = selectedName
            });
        }
    }
}
