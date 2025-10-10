// Path: Vectool.UI/MainForm.cs
// File: MainForm.cs

#nullable enable

using System;
using System.Windows.Forms;
// Ensure LogCtx is available in the solution; if not initialized at startup, calls can be added later.
// using LogCtx;

namespace Vectool.UI
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            WireUpEvents(); // Centralize event wiring to avoid duplicate subscriptions across partials. 
                            // [Design: idempotent re-wiring pattern used below] 
                            // LogCtx can be added here if initialized globally. 
        }

        // Centralized, idempotent wiring for Settings tab and related controls.
        // Pattern: always "-= handler; += handler;" to prevent duplicate subscriptions after re-inits.
        private void WireUpEvents()
        {
            // SETTINGS TAB
            cmbSettingsVectorStore.SelectedIndexChanged -= cmbSettingsVectorStoreSelectedIndexChanged;
            cmbSettingsVectorStore.SelectedIndexChanged += cmbSettingsVectorStoreSelectedIndexChanged;

            chkInheritExcludedFiles.CheckedChanged -= chkInheritExcludedFilesCheckedChanged;
            chkInheritExcludedFiles.CheckedChanged += chkInheritExcludedFilesCheckedChanged;

            chkInheritExcludedFolders.CheckedChanged -= chkInheritExcludedFoldersCheckedChanged;
            chkInheritExcludedFolders.CheckedChanged += chkInheritExcludedFoldersCheckedChanged;

            btnSaveVsSettings.Click -= btnSaveVsSettingsClick;
            btnSaveVsSettings.Click += btnSaveVsSettingsClick;

            btnResetVsSettings.Click -= btnResetVsSettingsClick;
            btnResetVsSettings.Click += btnResetVsSettingsClick;

            // Optional logging if LogCtx is initialized in Program.cs
            // LogCtx.Log.Info("WireUpEvents completed for Settings tab handlers");
        }

        // Note:
        // - The actual handler methods referenced here (e.g., cmbSettingsVectorStoreSelectedIndexChanged,
        //   chkInheritExcludedFilesCheckedChanged, chkInheritExcludedFoldersCheckedChanged,
        //   btnSaveVsSettingsClick, btnResetVsSettingsClick) are implemented in other partial files:
        //   MainForm.SettingsTab.cs and/or MainForm.VectorStoreManagement.cs.
        //
        // - This file intentionally centralizes event wiring to prevent triple-subscription issues and
        //   to keep the Settings tab behavior stable during programmatic UI updates and reloads.
    }
}
