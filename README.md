
```markdown
# VecTool

VecTool is a C#-based desktop application designed to streamline the process of managing and exporting code projects for use with Large Language Models (LLMs). While it originally focused on OpenAI vector stores, its most valuable features now are the ability to \*\*export entire projects to single files\*\* digestible efficiently by AI and \*\*generate AI-ready output of Git changes\*\* for generating commit comments with AI.

### Planned Features (TODO)

- Done: Add recent generated files list with drag n drop functionality
- Done: Review/Edit configuration settings on the Settings tab.
- Done: Store configured values with each project (exclusions, etc.).
- Utilize perplexity API to get git commit message
- Add ability to drag to the recent documents grid docs from the file system
- Add console commands (git log, build, dotnet test, etc) with redirection to a text file, and addint to the recent file list
- Link recent files entries to the active vector store on creation
- Add ability to filter the recent documents grid docs by vector store, sort by coluns, etc.
- Git repo branch should be part of the vector store linking key.
- **Progress Bar Enhancements** - **Need VS Code styling**

1. **Error Handling UI System**
2. **VS Code Theme System**
3. there should be a menu system to access get git changes, docx, md, pdf, filesize button functionality, etc. (they are important for the recent file list tab also )

## Table of Contents

- \[Key Features](#key-features)
- \[No API Key Required for Core Features](#no-api-key-required-for-core-features)
- \[Installation](#installation)
- \[Usage](#usage)
- \[Configuration](#configuration)
- \[Architecture](#architecture)
- \[Recent Changes](#recent-changes)
- \[Contributing](#contributing)
- \[License](#license)

---

## Key Features

### Single File Project Export (Recommended)

- \*\*Convert to Markdown:\*\* Export your entire project or selected folders to a single, well-formatted `.md` file that's perfect for uploading to any LLM chat interface.
- \*\*Code-Aware Formatting:\*\* Automatically detects file types and applies appropriate syntax highlighting tags in the exported files.
- \*\*Hierarchical Organization:\*\* Maintains your project's folder structure in the exported file for easy navigation.

### Git Changes Integration (Added April 12, 2025)

- \*\*Get Git Changes:\*\* Retrieve and save Git changes from selected folders into a single, well-formatted Markdown file.
- \*\*AI-Assisted Commit Messages:\*\* Includes a configurable AI prompt to analyze Git changes and provide concise, descriptive commit messages.
- \*\*Comprehensive Diff Information:\*\* Captures both status changes and detailed diffs for complete context.

### Enhanced File Handling (Updated March 8, 2025)

- \*\*Improved Exclusion Logic:\*\* Support for wildcard patterns when excluding files and folders.
- \*\*Comprehensive Content Processing:\*\* Handles various file types appropriately in the exported documents.

### Run Unit Tests (Added October 5, 2025)

- \*\*Execute Tests from UI:\*\* Run `dotnet test` programmatically from the main menu with `Ctrl+T` shortcut.
- \*\*Automatic Results Tracking:\*\* Generated test output files are automatically added to Recent Files with Git branch context.
- \*\*EMA-Based Progress Reporting:\*\* Real-time progress bar with accurate time-remaining estimates using Exponential Moving Average.

### Recent Files System (Added October 5, 2025)

- \*\*Drag-and-Drop Support:\*\* Recent Files panel with full drag-drop support for easy file sharing with external tools.
- \*\*Advanced Filtering:\*\* Filter by file type (Markdown, Git Changes, Test Results) and vector store linkage.
- \*\*Automatic Cleanup:\*\* Configurable retention policy (default: 30 days) with automatic purge of expired files.
- \*\*Persistent UI State:\*\* Column widths, row height scaling, and filter preferences are saved across sessions.

### Legacy Features (Removed October 5, 2025)

The following features have been \*\*removed\*\* as part of the October 2025 refactoring:

- \*\*DOCX Export:\*\* Previously allowed conversion to Word documents. Use Markdown export instead.
- \*\*PDF Export:\*\* Previously used QuestPDF library for PDF generation. Use Markdown export instead.
- \*\*OpenAI Vector Store Management:\*\* Direct OpenAI vector store upload/management has been removed. Use single-file export with any LLM platform.

---

## No API Key Required for Core Features

The most useful features of VecTool — \*\*single file export\*\* and \*\*Git changes\*\* — do not require any API keys and can be used with any LLM platform that supports file uploads, including:

- ChatGPT
- Claude
- Gemini
- Perplexity
- Any other chat interface that allows document uploads

---

## Installation

### Prerequisites

- \*\*.NET 8.0 SDK\*\* or later
- \*\*Git\*\* (for Git Changes feature)
- \*\*Windows OS\*\* (WinForms application)

### Build from Source

1. Clone the repository:

```

