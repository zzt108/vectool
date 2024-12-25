# VecTool

## Project Overview

The **VecTool** project is a C# based desktop application designed to streamline the process of managing files and interacting with OpenAI's vector stores. This tool allows users to easily select folders, process their contents, and upload these files to specific vector stores for use with OpenAI's models. It also provides functionality to manage existing vector stores and export folder contents to Markdown or DOCX.

## Key Features

*   **Folder Selection:** Allows users to select multiple folders whose content (including subfolders) will be processed. Selected folders are now remembered for each vector store, and the association is correctly updated when new folders are selected.
*   **Clear Selected Folders:** Provides a button to quickly clear the list of currently selected folders.
*   **Automatic Folder Content Export to DOCX:** Exports the content of each selected folder and its subfolders into individual `.docx` files, making large codebases or document collections easier to manage within vector stores. The `.docx` files include tags indicating the start and end of each folder (`<Folder name = ...>`, `</Folder>`) and file (`<File name = ...>`, `</File>`).
*   **Vector Store Management:**
    *   **Selection:** Allows users to select an existing vector store from a dropdown menu.
    *   **Creation:** Enables users to create new vector stores by entering a name.
    *   **Deletion (Files):** Provides a button to delete all files associated with a selected vector store.
*   **File Upload to Vector Stores:**
    *   **Upload/Replace:** Uploads the content of selected folders to a specified vector store. If a vector store with the same name already exists, all its files will be deleted and replaced with the new content.
    *   **Binary File Handling:** Identifies and uploads binary files separately, ensuring all relevant file types can be included in the vector store.
*   **MIME Type Handling:** Utilizes MIME types to correctly identify and process different file formats.
*   **Folder Association with Vector Stores:** Automatically saves and loads the association between selected folders and specific vector stores. This means when you select a vector store, the folders you previously used with it will be automatically loaded. This association is saved for future use in a configurable JSON file.
*   **Export to Markdown:** Converts the content of selected folders into a single Markdown (`.md`) file. Folder and file names are indicated using Markdown headings (`# Folder: ...`, `## File: ...`).
    *   **Automatic Tag Generation:** If a file extension is not found in `mdTags.json`, the extension itself (without the leading dot) will be used as the code block tag in the Markdown output. For example, a file named `script.xyz` will have the tag `xyz` in the Markdown.
*   **Excluding Files:** Users can configure a comma-separated list of filenames in the `app.config` to exclude them from processing.
*   **Excluding Folders:** Users can configure a comma-separated list of folder names in the `app.config` to exclude their contents from processing.
*   **Support for PowerShell and Jupyter/Polyglot files:** Added support for processing PowerShell scripts (`.ps1`, `.psm1`) and Jupyter/Polyglot notebook files (`.ipynb`).
*   **Logging:** Comprehensive logging using NLog for debugging and monitoring.

## Before 1st Run

