## Changelog

### VecTool v1.25.0412

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