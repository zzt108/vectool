// ✅ FULL FILE VERSION
// File: OaiUI/MainForm.cs

using oaiUI;
using oaiUI.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vectool.UI.Versioning;
using VecTool.Configuration;
using VecTool.Core;
using VecTool.Handlers;
using VecTool.RecentFiles;

namespace Vectool.OaiUI
{
    /// <summary>
    /// MainForm: WinForms UI for VecTool. Refactored into partials for better maintainability.
    /// All implementation moved to focused partial classes:
    /// - MainForm.Core.cs: Constructor, fields, initialization, event wiring
    /// - MainForm.MenuActions.cs: Menu item click handlers
    /// - MainForm.VectorStoreManagement.cs: Vector store CRUD operations
    /// - MainForm.GitOperations.cs: Git-related helper methods
    /// - MainForm.Utilities.cs: Utility/helper methods (filename sanitization)
    /// </summary>
    public partial class MainForm : Form
    {
        // ✅ All implementation moved to partial classes for Single Responsibility Principle compliance.
        // ✅ See individual partials for specific method implementations.
    }
}
