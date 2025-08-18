## VecTool...
VecTool is a C# based desktop application designed to streamline the process of managing and exporting code projects for use with Large Language Models (LLMs). While it originally focused on OpenAI vector stores, its most valuable features now are the ability to export entire projects to single files digestible efficiently by AI and generate AI-ready output of git changes for generating commit comments with AI.

### Key Features

#### Single File Project Export (Recommended)

- **Convert to DOCX/MD/PDF:** Export your entire project or selected folders to a single, well-formatted file that's perfect for uploading to any LLM chat interface.
- **Code-Aware Formatting:** Automatically detects file types and applies appropriate syntax highlighting tags in the exported files.
- **Hierarchical Organization:** Maintains your project's folder structure in the exported file for easy navigation.
- **Multiple Format Support:** Export to DOCX, Markdown, or PDF depending on your needs and the LLM platform you're using.

#### .gitignore and *.vtignore based exclusion. Legacy Exclusion Removed (V2, 2025-08-18)

`app.config` settings `excludedFiles` and `excludedFolders` are **deprecated** and
no longer read at runtime.  Path filtering is now handled exclusively by:

* `.gitignore` files – identical precedence and override semantics to git.
* Optional `*.VTIgnore` files – same syntax, evaluated after `.gitignore`.

To exclude a path, add a matching pattern to one of these files at any level
of the project tree.  No application restart is necessary; VecTool detects
changes automatically.

#### Git Changes Integration (Added: April 12, 2025)

- **Get Git Changes:** Retrieve and save Git changes from selected folders into a single, well-formatted Markdown file.
- **AI-Assisted Commit Messages:** Includes a configurable AI prompt to analyze Git changes and provide concise, descriptive commit messages.
- **Comprehensive Diff Information:** Captures both status changes and detailed diffs for complete context.

#### PDF Conversion (Added: February 15, 2025)

- **Convert to PDF:** Generate professional-looking PDF documents from your project folders.
- **QuestPDF Integration:** Uses QuestPDF library for high-quality PDF generation.

#### Enhanced File Handling (Updated: March 8, 2025)

- **Improved Exclusion Logic:** Support for wildcard patterns when excluding files and folders.
- **Comprehensive Content Processing:** Handles various file types appropriately in the exported documents.

#### OpenAI Vector Store Management (Legacy Feature)

> **Note:** The vector store features have not been updated recently. For most use cases, we recommend using the single file export options instead, which work with any LLM chat interface that allows file uploads.

- **Vector Store Association:** The application can save and load associations between selected folders and specific vector stores.
- **Upload/Replace Files:** Process and upload selected folder contents to chosen vector stores.

### No API Key Required for Core Features

The most useful features of VecTool (single file export and Git changes) **do not require any API keys** and can be used with any LLM platform that supports file uploads, including:

- ChatGPT
- Claude
- Gemini
- Perplexity
- Any other chat interface that allows document uploads

### Configuration

- **app.config:** Contains application-level settings, including:
  - `excludedFiles` and `excludedFolders`: Optional lists for excluding specific files and folders from processing.
  - `gitAiPrompt`: Configurable AI prompt for generating commit messages from Git changes.

- **Config/LogConfig.xml:** Configuration file for NLog, allowing customization of logging behavior.
- **MimeTypes/Config/*:** JSON files defining MIME types, new file extensions for processing, and Markdown tags for code blocks.

### Usage

1. **Select Folders:** Use the "Select Folders" button to choose directories for processing.
2. **Export to Single File:** Use the "Convert to single file" section to generate DOCX, MD, or PDF files from selected folders.
3. **Retrieve Git Changes:** Click the "Get Git Changes" button to analyze and save Git changes from selected folders.
4. **Upload to LLM:** Take the generated single file and upload it to your preferred LLM chat interface for analysis, questions, or code review.

### Planned Features (TODO)

- Review/Edit configuration settings on the Settings tab.
- Store configured values with each project (exclusions, etc.).

### Contributing

Contributions to the VecTool project are welcome! If you have ideas for improvements, new features, or bug fixes, please feel free to fork the repository and submit a pull request.

### License

This project is licensed under the Apache License 2.0. See the [LICENSE](LICENSE-2.0.txt) file for details.
