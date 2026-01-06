# VecTool

**AI-Ready Code Export & Development Workflow Manager**

| Property | Value |
|----------|-------|
| **Current Version** | 4.80.p2 |
| **Framework** | .NET 8.0 |
| **Platform** | Windows Forms (WinForms) |
| **License** | Apache 2.0 |
| **Status** | Production-Ready with Active Development |
| **Last Updated** | 2026-01-06 |

---

## Overview

VecTool is a modular C# Windows Forms desktop application designed to export code projects in
AI-digestible formats and manage Git-based development workflows. It bridges the gap between
local development environments and large language models (LLMs), enabling efficient AI-assisted
development practices.

### Primary Use Cases

- **Single-File Export** - Export entire project hierarchies to unified Markdown files optimized
  for LLM consumption
- **Git Integration** - Generate AI-ready summaries of code changes and Git diffs
- **Test Automation** - Execute unit tests with progress tracking and EMA-based time estimation
- **Recent Files Management** - Organize and track generated artifacts with drag-drop support
- **Smart Filtering** - Respect .gitignore, .vtignore, and project-specific exclusion rules

---

## Key Features

### 1. Single-File Export

Export project hierarchies to optimized **XML-wrapped Markdown** files with:

- Code-aware syntax highlighting tags (C#, JavaScript, Python, XML, etc.)
- Hierarchical folder structure preservation
- File metadata (LOC, language, modification dates)
- AI-ready formatting with cross-references
- Support for multiple source folders per vector store

**Export Format:**

```xml
<?xml version="1.0" encoding="utf-8"?>
<codebase version="4.80.2005.2330" 
          totalFiles="203" 
          totalLoc="17590" 
          exportDate="2026-01-06T00:31:03">
  <file path="Configuration/IIgnorePatternMatcher.cs" 
        lines="1-28" 
        loc="28" 
        language="csharp" 
        modified="2026-01-03T16:03:16">
    <content><![CDATA[
```csharp
namespace VecTool.Configuration.Exclusion;
public interface IIgnorePatternMatcher : IDisposable { }
```
]]></content>
  </file>
</codebase>
```

**File Metadata Attributes:**
- `path` - Relative file path from repository root
- `lines` - Line range (e.g., "1-337" or "50-120" for partial exports)
- `loc` - Total lines of code
- `language` - Detected programming language
- `modified` - Last modification timestamp (ISO 8601)

**Document Metadata:**
- `version` - VecTool version (e.g., "4.80.2005.2330")
- `totalFiles` - Total files in export
- `totalLoc` - Total lines of code across all files
- `exportDate` - Generation timestamp

### 2. Git Integration

- **Get Git Changes** - Retrieve diffs and status from repositories
- **Git Changes to Markdown** - Generate formatted output with embedded AI prompts
- **Configurable AI Prompts** - Automatic commit message generation
- **Branch Context Preservation** - Include branch and repository info
- **Intelligent Repository Root Detection** - Automatic repo discovery

### 3. Unit Test Runner

- **Keyboard Shortcut** - Ctrl+T for immediate execution
- **Menu Integration** - File > Run Tests option
- **Progress Tracking** - Real-time progress bar with visual feedback
- **EMA Time Estimation** - Exponential Moving Average calculates accurate time-remaining
- **Output Persistence** - Results saved to dated files in Generated directory
- **Recent Files Integration** - Test results automatically added to Recent Files panel

### 4. Recent Files System

- **Auto-Registration** - All generated outputs automatically tracked
- **Drag-and-Drop Support** - Drag recent files to external applications
- **Advanced Filtering** - Filter by file type (Markdown, Git changes, tests)
- **Vector Store Linking** - Filter by vector store association
- **Automatic Cleanup** - 30-day retention policy removes old files
- **Configurable Retention** - Adjust MaxCount and RetentionDays via app.config

### 5. Smart File Filtering

**Exclusion Methods:**

- **Legacy Pattern Matching** - Wildcard-based filtering from app.config
- **.gitignore Support** - Full Git specification compliance via GitignoreParserNet
- **.vtignore Override** - VecTool-specific patterns override Git rules
- **Dual-Path Testing** - Directory vs. file name matching
- **Pluggable Architecture** - Swappable adapter implementations

---

## Architecture

### Project Structure

```
VecTool/
├── OaiUI/                    # Main WinForms application
├── Configuration/            # Settings and configuration abstractions
├── Constants/               # Centralized strings and tags
├── Core/                    # Business logic and metadata
│   ├── Metadata/           # FileMetadata, ExportMetadata, MetadataCollector
│   ├── Models/             # Data classes and records
│   ├── Helpers/            # GitRunner, RepoLocator, etc.
│   └── Abstractions/       # Interfaces and contracts
├── Handlers/               # Feature implementations
│   ├── Export/            # XmlMarkdownWriter, export handlers
│   ├── Traversal/         # FileSystemTraverser, path helpers
│   └── Validators/        # Content validators
├── RecentFiles/           # Recent files management
├── Utils/                 # Utility classes (MimeTypeProvider, etc.)
├── Log/                   # Logging infrastructure (NLog, LogCtx)
└── UnitTests/            # Comprehensive test suite
```

### Design Principles

VecTool strictly adheres to **SOLID principles**:

- **Single Responsibility** - Each class has one reason to change
- **Open/Closed** - New exclusion adapters without modifying core logic
- **Liskov Substitution** - Consistent adapter contracts
- **Interface Segregation** - Minimal, focused interfaces
- **Dependency Inversion** - Abstract dependencies throughout

### Key Abstractions

**Configuration:**
- `ISettingsStore` - Key-value storage abstraction
- `InMemorySettingsStore` - For testability
- `ConfigurationManagerAppSettingsReader` - For app.config integration

**File Handling:**
- `IFileSystemTraverser` - Recursive file enumeration with exclusions
- `IIgnorePatternMatcher` - Pluggable pattern matching adapters
- `IFileMarkerExtractor` - File-level exclusion markers

**Export:**
- `IMetadataWriter` - Format-agnostic metadata writing interface
- `XmlMarkdownWriter` - Hybrid XML-Markdown implementation
- `FileHandlerBase` - Abstract base for all export handlers

**Recent Files:**
- `IRecentFilesManager` - Recent files tracking interface
- `IRecentFilesStore` - Persistence abstraction

---

## Technology Stack

| Component | Technology | Version | Purpose |
|-----------|-----------|---------|---------|
| **Framework** | .NET | 8.0+ | Latest runtime |
| **UI** | Windows Forms | Net8.0 | Desktop application |
| **Testing** | NUnit + Shouldly | 4.x | Test framework and assertions |
| **Logging** | LogCtx + NLog | main branch | Structured logging |
| **Git Integration** | GitignoreParserNet | 0.2.0+ | .gitignore compliance |
| **Pattern Matching** | MAB.DotIgnore | 3.0.2+ | Alternative .ignore patterns |
| **DOCX Export** | DocumentFormat.OpenXml | 3.3.0+ | Office document generation |
| **PDF Export** | QuestPDF | 2025.12.1+ | PDF document generation |

---

## Getting Started

### Prerequisites

- **OS** - Windows 7 SP1 or later (Windows 10/11 recommended)
- **.NET Runtime** - .NET 10.0 or later
- **Git** - Version 2.0+ for repository operations
- **IDE** - Visual Studio 2026 (optional, for development)

### Installation

#### Option 1: Download Pre-Built Executable

```bash
# Download latest release from GitHub
# Extract to desired location
# Run: oaiUI.exe
```

#### Option 2: Build from Source

```bash
# Clone repository
git clone https://github.com/your-repo/VecTool.git
cd VecTool

# Restore dependencies
dotnet restore

# Build solution
dotnet build -c Release

# Publish self-contained executable
dotnet publish OaiUI/oaiUI.csproj \
  -r win-x64 \
  -c Release \
  -p:PublishSingleFile=true \
  --self-contained true

# Output: OaiUI/bin/Release/net8.0-windows/win-x64/publish/oaiUI.exe
```

### First Run

1. **Launch Application** - Double-click `oaiUI.exe`
2. **Create Vector Store** - Enter name and click "Create Vector Store"
3. **Select Folders** - Click "Select Folders" to choose source directories
4. **Export** - Press Ctrl+M to export to Markdown

---

## Configuration

### app.config

Application settings are stored in `app.config`:

```xml
<configuration>
  <appSettings>
    <!-- Recent Files Settings -->
    <add key="recentFilesMaxCount" value="200" />
    <add key="recentFilesRetentionDays" value="30" />
    <add key="recentFilesOutputPath" value="Generated" />
    
    <!-- Exclusion Settings -->
    <add key="excludedFiles" value=".log,.tmp,.bak" />
    <add key="excludedFolders" value="bin,obj,.git,.vs" />
    
    <!-- Git Settings -->
    <add key="gitAiPrompt" value="Analyze the following Git changes..." />
  </appSettings>
</configuration>
```

### uiState.json

UI preferences are automatically saved to `uiState.json`:

```json
{
  "recentFilesPanel": {
    "columnWidths": [300, 50, 120, 100],
    "rowHeight": 24,
    "fontSize": 10
  }
}
```

### vectorStoreFolders.json

Vector store associations and custom exclusion rules:

```json
{
  "stores": [
    {
      "name": "VecTool",
      "folderPaths": ["C:\\Git\\vectoolDevMaster"],
      "customExclusions": [".vtignore"]
    }
  ]
}
```

### .vtignore

VecTool-specific exclusion patterns (overrides .gitignore):

```
# Override Git rules
!important.log

# VecTool-specific patterns
*.temp
build/artifacts/
```

---

## Usage Guide

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| **Ctrl+M** | Convert to Markdown |
| **Ctrl+G** | Get Git Changes |
| **Ctrl+F** | File Size Summary |
| **Ctrl+T** | Run Unit Tests |
| **Ctrl+R** | Export to Repomix (future) |
| **Alt+F4** | Exit Application |

### Menu Reference

**File Menu**
- Convert to Markdown - Export selected folders to single MD file
- Get Git Changes - Retrieve Git diff/status as formatted MD
- File Size Summary - Generate file size analysis report
- Run Tests - Execute dotnet test with progress
- Export to Repomix - Future integration
- Exit - Close application

**Help Menu**
- About - Display version information and credits

---

## Testing

### Running Tests

```bash
# Run all tests
dotnet test UnitTests/UnitTests.csproj

# Run specific test class
dotnet test UnitTests/UnitTests.csproj \
  --filter FullyQualifiedName~FileSizeSummaryHandlerTests

# Run with verbose output
dotnet test UnitTests/UnitTests.csproj --verbosity detailed
```

### Test Infrastructure

- **Framework** - NUnit with Shouldly fluent assertions
- **Apartment** - Single-Threaded (STA) for WinForms compatibility
- **Parallelism** - Single worker to prevent UI resource conflicts
- **Coverage** - Configuration, handlers, UI components, architecture validation

### Test Example

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

## Version Schema

VecTool uses a **hierarchical versioning scheme** to track product maturity and development phase.

### Format

`Major.PlanId.BuildPart` where `BuildPart = (PlanPhase × 1000) + DayOfYear`

### Examples

- `4.80.2005` - Major 4, Plan 80, Phase 2, Day 5
- `4.80.2102` - Major 4, Plan 80, Phase 2, Day 102
- `4.81.3256` - Major 4, Plan 81, Phase 3, Day 256

### Release Stability

| Version Range | Status | Support |
|---|---|---|
| 4.80.xxxx | Production | Full support |
| 4.71.xxxx | Legacy | Bug fixes only |
| 4.70.xxxx | EOL | No support |

---

## Roadmap

### Phase 4.80 - Enhanced Metadata for All Export Formats

- **Phase 2** ✅ XML-Markdown hybrid format (completed)
- **Phase 3** 🔄 DOCX metadata writer with custom properties and tables
- **Phase 4** 🔄 PDF metadata writer with document properties and styling
- **Phase 5** 📋 Comprehensive test suite for metadata validation

### Future Phases

- **4.81** - Perplexity API integration for AI-assisted commit messages
- **4.82** - Settings UI for configuration review and editing
- **4.83** - Advanced filtering with regex pattern support
- **4.84** - GitHub API integration for pull requests
- **4.85** - Jira issue linking and Slack notifications
- **4.86** - Performance optimization and caching
- **4.87** - SEQ centralized logging integration

---

## Export Formats

### Markdown (XML-Wrapped)

VecTool exports projects as **hybrid XML-Markdown** files optimized for AI consumption.

**Benefits:**
- **Structured Metadata** - `loc`, `language`, `modified` attributes per file
- **AI-Optimized** - XPath/LINQ parsing for LLM context management
- **Human-Readable** - Syntax highlighting in CDATA blocks
- **Scalable** - Support for partial exports and line ranges

### DOCX Export (Phase 4.80.p3)

Future release will include DOCX export with:

- Custom document properties (TotalFiles, TotalLOC, ExportDate)
- Metadata tables before each code block
- Formatted headers and styling for professional documents
- Integration with Microsoft Office suite

### PDF Export (Phase 4.80.p3)

Future release will include PDF export with:

- PDF document metadata (Title, Author, Subject, CreationDate)
- Summary tables with file statistics
- Professional formatting with syntax highlighting
- Print-ready output

---

## Compatibility

### Supported Platforms

- **Windows 7 SP1+** - WinForms support
- **Windows 10/11** - Recommended
- **.NET 8.0 runtime** or later

### Supported .gitignore Features

- Basic glob patterns (`.log`, `temp/`)
- Negation patterns (`!important.log`)
- Directory matching (`bin/`, `obj/`)
- Comments (lines starting with `#`)
- Escaped special characters (`file.txt`)

### Git Operations

- **Git 2.0+** - Minimum version
- **SSH and HTTPS** - Both protocols supported
- **SSH Agent** - SSH key support

---

## How to Report Issues

When reporting bugs or requesting features, please include:

1. **Version** - Show from Help > About (e.g., 4.80.2005)
2. **OS** - Windows version and architecture (e.g., Windows 11 x64)
3. **Steps** - Exact steps to reproduce
4. **Expected** - What should happen
5. **Actual** - What actually happens
6. **Logs** - Recent entries from ConfigLogConfig.xml
7. **Attachments** - Screenshots or anonymized project structures

---

## Contributing

Contributions are welcome! Please follow these guidelines:

### Setup

```bash
# Fork and clone the repository
git clone https://github.com/your-fork/VecTool.git
cd VecTool

# Create feature branch
git checkout -b feat/your-feature

# Make changes and test
dotnet build
dotnet test

# Commit with conventional message
git commit -m "feat(scope): description"

# Push and open PR
git push origin feat/your-feature
```

### Commit Message Format

```
type(scope): subject

- TL;DR: Overall purpose summary
- Specific change with context
- Another change with details
```

### Commit Types

- `feat` - New feature
- `fix` - Bug fix
- `docs` - Documentation
- `refactor` - Code refactoring
- `test` - Testing improvements
- `chore` - Build/tooling changes
- `perf` - Performance improvements

### Code Style

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use file-scoped namespaces (`namespace VecTool.Core;`)
- Enable nullable reference types (`nullable enable`)
- Add XML documentation for public members
- Keep methods focused and testable

---

## Deprecation Policy

- **Announce** - Major version bump with clear communication
- **Support** - Maintain support for 2 major version cycles
- **Migrate** - Provide migration guide and tools
- **Remove** - Remove after 1 year notice period

---

## License

VecTool is licensed under the **Apache License 2.0**. See LICENSE file for details.

---

## Support & Community

- **Issues** - Report bugs on GitHub Issues
- **Discussions** - Feature requests and discussions on GitHub Discussions
- **Wiki** - Extended documentation and tutorials
- **Releases** - Download latest releases from Releases page

---

## Acknowledgments

- **GitignoreParserNet** - Provides full .gitignore specification compliance
- **MAB.DotIgnore** - Alternative .ignore pattern matching
- **LogCtx** - Structured logging abstraction
- **NLog** - Logging framework
- **NUnit** - Testing framework
- **Shouldly** - Fluent assertion library

---

**Latest Version**: 4.80.p2  
**Last Updated**: 2026-01-06  
**Status**: Active Development  
**Maintainer**: [Your Name/Organization]

For more information, visit the [GitHub Repository](https://github.com/zzt108/VecTool).
