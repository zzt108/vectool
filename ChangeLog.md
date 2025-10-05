## Changelog

### VecTool v1.25.1005 – 2025-10-05

#### Breaking Changes

*   **Feature Removed:** DocX export feature and associated handlers removed from the codebase. This includes removal of DocX-specific handlers, tests, and UI elements.
*   **Feature Removed:** PDF export feature and associated handlers removed from the codebase. All PDF-related code, including QuestPDF integration, has been eliminated.
*   **Feature Removed:** OpenAI Vector Store management feature removed. All OAI-specific handlers, UI components, and configuration related to vector store operations have been removed.

#### Architecture Refactoring

*   **Enhancement:** Major project restructuring with new modular architecture. Introduced separate projects: `Configuration`, `Constants`, `Core`, `Handlers`, `RecentFiles`, and `Utils` for better separation of concerns. (VecTool.sln)
*   **Enhancement:** Implemented `VersionInfo` utility for centralized version reporting and assembly metadata management. (Constants/VersionInfo.cs)
*   **Enhancement:** Added `RepoLocator` for intelligent Git repository root detection across multiple folder selections. (Core/RepoLocator.cs)
*   **Refactor:** Consolidated UI state management into `UiStateConfig` with JSON-backed persistence for Recent Files layout preferences including column widths and row height scaling. (Configuration/UiStateConfig.cs)
*   **Refactor:** Introduced `ISettingsStore` abstraction with `InMemorySettingsStore` implementation for testable configuration management. (Configuration/InMemorySettingsStore.cs, Configuration/ISettingsStore.cs)
*   **Refactor:** Implemented `IAppSettingsReader` interface to decouple configuration reading from `System.Configuration` for improved testability. (Configuration/IAppSettingsReader.cs, Configuration/ConfigurationManagerAppSettingsReader.cs)

#### Recent Files System

*   **Feature:** Added comprehensive Recent Files tracking system with file type categorization (`RecentFileType` enum includes Docx, Md, Pdf, GitChanges, TestResults). (RecentFiles/RecentFileType.cs)
*   **Feature:** Implemented `RecentFilesPanel` UI control with drag-and-drop support, filtering, and persistence. (RecentFiles/RecentFilesPanel.cs)
*   **Feature:** Added `DatedFileWriter` for organizing generated files into dated subdirectories with automatic cleanup of expired files. (RecentFiles/DatedFileWriter.cs)
*   **Enhancement:** Recent Files now supports vector store linking with filters (`VectorStoreLinkFilter` enum: All, Linked, Unlinked, SpecificStore). (Core/RecentFiles/VectorStoreLinkFilter.cs)
*   **Enhancement:** Added `RecentFilesConfig` with validation for max count, retention days, and output path configuration. (Configuration/RecentFilesConfig.cs)
*   **Testing:** Comprehensive unit tests added for Recent Files manager including drag-drop scenarios and file existence validation. (UnitTests/RecentFilesManagerTests.cs, UnitTests/RecentFilesPanelTests.cs)

#### New Features

*   **Feature:** Added "Run Unit Tests" functionality with `Ctrl+T` shortcut. Executes `dotnet test` programmatically and saves results to dated output files. (Handlers/TestRunnerHandler.cs, MainForm.RunTests.cs)
*   **Feature:** Implemented Progress Bar system with EMA (Exponential Moving Average) rate calculation for accurate time-remaining estimates. (Progress/ProgressManager.cs, Progress/ProgressPanel.cs)
*   **UI Improvement:** Added About dialog accessible via menu. (MainForm.About.cs, OaiUI/AboutForm.cs)
*   **UI Improvement:** Menu system expanded with File menu containing Convert to MD, Get Git Changes, File Size Summary, Run Tests, and Exit options. (MainForm.Designer.cs)

#### Code Quality & Testing

*   **Enhancement:** Introduced VecTool.Constants library to eliminate magic strings with centralized `Tags`, `Attributes`, `TagBuilder`, and `TestStrings` classes. (Constants/Tags.cs, Constants/Attributes.cs, Constants/TagBuilder.cs, Constants/TestStrings.cs)
*   **Enhancement:** Added `TagBuilder` utility with proper XML attribute escaping for safe tag construction. (Constants/TagBuilder.cs)
*   **Testing:** Added architecture tests for Constants library ensuring consistent naming and no magic strings. (UnitTests/Constants/ConstantsArchitectureTests.cs)
*   **Testing:** Added unit tests for Progress Manager with fake clock implementation for deterministic testing. (UnitTests/Progress/ProgressManagerTests.cs)
*   **Testing:** Added tests for per-vector-store settings round-trip serialization. (UnitTests/Config/PerVectorStoreSettingsTests.cs)
*   **Enhancement:** Git operations now include timeout handling and proper cancellation token support. (Core/GitRunner.cs)

