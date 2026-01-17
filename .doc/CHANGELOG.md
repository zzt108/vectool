# CHANGELOG

All notable changes to VecTool are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/),
and this project adheres to [Semantic Versioning](https://semver.org/).

---

## [4.80.p2] - 2026-01-06

**Phase 2: XML-Markdown Hybrid Export Format**

### Added

- **XmlMarkdownWriter** - New handler for generating XML-wrapped Markdown exports
  - Metadata-enriched file format with attributes: `path`, `lines`, `loc`, `language`, `modified`
  - CDATA-wrapped code blocks with syntax highlighting tags
  - Document-level metadata: `totalFiles`, `totalLoc`, `exportDate`, `version`

- **Core.Metadata Infrastructure**
  - `FileMetadata` record - Per-file statistics (LOC, language, modified date, size)
  - `ExportMetadata` record - Document-level aggregates (total files, total LOC, version)
  - `MetadataCollector` class - Extracts and calculates file metrics

- **VersionInfo Extensions**
  - `DisplayVersion` property - Raw file version (e.g., "4.80.2005.2330")
  - `InformationalVersion` property - Semantic version (e.g., "4.80.p2")
  - Maintains backward compatibility with existing `GetSummary()` method

- **Enhanced MDHandler**
  - Pre-collects all files with metadata before streaming to output
  - Generates single-pass export with accurate document statistics
  - Improved error logging with export completion metrics

### Changed

- **Directory.Build.props** - Updated versioning to Plan 4.80, Phase 2
  - `PlanId`: 71 → 80
  - `PlanPhase`: 4 → 2

- **RecentFileType Enum** - Renamed for clarity
  - `Docx` → `Codebase_Docx`
  - `Pdf` → `Codebase_Pdf`
  - Aligns with enum naming conventions for export types

- **MDHandler.ExportSelectedFolders** - Architectural refactor
  - Replaced nested folder grouping with flat file collection
  - Removed manual `WriteFolderName()` folder headers (redundant with XML path attributes)
  - Switched to `XmlMarkdownWriter` for structured output generation

- **Core.csproj** - Added project reference to Utils
  - Enables `MimeTypeProvider` access for language detection fallback

### Files Added

- `Handlers/Export/XmlMarkdownWriter.cs` - Hybrid format generator
- `Core/Metadata/FileMetadata.cs` - File-level statistics record
- `Core/Metadata/ExportMetadata.cs` - Document-level aggregates record
- `Core/Metadata/MetadataCollector.cs` - Metadata extraction logic

### Files Modified

- `Constants/VersionInfo.cs` - Added DisplayVersion, InformationalVersion properties
- `Handlers/MDHandler.cs` - Refactored for XML metadata integration
- `Export.Docx/DocxExportHandler.cs` - Updated RecentFileType enum usage
- `Export.Pdf/PdfExportHandler.cs` - Updated RecentFileType enum usage
- `RecentFiles/RecentFileType.cs` - Removed Docx/Pdf, added Codebase_Docx/Codebase_Pdf
- `Directory.Build.props` - Version bump to 4.80.p2
- `Core/Core.csproj` - Added Utils project reference

### Technical Details

**XML-Markdown Hybrid Format Example:**

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

**Benefits:**
- AI-optimized structured format (LLM-friendly parsing via XPath/LINQ)
- Rich metadata for context planning (file size, language, modification dates)
- Human-readable code blocks with syntax highlighting
- Scalable for future partial exports (lines="50-120" ranges)

### Testing

- Existing MD export tests updated for new XmlMarkdownWriter format
- New metadata collection tests validate LOC calculation and language detection
- Integration tests verify document statistics accuracy

### Migration Notes

For users upgrading from v4.71.p4:

1. **Export Format Change** - MD exports now use XML wrapper
   - Existing tools expecting plain Markdown need XPath/LINQ parsing
   - Original code content remains in CDATA blocks
   - Recommended: Update LLM ingestion pipelines to parse XML structure

2. **RecentFileType Enum** - Enum values renamed
   - If persisting enum values to JSON, update mappings
   - Database migrations: `Docx` → `Codebase_Docx`, `Pdf` → `Codebase_Pdf`

### Next Phase (4.80.p3)

- DOCX export with embedded metadata tables
- PDF export with document properties and summary pages
- Unit test suite for all three export formats

---

## [4.71.p4] - 2025-12-27

**Stability and Bug Fix Release**

### features revived:
- DOCX export 
- PDF export 
- OpenAI VectorStore handling

### Fixed

- Recent Files auto-cleanup now respects retention policy
- UI state persistence now survives application crashes
- Git timeout now prevents hung processes
- Folder selection dialog now remembers last location
- Exclusion patterns now properly handle forward/backslashes

### Improved

- Error messages for file exclusions
- Refactored GitRunner for better testability
- Updated NLog configuration format
- Overall application responsiveness

---

## [4.71.p3] - 2025-11-15

**Progress Manager and UI Enhancements**

### Added

- Progress Manager with EMA (Exponential Moving Average) smoothing
  - Real-time progress bar with percentage display
  - Status label with EMA time-remaining estimation
  - Zero-division safety for edge cases

- Progress Panel UI component
  - Clean, uncluttered progress visualization
  - Integration with TestRunnerHandler

### Improved

- EMA smoothing (0.3 alpha) for stable time estimates
- Real-time UI updates during file processing
- Status text displays "X/Y Z - ETA HH:MM:SS" format

---

## [4.71.p2] - 2025-11-01

**Constants Library and Architecture**

### Added

- Constants project with centralized strings
  - Tags.cs for XML attribute names
  - Attributes.cs for test attributes
  - TestStrings.cs for reusable test data

- TagBuilder class for safe XML attribute escaping

### Changed

- Project reorganization into 8 focused projects
- Clear separation of concerns (UI, Business Logic, Configuration, Tests)
- Namespace organization: VecTool.Configuration, VecTool.Core, VecTool.Handlers

### Improved

- Architecture consistency across handlers
- Test data centralization
- XML generation safety

---

## [4.71.p1] - 2025-10-15

**Test Automation and Recent Files System**

### Added

- Unit Test Runner
  - Keyboard shortcut Ctrl+T for immediate execution
  - Menu integration via File > Run Tests
  - Real-time progress bar with visual feedback
  - EMA-based time estimation
  - Output persistence to dated files

- Recent Files System
  - Auto-registration of all generated outputs
  - Drag-and-drop support for external tool integration
  - Advanced filtering by file type and vector store
  - Automatic 30-day retention policy
  - Configurable retention settings

- Keyboard Shortcuts
  - Ctrl+M: Convert to Markdown
  - Ctrl+G: Get Git Changes
  - Ctrl+F: File Size Summary
  - Ctrl+T: Run Unit Tests

### Changed

- UI layout with tabbed interface
- Menu system reorganization
- Configuration consolidation to app.config

### Improved

- File traversal performance with single-pass collection
- Exclusion pattern caching
- UI responsiveness

---

## [4.70.p4] - 2025-09-25

**Major Architectural Refactoring**

### Breaking Changes

- Removed DOCX Export
  - Removed all DocX export handlers
  - Removed QuestPDF NuGet dependency
  - Removed DocX-related UI menu items
  - Rationale: Simplified codebase, Markdown superior for LLM consumption

- Removed PDF Export
  - Removed PDF export functionality
  - Removed PDF generation handlers
  - Rationale: Markdown and Repomix better serve workflow

- Removed OpenAI Vector Store Management
  - Removed direct OpenAI API integration
  - Removed vector store upload UI
  - Removed API key management
  - Rationale: Moving to Perplexity API in future phases

### Added

- Handler Delegation Pattern
  - FileHandlerBase abstract base class
  - Specific handlers: MarkdownExportHandler, GitChangesHandler, FileSizeSummaryHandler, TestRunnerHandler
  - Each handler has single responsibility

- Configuration Abstraction
  - ISettingsStore interface for key-value storage
  - InMemorySettingsStore for testability
  - ConfigurationManagerAppSettingsReader for app.config integration
  - UiStateConfig for UI preference persistence
  - VectorStoreConfig with 3-part structure

- Exclusion Pattern Adapter
  - IIgnorePatternMatcher interface
  - GitignoreParserNetAdapter for full .gitignore compliance
  - MabDotIgnoreAdapter as alternative matcher
  - LegacyConfigAdapter for backward compatibility
  - IgnoreMatcherFactory for adapter selection

- Git Integration Enhancements
  - GitRunner with process execution and timeout support
  - Cancellation tokens throughout async operations
  - RepoLocator for intelligent repository root detection
  - Timeout management to prevent hung operations

- UI State Persistence
  - uiState.json for column widths, row heights, font sizes
  - Restored on application startup
  - Saved on application close via Form.OnClosing

- Structured Logging
  - LogCtx integration throughout codebase
  - Structured context via `using var log = logger.SetContext(...)`
  - NLog configuration via ConfigLogConfig.xml
  - SEQ integration ready for centralized logging

### Changed

- Project structure: Split monolithic codebase into 8 focused projects
- Namespace organization per project (VecTool.Configuration, VecTool.Core, VecTool.Handlers, etc.)
- Configuration management to app.config with custom sections
- Git integration to process-based execution

### Improved

- Code testability via dependency injection
- Error messages and logging
- File traversal with exclusion pattern caching
- Application performance with lazy-loaded configuration

### Dependencies Changed

**Added:**
- System.Configuration.ConfigurationManager 9.0.10 - app.config support
- LogCtx (submodule) - Structured logging wrapper
- GitignoreParserNet 0.2.0.14 - Full .gitignore specification compliance

**Updated:**
- MAB.DotIgnore 2.x → 3.0.2 - Latest features

**Removed:**
- QuestPDF - PDF generation removed
- OpenAI SDK - Vector store management removed

### Testing

- NUnit 4.x with Shouldly assertions
- STA Apartment for WinForms compatibility
- Single Test Worker (LevelOfParallelism=1) to prevent UI conflicts

### Documentation

- VecTool-GUIDE.md - Usage guide with learning paths
- VecTool-SUMMARY.md - Architecture overview and design patterns
- VecTool-BI.md - Business logic, handlers, and configuration
- VecTool-UI.md - WinForms UI, components, and progress system
- VecTool-TESTS.md - Testing framework, fixtures, and patterns

---

## [4.70.p1] - 2025-08-10

**Initial Release with Core Export Functionality**

### Added

- Single-File Markdown Export
  - Export entire project hierarchies to unified files
  - Code-aware syntax highlighting tags
  - Hierarchical folder structure preservation
  - File metadata (LOC, complexity metrics)
  - AI-ready formatting with cross-references

- Git Integration
  - Get Git Changes - Retrieve diffs and status
  - Git Changes to Markdown - Formatted output with embedded AI prompts
  - Configurable AI prompts for automatic commit messages
  - Branch and repository context preservation
  - Intelligent repository root detection

- Recent Files Tracking
  - Auto-registration of all generated outputs
  - File filtering and search
  - Drag-and-drop support

- File Filtering with .gitignore Support
  - Full Git specification compliance
  - Pattern-based exclusions
  - Legacy configuration support

- WinForms UI
  - Tabbed interface for different operations
  - Real-time progress tracking
  - Configuration management
  - Responsive layout

### Technical Stack

- Framework: .NET 8.0
- UI: Windows Forms
- Testing: NUnit with Shouldly
- Logging: NLog with structured logging
- Git Integration: GitignoreParserNet, MAB.DotIgnore

---

## Version Numbering Scheme

VecTool follows a hierarchical versioning scheme to track both product maturity and development
phase progress.

### Format

`Major.PlanId.BuildPart` where `BuildPart = (PlanPhase * 1000) + DayOfYear`

### Examples

- `4.80.2005` - Major 4, Plan 80, Phase 2, Day 5
- `4.80.2102` - Major 4, Plan 80, Phase 2, Day 102
- `4.81.3256` - Major 4, Plan 81, Phase 3, Day 256

### Release Stability

| Version Range | Status | Support |
|---|---|---|
| 4.80.xxxx | **Production** | Full support |
| 4.71.xxxx | **Legacy** | Bug fixes only |
| 4.70.xxxx | **EOL** | No support |

---

## Compatibility

### Supported Platforms

- Windows 7 SP1+ (WinForms)
- Windows 10/11 (recommended)
- .NET 8.0 runtime or later

### Supported .gitignore Features

- Basic glob patterns (`.log`, `temp/`)
- Negation patterns (`!important.log`)
- Directory matching (`bin/`, `obj/`)
- Comments (lines starting with `#`)
- Escaped special characters (`file.txt`)

### Git Operations

- Git 2.0+
- SSH and HTTPS protocols supported
- SSH agent support for SSH keys

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

## Deprecation Policy

- **Announce** - Major version bump with clear communication
- **Support** - Maintain support for 2 major version cycles
- **Migrate** - Provide migration guide and tools
- **Remove** - Remove after 1 year notice period

---

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch: `git checkout -b feat/your-feature`
3. Commit with conventional commits: `git commit -m "feat(scope): description"`
4. Push to branch: `git push origin feat/your-feature`
5. Open a Pull Request

### Commit Message Format

```
type(scope): subject

- TL;DR: Overall purpose summary
- Specific change with context
- Another change with details
```

### Types

- `feat` - New feature
- `fix` - Bug fix
- `docs` - Documentation
- `refactor` - Code refactoring
- `test` - Testing improvements
- `chore` - Build/tooling changes
- `perf` - Performance improvements

---

**Latest Version**: 4.80.p2  
**Last Updated**: 2026-01-06  
**Status**: Active Development
