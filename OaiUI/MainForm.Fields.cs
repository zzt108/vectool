// File: OaiUI/MainForm.Fields.cs

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VecTool.Configuration;

namespace Vectool.UI
{
    public partial class MainForm : Form
    {
        // Shared UI/session state used across MainForm partials

        // Folders selected by the user via FolderSelection flows
        private readonly List<string> selectedFolders = new();

        // UI abstraction used by partials (status updates, dialogs, etc.)
        // Kept as dynamic to avoid hard dependency on specific UI adapter types in this partial
        private dynamic userInterface = null!;

        // Recent files manager abstraction used by partials
        // Kept as dynamic for the same reason as userInterface
        private dynamic recentFilesManager = null!;

        /// <summary>
        /// Shared in-memory cache of vector store configurations across MainForm partials.
        /// Keys are vector store names (case-insensitive for better UX).
        /// </summary>
        private Dictionary<string, VectorStoreConfig> allVectorStoreConfigs
            = new Dictionary<string, VectorStoreConfig>(StringComparer.OrdinalIgnoreCase);

    }
}