#### Configuration & Architecture

*   **Enhancement:** Introduced `PerVectorStoreSettings` for per-store configuration of custom files and exclusion patterns. (Configuration/VectorStoreConfig.cs)
*   **Enhancement:** Added `LastSelectionService` for persisting user's last selected vector store across sessions. (Services/LastSelectionService.cs)
*   **Enhancement:** Improved `FileHandlerBase` with delegation to specialized helpers: `AiContextGenerator` and `FileSystemTraverser` following Single Responsibility Principle. (Handlers/FileHandlerBase.cs)
*   **Configuration:** Updated app.config with new `recentFilesMaxCount`, `recentFilesRetentionDays`, and `recentFilesOutputPath` settings. (App.config)

#### Documentation & Planning

*   **Documentation:** Added comprehensive Gap Analysis Report for "Run Unit Tests" feature documenting implementation status, gaps, and next steps. (current plan.md)
*   **Documentation:** Updated README.md to reflect new architecture, removed legacy features, and clarify that core features work without API keys. (README.md)
*   **Documentation:** Added Constants library README explaining architecture, usage guidelines, and integration patterns. (Constants/README.md)

#### Build & DevOps

*   **Configuration:** Added VS Code launch configuration for debugging Vectool UI. (.vscode/launch.json)
*   **Configuration:** Updated solution file with new project structure including Configuration, Constants, Core, Handlers, RecentFiles, and Utils projects. (VecTool.sln)

### VecTool v1.25.0412 – 2025-04-12

*   **Enhancement:** Added a new "Get Git Changes" button to the main form. This feature allows users to retrieve and save Git changes from selected folders. (MainForm.cs, MainForm.Designer.cs)
*   **Enhancement:** Implemented a new `GitChangesHandler` class to process Git changes and generate a Markdown file with the changes. (GitChangesHandler.cs)
*   **UI Improvement:** Updated the main form layout to accommodate the new Git changes feature. (MainForm.Designer.cs)
*   **Testing:** Added new unit tests for PDF conversion functionality. (ConvertSelectedFoldersToPdfTests.cs)
*   **Dependencies:** Updated `QuestPDF` NuGet package to version 2025.1.2. (oaiUI.csproj, DocXHandler.csproj)

### VecTool v1.25.0308

*   **Enhancement:** Improved file exclusion logic in `FileHandlerBase` class to support wildcard patterns. (FileHandlerBase.cs)
*   **Testing:** Enhanced unit tests for DOCX and Markdown conversion, adding more comprehensive assertions and edge cases. (ConvertSelectedFoldersToDocxTests.cs, ConvertSelectedFoldersToMDTests.cs)

### VecTool v1.25.0215

*   **Feature:** Implemented PDF conversion functionality using QuestPDF library. (PdfHandler.cs)
*   **UI Improvement:** Added a "Convert to PDF" button in the main form. (MainForm.cs, MainForm.Designer.cs)
*   **Enhancement:** Updated `README.md` with detailed information on new features, configuration, and usage instructions.
*   **Refactor:** Updated `mdTags.json` to include more default tags.

### VecTool v1.25.0102

*   **Enhancement:** Implemented automatic saving and loading of folder associations with vector stores. (MainForm.cs, LoadVectorStoreFolderData.cs, SaveVectorStoreFolderData.cs)
*   **Feature:** Added support for deleting vector store associations. (MainForm.cs)
*   **UI Improvement:** Added a "Delete Folder Associations" button in the main form. (MainForm.Designer.cs)
*   **Configuration:** Introduced `vectorStoreFoldersPath` setting in `app.config` to configure the path for the `vectorStoreFolders.json` file.

### VecTool v1.25.0101

*   **Enhancement:** Improved vector store loading logic to prioritize local file data over OpenAI data. (MainForm.cs)
*   **Refactor:** Updated file upload process to handle binary files separately. (UploadFiles.cs)
*   **Enhancement:** Implemented more robust error handling and logging in file upload process. (UploadFiles.cs)
*   **Configuration:** Added support for excluded folders in `app.config`. (MainForm.cs)

### VecTool v0.24.12.31