1. **Configure Application Settings:**
    *   Locate the `app.config` file in the `OaiUI` project.
    *   Add or modify the following settings in the `appSettings` section:
        *   `vectorStoreFoldersPath`: Specifies the path for the `vectorStoreFolders.json` file. If this setting is not present, a default path of `..\..\vectorStoreFolders.json` will be used.
        *   `excludedFiles`: (Optional) A comma-separated list of filenames to exclude from processing.
        *   `excludedFolders`: (Optional) A comma-separated list of folder names to exclude from processing.

    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
        <configSections>
        <appSettings>
            <add key="vectorStoreFoldersPath" value="path/to/your/vectorStoreFolders.json" />
            <add key="excludedFiles" value=".gitignore,fileToExclude.txt" />
            <add key="excludedFolders" value=".git,node_modules" />
        </appSettings>
    </configuration>
    ```

2. **Configure OpenAI API Key:**
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
2. Click the "Select Folders" button on the main tab.
3. In the folder browser dialog, navigate to and select the folder(s) you want to process. Click "OK".
4. The selected folder paths will appear in the list box below the button. When a folder is selected, it is automatically associated with the currently selected vector store (if any).

### Clearing Selected Folders

1. Click the "Empty Selected" button next to the "Select Folders" button. This will clear the list of folders currently selected and correctly remove the association of these folders with the currently selected vector store.

### Selecting or Creating a Vector Store

1. **Select Existing:** On the main tab, choose a vector store from the dropdown menu labeled "Select Existing Vector Store:". Previously used folders for this vector store will be automatically loaded into the "Selected Folders" list.
2. **Create New:** If you intend to upload to a new vector store:
    *   Enter a name for the new vector store in the text box next to the label "Or Enter New Name:".
    *   The new vector store will be created when you upload files.

### Uploading/Replacing Files to a Vector Store

This action will upload the content of the selected folders to the chosen vector store. If a vector store with the same name already exists, all its files will be deleted and replaced with the new content.

1. Ensure you have selected the desired folders and either selected an existing vector store or entered a name for a new one.
2. Click the "Upload / Replace" button.
3. The application will process each folder, convert text-based files to include appropriate Markdown tags, and upload the content to the vector store. Binary files will be uploaded directly. To handle large text files efficiently, the content of each folder is temporarily converted to a `.docx` file before individual files are processed and uploaded.
4. A progress bar at the top of the window will indicate the upload progress. The status of the current operation and the number of processed items are displayed at the bottom of the window.

### Deleting All Files from a Vector Store

1. Select the vector store from which you want to delete all files using the dropdown menu.
2. Click the "Delete All VS files" button.
3. Confirm the action if prompted. All files associated with the selected vector store in OpenAI will be deleted. The status bar will indicate the progress.

### Converting Selected Folders to a Single DOCX File

This will create a single `.docx` file containing the content of all selected folders and their subfolders, with folder and file tags.

1. Select the desired folders.
2. Click the "DOCX" button in the "Convert to single file" panel.
3. A "Save As" dialog will appear. Choose a location and filename for the output `.docx` file. If you have entered a new Vector Store name or selected an existing one, that name will be used as the default file name. Click "Save".
4. The content of each folder and its files will be added to the `.docx` file, with folder names enclosed in `<Folder name = ...>` tags and file names in `<File name = ...>` tags.

### Converting Selected Folders to a Single Markdown File

This will create a single `.md` file containing the content of all selected folders and their subfolders, using Markdown headings for folders and files.

1. Select the desired folders.
2. Click the "MD" button in the "Convert to single file" panel.
3. A "Save As" dialog will appear. Choose a location and filename for the output `.md` file. If you have entered a new Vector Store name or selected an existing one, that name will be used as the default file name. Click "Save".
4. The content of each folder and its files will be added to the `.md` file, with folder names indicated by `# Folder: [folder path]` and file names by `## File: [file name]`.
    *   **Automatic Tag Generation:** If a file's extension is not defined in `mdTags.json`, the extension itself (without the leading dot) will be used as the code block tag. For example:

    ```markdown
    ## File: my_script.xyz Time:2024-12-25 10:30:00
    ```xyz
    // Content of my_script.xyz
    ```

## Automatic Saving and Loading of Folder Associations

The application automatically saves which folders you have selected for each vector store. When you select a vector store from the dropdown, the folders you previously used with that vector store will be automatically loaded into the "Selected Folders" list. This feature allows you to easily manage different sets of folders for different vector stores. The path to the file where this information is stored can be configured in the `app.config` file using the `vectorStoreFoldersPath` setting.

## Configuration

*   **app.config:** Contains application-level settings, including the path to the `vectorStoreFolders.json` file and the optional list of `excludedFiles` and `excludedFolders`. `excludedFolder` names support wildcards (e.g., `bin`, `obj`, `*Temporary*`).
*   **.openai:** Contains your OpenAI API key and optional organization/project identifiers. Ensure this file is properly configured before running the application.
*   **OaiUI/Config/LogConfig.xml:** Configuration file for NLog, allowing you to customize logging behavior (e.g., log levels, output targets).
*   **MimeTypes/Config/\*:** JSON files defining MIME types (`mimeTypes.json`), new file extensions for processing (`newExtensions.json`), and Markdown tags for code blocks (`mdTags.json`).

## Contributing

Contributions to the **VecTool** project are welcome! If you have ideas for improvements, new features, or bug fixes, please feel free to fork the repository and submit a pull request.

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE-2.0.txt) file for details.