# Before 1st run 
- Copy template.openai to .openai 
- fill in API key and optionally OrgId and Project ID
# OaiVectorStore

## Project Overview

The **OaiVectorStore** project is a C# based desktop application designed to streamline the process of managing files and interacting with OpenAI's vector stores. This tool allows users to easily select folders, convert their contents into manageable `.docx` files, and upload these files to specific vector stores for use with OpenAI's models. It also provides functionality to manage existing vector stores and export folder contents to Markdown.

## Key Features

*   **Folder Selection:** Allows users to select multiple folders whose content (including subfolders) will be processed.
*   **Automatic Folder Content Export to DOCX:** Exports the content of each selected folder and its subfolders into individual `.docx` files, making large codebases or document collections easier to manage within vector stores.
*   **Vector Store Management:**
    *   **Selection:** Users can select an existing vector store from a dropdown.
    *   **Creation:** Users can create new vector stores by providing a name.
    *   **Deletion (Files):** Users can delete all files associated with a selected vector store.
*   **File Upload to Vector Stores:**
    *   **Upload/Replace:** Uploads the content of selected folders to a specified vector store, replacing existing content if the vector store name is the same.
    *   **Binary File Handling:** Identifies and uploads binary files separately, ensuring all relevant file types can be included in the vector store.
*   **MIME Type Handling:** Utilizes MIME types to correctly identify and process different file formats.
*   **Folder Association with Vector Stores:** Automatically saves and loads the association between selected folders and specific vector stores. This means when you select a vector store, the folders you previously used with it will be automatically loaded.
*   **Export to Markdown:** Converts the content of selected folders into a single Markdown (`.md`) file.
*   **Logging:** Comprehensive logging using NLog for debugging and monitoring.

## Directory Structure

OaiVectorStore/
├── .cr/
├── .git/
├── .idea/
├── .vs/
├── .vscode/
├── DocX/ # Handles DOCX file conversion
│ ├── DocXHandler.cs
│ ├── DocXHandler.csproj
│ └── MDHandler.cs # Handles Markdown file export
├── Log/ # Contains logging implementations
│ └── Log.csproj
├── LogCtx/ # Shared logging context components
│ ├── LogCtxShared/
│ │ ├── ILogCtxLogger.cs
│ │ ├── IScopeContext.cs
│ │ ├── JsonExtensions.cs
│ │ ├── LogCtx.cs
│ │ └── Props.cs
│ └── NLogShared/
│ └── CtxLogger.cs
├── Logs/ # Default location for log files
├── MimeTypes/ # Handles MIME type detection
│ ├── Config/ # Configuration files for MIME types
│ │ ├── mdTags.json
│ │ ├── mimeTypes.json
│ │ └── newExtensions.json
│ ├── MimeTypeProvider.cs
│ └── MimeTypes.csproj
├── OaiUI/ # Windows Forms user interface
│ ├── Config/ # Configuration files for the UI
│ │ └── LogConfig.xml
│ ├── MainForm.Designer.cs
│ ├── MainForm.cs
│ ├── oaiUI.csproj
│ └── Program.cs
├── oaiVectorStore/ # Core logic for vector store interaction
│ ├── FileStoreManager.cs
│ ├── MyOpenAIClient.cs
│ ├── oaiVectorStore.csproj
│ └── VectorStoreManager.cs
├── packages/
├── UnitTests/ # Unit tests for various components
│ ├── Config/ # Configuration files for unit tests
│ │ ├── NLogConfig.xml
│ │ ├── SeriLogConfig.json
│ │ └── SeriLogConfig.xml
│ ├── ConvertSelectedFoldersToDocxTests.cs
│ ├── DocXHandlerTests.cs
│ ├── NLogCtxTests.cs
│ ├── SeriLogCtxTests.cs
│ ├── UnitTests.cs
│ └── UnitTests.csproj
├── projectsummary.md # Project summary (this file)
├── README.md # Project README (this file)
└── todo.md # List of pending tasks

## Before 1st Run

1. **Configure OpenAI API Key:**
    *   Copy the `template.openai` file located in the root directory to `.openai`.
    *   Open the newly created `.openai` file.
    *   Fill in your OpenAI API key. Optionally, you can also provide your Organization ID and Project ID if needed.

    ```
    {
      "ApiKey": "YOUR_OPENAI_API_KEY",
      "OrgId": "YOUR_OPTIONAL_ORG_ID",
      "Project": "YOUR_OPTIONAL_PROJECT_ID"
    }
    ```

## Usage

### Selecting Folders

1. Launch the `oaiUI.exe` application (located in `OaiUI/bin/Debug/net8.0-windows/`).
2. Click the "Select Folders" button.
3. In the folder browser dialog, navigate to and select the folder(s) you want to process. Click "OK".
4. The selected folder paths will appear in the list box below the button.

### Selecting or Creating a Vector Store

1. **Select Existing:** Choose a vector store from the dropdown menu labeled "Select Existing Vector Store:".
2. **Create New:** If you want to create a new vector store:
    *   Enter a name for the new vector store in the text box next to the label "Or Enter New Name:".
    *   The new vector store will be created when you upload files.

### Uploading/Replacing Files to a Vector Store

This action will upload the content of the selected folders to the chosen vector store. If a vector store with the same name already exists, its files will be deleted and replaced with the new content.

1. Ensure you have selected the desired folders and either selected an existing vector store or entered a name for a new one.
2. Click the "Upload Replace" button.
3. The application will process each folder, convert text-based files to include appropriate Markdown tags, and upload the content to the vector store. Binary files will be uploaded directly.
4. A progress bar at the top of the window will indicate the upload progress.
5. Status information is displayed at the bottom of the window.

### Deleting All Files from a Vector Store

1. Select the vector store from which you want to delete all files using the dropdown menu.
2. Click the "Delete All VS files" button.
3. Confirm the action if prompted. All files associated with the selected vector store will be deleted.

### Converting Selected Folders to a Single DOCX File

This will create a single `.docx` file containing the content of all selected folders and their subfolders.

1. Select the desired folders.
2. Click the "Convert to 1 DOCX" button.
3. A "Save As" dialog will appear. Choose a location and filename for the output `.docx` file and click "Save".
4. The content of each folder and its files will be added to the `.docx` file, with folder and file names indicated.

### Converting Selected Folders to a Single Markdown File

This will create a single `.md` file containing the content of all selected folders and their subfolders.

1. Select the desired folders.
2. Click the "Convert to 1 MD" button.
3. A "Save As" dialog will appear. Choose a location and filename for the output `.md` file and click "Save".
4. The content of each folder and its files will be added to the `.md` file, with folder and file names indicated using Markdown headings.

### Automatic Saving and Loading of Folder Associations

The application automatically saves which folders you have selected for each vector store. When you select a vector store from the dropdown, the folders you previously used with that vector store will be automatically loaded into the "Selected Folders" list.

## Configuration

*   **.openai:** Contains your OpenAI API key and optional organization/project identifiers. Ensure this file is properly configured before running the application.
*   **OaiUI/Config/LogConfig.xml:** Configuration file for NLog, allowing you to customize logging behavior (e.g., log levels, output targets).
*   **MimeTypes/Config/\*:** JSON files defining MIME types, new file extensions for processing, and Markdown tags for code blocks.

## Contributing

Contributions to the **OaiVectorStore** project are welcome! If you have ideas for improvements, new features, or bug fixes, please feel free to fork the repository and submit a pull request.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE-2.0.txt) file for details.