*   **Fix:** Delete VS Association button now correctly removes the VS name from the combobox.
*   **Fix:** Delete VS Association button now correctly clears the selected folders listbox.
*   **Fix:** Upload now correctly adds a new VS name to the combobox.
*   **Fix:** Upload now correctly selects a new VS name in the combobox.
*   **Fix:** Upload now correctly clears the new VS name textbox.
*   **Enhancement:** Vector stores are now loaded from both OpenAI and the local file, prioritizing the local file and removing any OpenAI entries that are in the file.
*   **Dependencies:** Updated `NUnit3TestAdapter` NuGet package to version 4.6.0.
*   **Dependencies:** Updated `Microsoft.NET.Test.Sdk` NuGet package to version 17.12.0.
*   **Dependencies:** Updated `NUnit.Analyzers` NuGet package to version 4.4.0.

### VecTool v0.24.12.25c

*   **Enhancement:** Improved the logic for clearing selected folders. Clearing the selected folders now correctly removes the folder association with the currently selected vector store.
*   **Internal:** Refactored the `GetVectorStoreName` method in `MainForm.cs` for better clarity and to ensure consistent updating of the vector store combobox.
*   **Internal:** Updated the `btnSelectFolders_Click` method to ensure that folder associations are correctly saved when new folders are selected.
*   **Testing:** Added more robust assertions in `ConvertSelectedFoldersToDocxTests.cs` to verify the content and structure of the generated DOCX and Markdown files.
*   **Testing:** Added new unit tests in `FileHandlerBaseTests.cs` to specifically test the `IsFileExcluded` method with various wildcard scenarios.

### VecTool v0.24.12.25b

*   **Enhancement:** If an extension is not found in `mdTags.json`, the extension itself is now used as the tag in Markdown output (e.g., `.xyz` will use `xyz` as the tag).
*   **Refactor:** Updated `mdTags.json` to include more default tags.

### VecTool v0.24.12.25

*   **Feature:** Added support for PowerShell (`.ps1`, `.psm1`) and Jupyter/Polyglot (`.ipynb`) files.
*   **Feature:** Implemented automatic saving and loading of folder associations with vector stores. When a vector store is selected, the folders previously used with it are automatically loaded. This association is stored in a configurable JSON file (`vectorStoreFolders.json`).
*   **Feature:** Added the ability to clear the list of selected folders using the "Empty Selected" button. This also removes the folder association with the current vector store.
*   **Enhancement:** Selected folders are now remembered for each vector store, improving workflow when switching between vector stores.
*   **Enhancement:** When creating a new vector store or selecting an existing one, the combobox is updated to reflect the current selection, ensuring the UI is synchronized with the application state.
*   **Fix:** Corrected an issue where new vector store names were not being consistently added to the combobox.
*   **UI Improvement:** Added a "Convert to single file" section with buttons to convert selected folders to a single DOCX or MD file.
*   **UI Improvement:** Added a progress bar and status labels to provide feedback on upload progress and other operations.
*   **Documentation:** Updated `README.md` with detailed information on new features, configuration, and usage instructions.
*   **Configuration:** Introduced `vectorStoreFoldersPath` setting in `app.config` to configure the path for the `vectorStoreFolders.json` file.
*   **Configuration:** Added `excludedFolders` setting in `app.config` to specify folders to exclude from processing.
*   **Internal:** Refactored code related to vector store selection and creation for better clarity and maintainability.
*   **Internal:** Improved logging with NLog, particularly around file deletion and vector store operations.
*   **Testing:** Added new unit tests to cover the `ConvertSelectedFoldersToDocx` and Markdown export functionalities.
*   **Dependencies:** Updated `OpenAI-DotNet` NuGet package to version 8.4.1.

### Previous Changes

*   **Feature:** Allows users to select multiple folders for processing.
*   **Feature:** Automatically exports the content of selected folders (including subfolders) into individual `.docx` files, with folder and file tags.
*   **Feature:** Enables management of vector stores, including selection, creation, and deletion of associated files.
*   **Feature:** Implements file upload functionality to vector stores, with the option to replace existing files.
*   **Feature:** Handles binary files separately during the upload process.
*   **Feature:** Utilizes MIME types for correct file format identification.
*   **Feature:** Provides an option to export the content of selected folders to a single Markdown (`.md`) file.
*   **Feature:** Allows users to configure a list of excluded files in `app.config`.
*   **Feature:** Includes comprehensive logging using NLog for debugging and monitoring.
*   **Configuration:** Requires configuration of the OpenAI API key in the `.openai` file.
*   **Configuration:** Introduces `excludedFiles` setting in `app.config` for specifying files to exclude from processing.

### Planned (Todo)

*   Store last upload date for vector stores.
*   Reviewing/Editing configuration settings on the Settings tab
*   Store configured values with each vector store data (exclusions, etc)
*   Export to PDF format, probably convert DOCX-es to PDF?