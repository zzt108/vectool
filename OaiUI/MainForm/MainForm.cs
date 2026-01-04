// File: OaiUI/MainForm.cs

namespace Vectool.OaiUI
{
    /// <summary>
    /// MainForm: WinForms UI for VecTool. Refactored into partials for better maintainability.
    /// All implementation moved to focused partial classes:
    /// - MainForm.Core.cs: Constructor, fields, initialization, event wiring
    /// - MainForm.MenuActions.cs: Menu item click handlers
    /// - MainForm.VectorStoreManagement.cs: Vector store CRUD operations
    /// - MainForm.GitOperations.cs: Git-related helper methods
    /// - MainForm.Configuration.cs: Utility/helper methods (filename sanitization)
    /// </summary>
    public partial class MainForm : Form
    {
    }
}
