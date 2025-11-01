# VecTool: AI-Ready Code Export & Development Workflow Manager

**Version**: 4.5.25.1101  
**Framework**: .NET 8.0 Windows (WinForms)  
**License**: Apache 2.0  
**Status**: Production-Ready with Active Development

---

## 🎯 Overview

VecTool is a modular C# WinForms desktop application designed to export code projects in AI-digestible formats and manage Git-based development workflows. It bridges the gap between local development environments and large language models (LLMs), enabling efficient AI-assisted development practices.

### Primary Use Cases

- **📄 Single-File Export**: Export entire project hierarchies to unified Markdown files optimized for LLM consumption
- **🔄 Git Integration**: Generate AI-ready summaries of code changes and Git diffs
- **🧪 Test Automation**: Execute unit tests with progress tracking and EMA-based time estimation
- **📂 Recent Files Management**: Organize and track generated artifacts with drag-drop support
- **🔍 Smart Filtering**: Respect .gitignore, .vtignore, and project-specific exclusion rules

---

## ✨ Key Features

### 1. Single-File Export ⭐
Export project hierarchies to optimized Markdown files with:
- Code-aware syntax highlighting tags (C#, JavaScript, Python, XML, etc.)
- Hierarchical folder structure preservation
- File metadata (LOC, complexity metrics)
- AI-ready formatting with cross-references
- Support for multiple source folders per vector store

### 2. Git Integration
- **Get Git Changes**: Retrieve diffs and status from repositories
- **Git Changes to MD**: Generate formatted Markdown with embedded AI prompts
- Configurable AI prompts for automatic commit message generation
- Branch and repository context preservation
- Intelligent repository root detection

### 3. Unit Test Runner
- **Shortcut**: Ctrl+T (or menu-based execution)
- **Execution**: Runs `dotnet test` programmatically
- **Progress Tracking**: Real-time progress bar with EMA-based time-remaining calculation
- **Output**: Saves dated results to configurable directory
- **Recent Files Integration**: Automatic artifact tracking

### 4. Recent Files System
- **Auto-Tracking**: Captures all generated outputs (Markdown, Git changes, test results)
- **Drag-Drop Support**: External tool integration
- **Advanced Filtering**: Filter by file type, vector store association
- **Automatic Cleanup**: 30-day retention policy (configurable)
- **Persistent Layout**: Column widths and preferences saved

### 5. Smart File Filtering & Exclusion
- **Legacy Pattern Matching**: Wildcard-based filtering
- **.gitignore Support**: Full Git specification compliance via GitignoreParserNet
- **.vtignore Override**: VecTool-specific patterns override Git rules
- **Dual-Path Testing**: Directory vs. file name matching
- **Pluggable Architecture**: Swappable adapter implementations

---

## 🏗️ Architecture

### Project Structure

```
VecTool/
├── OaiUI/                    # Main WinForms application
│   ├── MainForm.cs
│   ├── Progress/             # ProgressManager, ProgressPanel
│   └── AboutForm.cs
├── Configuration/            # Settings & persistence
│   ├── ISettingsStore        # Key-value abstraction
│   ├── UiStateConfig         # UI layout persistence
│   ├── RecentFilesConfig     # Recent files settings
│   ├── VectorStoreConfig/    # Multi-part vector store config
│   └── Exclusion/            # Pattern matching adapters
├── Constants/                # Centralized strings & tags
├── Core/                     # Business logic
│   ├── GitRunner             # Git command execution
│   ├── RepoLocator           # Git root detection
│   ├── FileSystemTraverser   # Recursive file traversal
│   └── AiContextGenerator    # Metadata extraction
├── Handlers/                 # Feature implementations
│   ├── FileHandlerBase       # Base handler (delegation)
│   ├── MarkdownExportHandler
│   ├── GitChangesHandler
│   ├── FileSizeSummaryHandler
│   └── TestRunnerHandler
├── RecentFiles/              # Recent files management
│   ├── IRecentFilesManager
│   ├── RecentFilesManager
│   ├── RecentFilesPanel      # WinForms UserControl
│   └── DatedFileWriter
├── Utils/                    # Utilities
│   ├── VersionInfo           # Assembly metadata
│   ├── MimeTypeMapper
│   └── FileHelper
├── Log/                      # Logging infrastructure
├── UnitTests/                # NUnit test suite
└── LogCtx/                   # Submodule (logging wrapper)
```

### Design Principles

The architecture strictly adheres to **SOLID principles**:

- **Single Responsibility**: Each class has one reason to change
- **Open/Closed**: New exclusion adapters without modifying core logic
- **Liskov Substitution**: Consistent adapter contracts
- **Interface Segregation**: Minimal, focused interfaces
- **Dependency Inversion**: Abstract dependencies throughout

### Technology Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Framework | .NET 8.0 | Latest runtime |
| UI | Windows Forms | Desktop application |
| Testing | NUnit + Shouldly | Test framework & assertions |
| Logging | NLog + LogCtx | Structured logging |
| Git Operations | Git CLI + GitRunner | Process-based Git integration |
| Ignore Patterns | GitignoreParserNet, MAB.DotIgnore | Pattern matching |
| Configuration | System.Configuration | App.config support |
| Serialization | System.Text.Json | JSON persistence |

---

## 🚀 Getting Started

### Prerequisites

- **Windows OS** (XP SP3 or later for WinForms)
- **.NET 8.0 Runtime** or SDK
- **Git** (for repository operations)
- **Visual Studio 2022** (for development)

### Installation

#### Option 1: Download Pre-Built Executable
```
VecTool/bin/Release/net8.0-windows/win-x64/publish/oaiUI.exe
```

#### Option 2: Build from Source
```bash
# Clone repository
git clone https://github.com/your-repo/VecTool.git
cd VecTool

# Restore dependencies
dotnet restore

# Build
dotnet build -c Release

# Publish self-contained executable
dotnet publish OaiUI/oaiUI.csproj -r win-x64 -c Release /p:PublishSingleFile=true --self-contained true
```

### First Run

1. **Launch Application**: Double-click `oaiUI.exe`
2. **Create Vector Store**: Enter name and click "Create Vector Store"
3. **Select Folders**: Click "Select Folders" to choose source directories
4. **Export**: Press Ctrl+M to export to Markdown

### Configuration

Configuration is stored in three locations:

#### `app.config` (Application Settings)
```xml
<appSettings>
    <add key="recentFilesMaxCount" value="200" />
    <add key="recentFilesRetentionDays" value="30" />
    <add key="recentFilesOutputPath" value="Generated" />
    <add key="excludedFiles" value="*.log,*.tmp,*.bak" />
    <add key="excludedFolders" value="bin,obj,.git,.vs" />
</appSettings>
```

#### `uiState.json` (UI Preferences)
Automatically saved on application close. Stores column widths, row heights, and font sizes for Recent Files panel.

#### `vectorStoreFolders.json` (Vector Store Associations)
Tracks folder-to-vector-store associations and custom exclusion rules per store.

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **Ctrl+M** | Convert to Markdown |
| **Ctrl+G** | Get Git Changes |
| **Ctrl+F** | File Size Summary |
| **Ctrl+T** | Run Unit Tests |
| **Ctrl+R** | Export to Repomix (future) |
| **Alt+F4** | Exit Application |

---

## 📋 Menu Reference

### File Menu
- **Convert to Markdown** - Export selected folders to single MD file
- **Get Git Changes** - Retrieve Git diff/status as formatted MD
- **File Size Summary** - Generate file size analysis report
- **Run Tests** - Execute `dotnet test` with progress
- **Export to Repomix** - Future integration
- **Exit** - Close application

### Help Menu
- **About** - Display version information and credits

---

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test UnitTests/UnitTests.csproj

# Run specific test class
dotnet test UnitTests/UnitTests.csproj --filter "FullyQualifiedName~FileSizeSummaryHandlerTests"

# Run with verbose output
dotnet test UnitTests/UnitTests.csproj --verbosity detailed
```

### Test Infrastructure

- **Framework**: NUnit with Shouldly fluent assertions
- **Apartment**: Single-Threaded (STA) for WinForms compatibility
- **Parallelism**: Single worker to prevent UI resource conflicts
- **Coverage**: Configuration, handlers, UI components, architecture validation

### Test Examples

```csharp
[TestFixture]
public class RecentFilesConfigTests
{
    [Test]
    public void ConstructorShouldValidateMaxCount()
    {
        Action act = () => new RecentFilesConfig(-1, 30, "Generated");
        Should.Throw<ArgumentOutOfRangeException>(act);
    }
}
```

---

## 📦 Version Schema

VecTool uses a hierarchical versioning scheme:

```
AssemblyVersion: {Major}.0.0.0
FileVersion: {Major}.{PlanId}.{BuildPart}.{HHmm}
  where BuildPart = (PlanPhase * 1000) + DayOfYear
ApplicationDisplayVersion: {Major}.{PlanId}.p{PlanPhase}
```

**Current**: v1.25.1005  
- Major = 1
- PlanId = 25  
- BuildPart = 1005 (phase 1, day 5 of year)

---

## 🔄 Recent Changes (v1.25.1005 - October 5, 2025)

### Breaking Changes ❌
- ❌ **Removed DOCX Export**: All DocX-related handlers eliminated
- ❌ **Removed PDF Export**: QuestPDF integration removed
- ❌ **Removed OpenAI Vector Store Management**: Direct OAI vector store features deprecated

### New Features ✅
- ✅ **Run Unit Tests**: Ctrl+T executes `dotnet test` with progress tracking
- ✅ **Recent Files System**: Drag-drop, type filtering, 30-day cleanup
- ✅ **Progress Manager**: EMA-based time-remaining estimation
- ✅ **Constants Library**: Project-wide centralized strings
- ✅ **UI State Persistence**: JSON-backed layout preferences

### Improvements 🏗️
- 🏗️ Modular refactor across 8+ projects
- 🏗️ Configuration abstraction for testability
- 🏗️ Git timeout handling with cancellation tokens
- 🏗️ Swappable exclusion pattern adapters
- 🏗️ Structured logging via LogCtx

---

## 📅 Roadmap

### Planned Features (Next Phases)

- **Settings UI**: Configuration review/editing interface
- **Per-Store Customization**: Custom file & exclusion rules per vector store
- **Perplexity API Integration**: AI-assisted commit messages
- **Drag-to-Recent**: Drag files from file system into recent files
- **Console Execution**: Command execution with file redirection
- **Branch Awareness**: Vector store linking to Git branches
- **Error UI System**: Centralized error notification
- **VS Code Theming**: Dark/light theme support
- **Last Upload Tracking**: Per-store upload date storage

### Future Integrations

- [ ] Perplexity API for commit message generation
- [ ] Claude API integration
- [ ] GitHub/GitLab API integration
- [ ] Vector store auto-synchronization
- [ ] Batch export operations
- [ ] CI/CD pipeline integration

---

## 🐛 Known Limitations

- **Windows Only**: WinForms is Windows-specific; macOS/Linux not supported
- **Single Thread UI**: Async operations may block UI briefly during heavy file traversal
- **Git Dependency**: Some features require Git CLI installed
- **Performance**: Large codebases (>100k files) may require optimization
- **Pattern Matching**: Complex regex patterns not currently supported

---

## 🛠️ Development

### Project Structure for Development

```
VecTool/
├── OaiUI/                    # Modify MainForm, add UI features here
├── Handlers/                 # Add new feature handlers (inherit FileHandlerBase)
├── Configuration/            # Extend settings management
├── Core/                     # Core business logic (Git, traversal, etc.)
├── UnitTests/                # Add tests for any changes
└── LogCtx/                   # Git submodule - do not modify here
```

### Adding a New Feature

1. **Create Handler** (`Handlers/MyNewHandler.cs`)
   ```csharp
   public class MyNewHandler : FileHandlerBase
   {
       protected override void Execute(List<string> folders, VectorStoreConfig config)
       {
           // Implementation
       }
   }
   ```

2. **Add UI Menu Item** (`OaiUI/MainForm.Designer.cs`)
   ```csharp
   myNewToolStripMenuItem.Click += (s, e) => new MyNewHandler(...).Execute(...);
   ```

3. **Write Tests** (`UnitTests/MyNewHandlerTests.cs`)
   ```csharp
   [TestFixture]
   public class MyNewHandlerTests
   {
       [Test]
       public void ShouldProduceCorrectOutput() { }
   }
   ```

4. **Update Configuration** if needed
5. **Run full test suite**: `dotnet test`

### Coding Standards

- **Language**: English only (code, comments, UI)
- **Naming**: PascalCase for classes/methods, camelCase for locals/parameters
- **Underscores/Slashes**: Preserve exactly as used in Git/file paths
- **Logging**: Use LogCtx structured context for all operations
- **Null Safety**: `#nullable enable` required in all files
- **SOLID**: Adhere to Single Responsibility, Open/Closed, Liskov, Interface Segregation, Dependency Inversion

### Building & Publishing

```bash
# Build
dotnet build -c Release

# Run tests
dotnet test -c Release

# Publish self-contained executable
dotnet publish OaiUI/oaiUI.csproj -r win-x64 -c Release \
  /p:PublishSingleFile=true \
  --self-contained true
```

---

## 📚 Documentation

Full codebase documentation is split into focused documents:

1. **VecTool-SUMMARY.md** (~8KB) - Architecture overview & design patterns
2. **VecTool-BI.md** (~25KB) - Business logic, handlers, configuration
3. **VecTool-UI.md** (~20KB) - WinForms UI, components, progress system
4. **VecTool-TESTS.md** (~18KB) - Testing framework, fixtures, patterns
5. **VecTool-GUIDE.md** (~13KB) - Document usage guide with examples

See **VecTool-GUIDE.md** for detailed navigation and recommended learning paths.

---

## 🔗 Dependencies

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| GitignoreParserNet | 0.2.0.14 | Full .gitignore compliance |
| MAB.DotIgnore | 3.0.2 | Alternative pattern matcher |
| System.Configuration.ConfigurationManager | 9.0.10 | app.config support |
| NUnit | Latest | Test framework |
| Shouldly | Latest | Fluent assertions |
| NLog | Latest | Structured logging |

### Git Submodules

- **LogCtx** (https://github.com/zzt108/LogCtx.git) - Custom logging wrapper for NLog+SEQ

---

## 💬 Contributing

### Bug Reports

Report bugs via GitHub Issues with:
- VecTool version (see Help → About)
- Windows version
- Steps to reproduce
- Expected vs. actual behavior
- Screenshots (if applicable)

### Pull Requests

1. Fork repository
2. Create feature branch (`git checkout -b feature/YourFeature`)
3. Make changes with tests
4. Update documentation
5. Submit PR with description

### Code Review Checklist

- [ ] Follows SOLID principles
- [ ] Unit tests included and passing
- [ ] LogCtx structured logging used
- [ ] `#nullable enable` in new files
- [ ] Underscores/slashes preserved
- [ ] English-only code/comments
- [ ] No breaking changes without justification

---

## 📝 License

VecTool is licensed under the **Apache 2.0 License**. See LICENSE file for details.

---

## 🤝 Support

- **Issues**: GitHub Issues for bug reports and feature requests
- **Discussions**: GitHub Discussions for questions and ideas
- **Documentation**: See embedded markdown documents
- **Examples**: Test suite contains comprehensive examples

---

## 👨‍💼 About

VecTool was created to streamline the workflow of exporting code projects for AI consumption and managing development workflows across local environments and LLM services.

**Maintained by**: Development Team  
**Last Updated**: November 2025  
**Status**: Production-Ready with Active Development

---

## 📞 Quick Links

- 📖 [Full Documentation](VecTool-GUIDE.md)
- 🏗️ [Architecture](VecTool-SUMMARY.md)
- 💼 [Business Logic](VecTool-BI.md)
- 🖥️ [User Interface](VecTool-UI.md)
- 🧪 [Testing Guide](VecTool-TESTS.md)

---

**Version**: 1.25.1005 | **Last Updated**: November 1, 2025