git clone https://github.com/your-repo/VecTool.git
cd VecTool

```

2. Restore dependencies and build:

```

dotnet restore
dotnet build --configuration Release

```

3. Run the application:

```

dotnet run --project OaiUI/Vectool.UI.csproj

```

### Publish as Single Executable

```

dotnet publish OaiUI/Vectool.UI.csproj -r win-x64 -c Release -p:PublishSingleFile=true --self-contained true

```

The output executable can be found in `OaiUI/bin/Release/net8.0-windows/win-x64/publish`.

---

## Usage

1. \*\*Select Folders:\*\* Use the "Select Folders" button to choose directories for processing.
2. \*\*Export to Single File:\*\* Use the "Convert to single file" section to generate Markdown files from selected folders.
3. \*\*Retrieve Git Changes:\*\* Click the "Get Git Changes" button to analyze and save Git changes from selected folders.
4. \*\*Run Unit Tests:\*\* Use the menu item `File > Run Tests` or press `Ctrl+T` to execute tests and track results.
5. \*\*Upload to LLM:\*\* Take the generated single file and upload it to your preferred LLM chat interface for analysis, questions, or code review.

---

## Configuration

VecTool uses several configuration files:

### app.config

Contains application-level settings, including:

- \*\*excludedFiles\*\* and \*\*excludedFolders:\*\* Optional lists for excluding specific files and folders from processing (supports wildcards).
- \*\*gitAiPrompt:\*\* Configurable AI prompt for generating commit messages from Git changes.
- \*\*recentFilesMaxCount:\*\* Maximum number of recent files to track (default: 200).
- \*\*recentFilesRetentionDays:\*\* Number of days to retain recent files before automatic cleanup (default: 30).
- \*\*recentFilesOutputPath:\*\* Directory for generated output files (default: "Generated").

### Config/LogConfig.xml

Configuration file for NLog, allowing customization of logging behavior.

### MimeTypes\*.json

JSON files defining MIME types, file extensions for processing, and Markdown tags for code blocks.

### uiState.json

Stores UI layout preferences (column widths, row height scaling, last selected vector store, Recent Files filter state). Automatically saved on application exit.

---

## Architecture

VecTool follows a \*\*modular, SOLID-compliant architecture\*\* with clear separation of concerns:

### Project Structure

- \*\*Vectool.UI\*\* (`OaiUI/`): Main WinForms UI layer, MainForm, progress panels, and user controls.
- \*\*Configuration\*\* (`Configuration/`): App.config abstraction, settings stores, UI state persistence.
- \*\*Constants\*\* (`Constants/`): Centralized tag names, attributes, and string builders to eliminate magic strings.
- \*\*Core\*\* (`Core/`): Business logic including Git operations, file system traversal, and repo locators.
- \*\*Handlers\*\* (`Handlers/`): Feature handlers (Markdown export, Git changes, test runner) implementing Command pattern.
- \*\*RecentFiles\*\* (`RecentFiles/`): Recent Files manager, panel UI, and dated file writer with automatic cleanup.
- \*\*Utils\*\* (`Utils/`): Utility classes including MIME type detection, file helpers, and version info.
- \*\*UnitTests\*\* (`UnitTests/`): Comprehensive NUnit + Shouldly test suite covering all layers.

### Key Design Patterns

- \*\*Dependency Injection:\*\* Interfaces (`ISettingsStore`, `IAppSettingsReader`, `IGitRunner`) for testability.
- \*\*Strategy Pattern:\*\* File handlers delegate to specialized helpers (`AiContextGenerator`, `FileSystemTraverser`).
- \*\*Command Pattern:\*\* Feature handlers encapsulate operations (`TestRunnerHandler`, `GitChangesHandler`).
- \*\*Observer Pattern:\*\* Progress Manager with EMA-based rate calculation publishes updates to UI.

### Testing Philosophy

- \*\*NUnit\*\* for test framework.
- \*\*Shouldly\*\* for fluent assertions.
- \*\*Architecture tests\*\* ensure Constants library consistency and no magic strings leak.
- \*\*Fake implementations\*\* (`InMemorySettingsStore`, `FakeClock`) for deterministic tests.

---

## Recent Changes

### VecTool v1.25.1005 – October 5, 2025

#### Breaking Changes

- \*\*Removed DOCX Export:\*\* All DocX-related handlers, tests, and UI elements removed.
- \*\*Removed PDF Export:\*\* QuestPDF integration and all PDF-related code eliminated.
- \*\*Removed OpenAI Vector Store Management:\*\* Direct OAI vector store features removed; use single-file export instead.

#### New Features

- \*\*Run Unit Tests:\*\* New menu item with `Ctrl+T` shortcut executes `dotnet test` and saves results to dated files.
- \*\*Recent Files System:\*\* Comprehensive tracking with drag-drop, filtering by type/store, and automatic cleanup.
- \*\*Progress Manager:\*\* EMA-based time estimation for accurate progress reporting.
- \*\*Constants Library:\*\* Project-wide elimination of magic strings with centralized `Tags`, `Attributes`, and `TagBuilder` classes.

#### Architecture Improvements

- \*\*Modular Refactor:\*\* Split into 7+ projects (Configuration, Constants, Core, Handlers, RecentFiles, Utils, UI).
- \*\*Configuration Abstraction:\*\* Introduced `ISettingsStore`, `IAppSettingsReader` for testability.
- \*\*UI State Persistence:\*\* JSON-backed storage for layout preferences with validation.
- \*\*Git Operations:\*\* Enhanced with timeout handling and proper cancellation token support.

#### Documentation

- \*\*Gap Analysis Report:\*\* Comprehensive report for "Run Unit Tests" feature documenting implementation status and next steps.
- \*\*Constants README:\*\* Added architecture, usage guidelines, and integration patterns for Constants library.
- \*\*VS Code Integration:\*\* Added launch configuration for debugging.

For full changelog, see \[ChangeLog.md](ChangeLog.md).

---

## Contributing

Contributions to the VecTool project are welcome! If you have ideas for improvements, new features, or bug fixes, please feel free to fork the repository and submit a pull request.

---

## License

This project is licensed under the \*\*Apache License 2.0\*\*. See the \[LICENSE](LICENSE) file for details.

---

## Support

For questions, issues, or feature requests, please open an issue on the GitHub repository.
```

## **Summary of README.md Updates**

1. **Removed references** to DOCX and PDF export features[^1](VectoolDev.feature_versioning.md)
2. **Removed references** to OpenAI Vector Store management (now labeled "Legacy Features - Removed")[^1](VectoolDev.feature_versioning.md)
3. **Added "Run Unit Tests" feature** section[^1](VectoolDev.feature_versioning.md)
4. **Added "Recent Files System" feature** section with drag-drop and filtering[^1](VectoolDev.feature_versioning.md)
5. **Added "Recent Changes" section** with v1.25.1005 summary[^1](VectoolDev.feature_versioning.md)
6. **Updated Architecture section** reflecting modular refactor (7+ projects)[^1](VectoolDev.feature_versioning.md)
7. **Added Configuration section** with `recentFilesMaxCount`, `recentFilesRetentionDays`, `recentFilesOutputPath`[^1](VectoolDev.feature_versioning.md)
8. **Added mention of `uiState.json`** for UI layout persistence[^1](VectoolDev.feature_versioning.md)
9. **VS Code integration** mentioned in documentation section[^1](VectoolDev.feature_versioning.md)



