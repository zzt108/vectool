## Changelog

### VecTool v4.25.1007 – 2025-10-07

#### Bug Fixes

*   **Build:** Fixed broken codebase - restored project to buildable state
*   **Testing:** Fixed ~10 unit tests (reduced failures from 35 to ~25)
*   **Testing:** Test suite stabilization work in progress

---

### VecTool v1.25.1005 – 2025-10-05

#### Breaking Changes

*   **Feature Removed:** DocX export feature and associated handlers removed from the codebase. This includes removal of DocX-specific handlers, tests, and UI elements.
*   **Feature Removed:** PDF export feature and associated handlers removed from the codebase. All PDF-related code, including QuestPDF integration, has been eliminated.
*   **Feature Removed:** OpenAI Vector Store management removed from the codebase. All OAI-specific handlers, UI components for vector store operations, and configuration related to vector store operations have been removed.

#### Architecture Changes

*   **Refactoring:** Solution modularized into 7 projects:
    *   **Vectool.UI** (OaiUI) – Main WinForms user interface layer
    *   **Configuration** – app.config abstraction, settings stores  
    *   **Constants** – Centralized tag names and XML attributes
    *   **Core** – Business logic (Git operations, file system traversal)
    *   **Handlers** – Feature handlers (Markdown export, Git changes, test runner)
    *   **RecentFiles** – Recent Files manager with drag-drop support
    *   **Utils** – Utility classes (MIME type detection, file helpers)

#### Features

*   **Core Functionality** (no API keys required):
    *   **Markdown Export** – Export entire project as single `.md` file
    *   **Git Changes Integration** – Generate AI-assisted commit messages
    *   **Run Unit Tests** – Execute `dotnet test` with `Ctrl+T` keyboard shortcut
    *   **Recent Files System** – Drag-drop support, filtering, automatic cleanup

#### Dependencies

*   **Updated:** OpenAI-DotNet NuGet package to version 8.4.1

#### Documentation

*   **Updated:** README.md to reflect removed features and current architecture
*   **Updated:** User guide sections for new modular structure

---

### VecTool v0.24.12.25 – 2024-12-25

#### New Features

*   **Feature:** Multi-folder selection support for batch processing
*   **Feature:** Automatic export of selected folders (including subfolders) to individual .docx files with folder and file tags
*   **Feature:** Vector store management - selection, creation, and deletion of associated files
*   **Feature:** File upload functionality to vector stores with option to replace existing files
*   **Feature:** Binary file handling during upload process
*   **Feature:** MIME type-based file format identification
*   **Feature:** Export selected folders to single Markdown .md file
*   **Feature:** Configurable excluded files list in app.config
*   **Feature:** Comprehensive logging with NLog and Serilog integration

#### Improvements

*   **UI:** Enhanced folder selection dialog
*   **Performance:** Optimized file processing for large directories
*   **Logging:** Structured logging with contextual information

#### Testing

*   **Added:** Unit tests for ConvertSelectedFoldersToDocx functionality
*   **Added:** Unit tests for Markdown export functionality