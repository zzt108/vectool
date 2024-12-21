# Project Summary

## Overview
The **OaiVectorStore** project is a solution designed to manage and process files, manage MIME types, and provide a user-friendly interface for interacting with vector stores. The project is structured into several key components, each responsible for specific functionalities.

The user can select folders whose content and its subfolders and their content is exported as docx files for each folders

## Directory Structure

### DocX
- **DocXHandler.cs**
  - **DocXHandler**: A class responsible for converting files to the `.docx` format.
  - **Method**: `ConvertFilesToDocx(string folderPath, string outputPath)` – Converts files in the specified folder to `.docx` and outputs them to the designated path.

### LogCtx
The `LogCtx` directory handles logging functionalities across the project.

#### LogCtxShared
- **ILogCtxLogger.cs**
  - **ILogCtxLogger**: An interface defining logging levels (Fatal, Error, Warn, Info, Debug, Trace) and configuration methods.
  - **IScopeContext**: An interface for managing logging context properties.
  
- **JsonExtensions.cs**
  - **JsonExtensions**: A static class providing JSON serialization and deserialization helper methods.

- **LogCtx.cs**
  - **LogCtx**: Manages the logging context and properties.
  
- **Props.cs**
  - **Props**: Extends `Dictionary<string, object>` to handle additional properties and implements `IDisposable`.

#### NLogShared
- **CtxLogger.cs**
  - **CtxLogger**: Implements `ILogCtxLogger` using NLog for logging.
  - **NLogScopeContext**: Manages logging scope properties.

### MimeTypes
- **MimeTypeProvider.cs**
  - **MimeTypeProvider**: Handles MIME type retrieval, new file extensions, Markdown tags, and checks if a file extension represents a binary file.

### OaiUI
The `OaiUI` directory contains the user interface components of the project.

- **MainForm.cs**
  - **MainForm**: The main Windows Form handling user interactions such as selecting folders, uploading files, and managing vector stores.
  - **Methods**: Includes event handlers like `btnClearFolders_Click`, `btnUploadFiles_Click`, and methods for uploading and managing files and vector stores.

- **MainForm.Designer.cs**
  - **Designer Code**: Automatically generated code for designing the MainForm UI.
  
- **Program.cs**
  - **Program**: The entry point of the application containing the `Main` method.

### oaiVectorStore
Handles operations related to vector stores.

- **FileStoreManager.cs**
  - **FileStoreManager**: Manages uploading and deleting files in the file store.
  
- **MyOpenAIClient.cs**
  - **MyOpenAIClient**: A custom implementation of an OpenAI client, implementing `IDisposable`.
  
- **VectorStoreManager.cs**
  - **VectorStoreManager**: Manages creation, deletion, and retrieval of vector stores, and handles file associations with vector stores.

### UnitTests
Contains unit tests for various components of the project.

- **DocXHandlerTests.cs**
- **NLogCtxTests.cs**
- **SeriLogCtxTests.cs**
- **UnitTests.cs**
- **Config/**: Configuration files for different logging frameworks used in tests.

## Key Functionalities

- **File Conversion**: Converts files to `.docx` format using the `DocXHandler` class.
- **Logging**: Provides comprehensive logging capabilities with support for multiple logging levels and contexts via NLog and Serilog adapters.
- **MIME Type Handling**: Determines MIME types, new file extensions, and handles binary file checks using `MimeTypeProvider`.
- **User Interface**: Offers a Windows Forms-based UI for user interactions, allowing operations like selecting folders, uploading files, and managing vector stores.
- **Vector Store Management**: Facilitates the creation, deletion, and retrieval of vector stores, and manages the association of files with these stores.
- **Unit Testing**: Ensures code reliability and correctness through a suite of unit tests covering critical components.

## Conclusion
The **OaiVectorStore** project is well-organized with a clear separation of concerns across its directories. It leverages robust logging mechanisms, efficient file handling, and provides a user-friendly interface for managing vector stores. The inclusion of comprehensive unit tests further ensures the project's reliability and maintainability.
