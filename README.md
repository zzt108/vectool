## VecTool

VecTool is a C# based desktop application designed to streamline the process of managing files and interacting with OpenAI's vector stores. This tool allows users to easily select folders, process their contents, and upload these files to specific vector stores for use with OpenAI's models. It also provides functionality to manage existing vector stores and export folder contents to various formats.

### Key Features

#### Git Changes Integration (Added: April 12, 2025)

- **Get Git Changes:** A new button has been added to retrieve and save Git changes from selected folders.
- **Git Changes Handler:** Implemented a new `GitChangesHandler` class to process Git changes and generate a Markdown file with the changes.
- **AI-Assisted Commit Messages:** The tool now includes an AI prompt (configurable in `app.config`) to analyze Git changes and provide concise, descriptive commit messages.

#### PDF Conversion (Added: February 15, 2025)

- **Convert to PDF:** Users can now convert selected folders to a single PDF file.
- **PDF Handler:** A new `PdfHandler` class has been implemented to handle the conversion of folders and files to PDF format.
- **QuestPDF Integration:** The project now uses QuestPDF library for PDF generation, with the license set to Community.

#### Enhanced DOCX and Markdown Conversion (Updated: March 8, 2025)

- **Improved File Handling:** The `FileHandlerBase` class has been updated to support wildcard patterns in file exclusion logic.
- **Comprehensive Unit Tests:** Enhanced unit tests for DOCX and Markdown conversion, adding more assertions and edge cases.

#### Vector Store Management Improvements (Updated: January 1, 2025)

- **Automatic Folder Association:** The application now automatically saves and loads the association between selected folders and specific vector stores.
- **Local Data Prioritization:** When loading vector stores, the application now prioritizes local file data over OpenAI data, removing any OpenAI entries that are in the local file.

#### User Interface Enhancements (Updated: April 12, 2025)

- **Progress Tracking:** Improved progress bar and status updates for various operations.
- **New Buttons:** Added buttons for PDF conversion and Git changes retrieval.
- **Layout Updates:** The main form layout has been updated to accommodate new features.

### Configuration

- **app.config:** Contains application-level settings, including:
  - `vectorStoreFoldersPath`: Specifies the path for the `vectorStoreFolders.json` file.
  - `excludedFiles` and `excludedFolders`: Optional lists for excluding specific files and folders from processing.
  - `gitAiPrompt`: Configurable AI prompt for generating commit messages from Git changes.

- **.openai:** Contains your OpenAI API key and optional organization/project identifiers.
- **Config/LogConfig.xml:** Configuration file for NLog, allowing customization of logging behavior.
- **MimeTypes/Config/\*:** JSON files defining MIME types, new file extensions for processing, and Markdown tags for code blocks.

### Usage

1. **Select Folders:** Use the "Select Folders" button to choose directories for processing.
2. **Manage Vector Stores:** Select existing vector stores or create new ones using the provided dropdown and text input.
3. **Upload/Replace Files:** Process and upload selected folder contents to the chosen vector store.
4. **Convert Files:** Use the "Convert to single file" section to generate DOCX, MD, or PDF files from selected folders.
5. **Retrieve Git Changes:** Click the "Get Git Changes" button to analyze and save Git changes from selected folders.

### Planned Features (TODO)

- Store last upload date for vector stores.
- Review/Edit configuration settings on the Settings tab.
- Store configured values with each vector store data (exclusions, etc.).

### Contributing

Contributions to the VecTool project are welcome! If you have ideas for improvements, new features, or bug fixes, please feel free to fork the repository and submit a pull request.

### License

This project is licensed under the Apache License 2.0. See the [LICENSE](LICENSE-2.0.txt) file for details.
