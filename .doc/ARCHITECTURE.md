# VecTool Architecture

## Overview

VecTool allows developers to export their codebase into AI-ready formats. The architecture follows strict **SOLID principles**, ensuring modularity, testability, and ease of extension.

## High-Level Design

The application is layered into Presentation (WinForms), Application (Handlers), Domain (Core), and Infrastructure capabilities.

![Architecture](https://www.plantuml.com/plantuml/png/RLB1RXen4BtxAonES4WEFVOG1K6Jh4G4WaGzL3cCpE1Qx8vNzcmBQlll7UyQC14tUzxRzzw-yMDa2DmrjOfxHoq4QBJnJUi9YJTyh54B-AL0Mp1xi3XW0Gq3sHgLodCzMWMCI0piFEa5736Cz1-bzOybkFTAZxyxslhVKJ_vqCJ9XuQetwk4huAtycrS0M7Tpq-JXAxHYEgJgx4W34Frhx2VjctjJE6knoz0snf3d0n173667HWXLqalmwCaMELBSdo5i9UazEtG-i0Pw6rVvmNjsV7i9v35M59aMd3UCFAw8y9GSj8qtwz-OUuRSEVqCnzmaYjCSJV7oDYy1VY58MQbV3i5obwd86auTJDFZ0T-Ha60_O1EgnaVXdfRl_2c1wx2LBGB43bqXhk0EOTexPpjGbzpBpHCCXOPYED7kYsy5OUU8aVho2PrUGVtzm_djlLNFSSQBJ0cfQQHSSoah2oKDS8sfbPynshO_2t2Yulimr6fyvRqtTtVYvcIHcu3ie_RoBNMmwLkCTqOeAZxTtdD6heLHdhem7r5ZCJ9lwJ9DMNlHCNAqUJVBzCKlF6oJPr3hSsmpubIEKxcVQgZikG3YduAA1g9arSXf7IAUjdYiCger2FIjclg_m00)

```plantuml
@startuml
skinparam componentStyle uml2
skinparam packageStyle rectangle

package "Presentation Layer" {
  [OaiUI (Windows Forms)] as UI
  [ProgressPanel] as Progress
  [RecentFilesPanel] as Recent
}

package "Application Layer (Handlers)" {
  interface "IFileHandler" as IHandler
  [MarkdownExportHandler] as MDHandler
  [GitChangesHandler] as GitHandler
  [TestRunnerHandler] as TestHandler
}

package "Domain Layer (Core)" {
  [FileSystemTraverser] as Traverser
  [GitRunner] as Git
  [AiContextGenerator] as AICtx
  [RepoLocator]
}

package "Infrastructure" {
  [Configuration] as Config
  [Constants]
  [LogCtx] as Logging
}

UI ..> IHandler : Delegates Commands
UI ..> Config : Reads Layout
Recent ..> Config : Reads History

MDHandler --|> IHandler
GitHandler --|> IHandler
TestHandler --|> IHandler

MDHandler --> Traverser : Scans Files
MDHandler --> AICtx : Formats Content
GitHandler --> Git : Runs git commands
TestHandler --> Git : (Optional context)

Traverser --> Config : Reads Ignore Rules
Git --> Logging : Logs Operations

@enduml
```

## detailed Components

### 1. Presentation Layer (`OaiUI`)

- **MainForm**: The primary container. Handles menu events and initializes handlers.
- **ProgressPanel**: encapsulated UI for showing operation progress with EMA (Exponential Moving Average) time estimation.
- **RecentFilesPanel**: Manages the list of generated files, supporting drag-and-drop and filtering.

### 2. Application Layer (`Handlers`)

- **Pattern**: Command / Strategy Pattern.
- **FileHandlerBase**: Abstract base class that standardizes execution flow (validation -> execution -> logging).
- **Handlers**:
  - `MarkdownExportHandler`: Orchestrates the file scan and markdown generation.
  - `GitChangesHandler`: Captures git status/diff and formats it.
  - `TestRunnerHandler`: Executes `dotnet test` and parses output.

### 3. Domain Layer (`Core`)

- **FileSystemTraverser**: A robust, exclusion-aware file scanner. It uses `IGitignoreParser` to respect `.gitignore` and `.vtignore` files.
- **GitRunner**: A wrapper around the `git` CLI process. Handles timeouts, cancellation, and output parsing.
- **AiContextGenerator**: Responsible for reading file content and wrapping it in XML-style tags with metadata (TOKENS, LOC).

### 4. Infrastructure

- **Configuration**:
  - `ISettingsStore`: Abstraction for `app.config` vs `InMemory` (for tests).
  - `VectorStoreConfig`: Manages mappings between logical "Vector Stores" and physical folders.
- **Logging**: Uses `LogCtx` (NLog wrapper) for structured logging.

## Design Patterns

- **Dependency Injection**: Dependencies are injected into Handlers (though currently via constructor manually in `MainForm`, designed for DI container adoption).
- **Observer**: Progress updates are pushed to the UI via `IProgress<T>`.
- **Template Method**: `FileHandlerBase` defines the skeleton of an operation.
