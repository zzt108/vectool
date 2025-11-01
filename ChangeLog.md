# CHANGELOG

All notable changes to VecTool are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [4.5.25.1101] - 2025-11-01

Major architectural refactoring with modular project reorganization, new feature suite, and breaking changes. This version represents a significant evolution toward a more maintainable, testable codebase.

### ❌ Breaking Changes

#### Removed DOCX Export
- ❌ Removed all DocX export handlers
- ❌ Removed QuestPDF NuGet dependency
- ❌ Removed DocX-related UI menu items and buttons
- ❌ Removed DocX test suite
- **Rationale**: Simplified codebase; Markdown is superior for LLM consumption

#### Removed PDF Export
- ❌ Removed PDF export functionality
- ❌ Removed QuestPDF integration
- ❌ Removed PDF generation handlers
- **Rationale**: Markdown + Repomix better serves workflow

#### Removed OpenAI Vector Store Management
- ❌ Removed direct OpenAI API integration
- ❌ Removed vector store upload UI
- ❌ Removed API key management
- **Rationale**: Moving to Perplexity API in future phases

### ✅ New Features

#### Unit Test Runner
- ✅ **Keyboard Shortcut**: Ctrl+T for immediate test execution
- ✅ **Menu Integration**: File → Run Tests option
- ✅ **Progress Tracking**: Real-time progress bar with visual feedback
- ✅ **EMA Time Estimation**: Exponential Moving Average calculates accurate time-remaining
- ✅ **Output Persistence**: Results saved to dated files in Generated/ directory
- ✅ **Recent Files Integration**: Test results automatically added to Recent Files panel
- **Implementation**: `TestRunnerHandler` delegates to `ProgressManager` and `DatedFileWriter`

#### Recent Files System
- ✅ **Auto-Registration**: All generated outputs automatically tracked
- ✅ **Drag-and-Drop Support**: Drag recent files to external applications
- ✅ **Advanced Filtering**: Filter by file type (Markdown, Git changes, tests)
- ✅ **Vector Store Linking**: Filter by vector store association
- ✅ **Automatic Cleanup**: 30-day retention policy removes old files
- ✅ **Configurable Retention**: Adjust MaxCount and RetentionDays via app.config
- **Implementation**: `RecentFilesManager`, `RecentFilesPanel` (WinForms UserControl)

#### Progress Manager
- ✅ **EMA Smoothing**: Exponential Moving Average (α=0.3) for stable time estimates
- ✅ **Real-Time Updates**: UI refreshes per item processed
- ✅ **Status Text**: Displays "X/Y (Z%) - ETA HH:MM:SS" format
- ✅ **Zero-Division Safety**: Gracefully handles empty/zero totals
- **Implementation**: `ProgressManager` + `ProgressPanel` UI component

#### Constants Library
- ✅ **Centralized Strings**: All magic strings moved to Constants project
- ✅ **Tag Builder**: Safe XML attribute escaping via `TagBuilder` class
- ✅ **Test Constants**: Reusable test data in `TestStrings` class
- ✅ **Architecture Tests**: Validates Constants library consistency
- **Files**: `Constants/Tags.cs`, `Constants/Attributes.cs`, `Constants/TestStrings.cs`

### 🏗️ Architecture Improvements

#### Project Restructuring
- 🏗️ Split monolithic codebase into 8+ focused projects
- 🏗️ Clear separation of concerns: UI, Business Logic, Configuration, Tests
- 🏗️ Namespace organization: `VecTool.Configuration`, `VecTool.Core`, `VecTool.Handlers`, etc.
- 🏗️ Each project has single, well-defined responsibility

#### Configuration Abstraction
- 🏗️ **ISettingsStore** interface for key-value storage
- 🏗️ **InMemorySettingsStore** for testability
- 🏗️ **ConfigurationManagerAppSettingsReader** for app.config integration
- 🏗️ **UiStateConfig** consolidates UI preference persistence
- 🏗️ **VectorStoreConfig** (3-part: Core, Filtering, Persistence) for store management

#### Exclusion Pattern Adapter
- 🏗️ **IIgnorePatternMatcher** interface enables swappable implementations
- 🏗️ **GitignoreParserNetAdapter** for full .gitignore compliance
- 🏗️ **MabDotIgnoreAdapter** as alternative matcher
- 🏗️ **LegacyConfigAdapter** for backward-compatible wildcard patterns
- 🏗️ **IgnoreMatcherFactory** centralizes adapter selection

#### Handler Delegation Pattern
- 🏗️ **FileHandlerBase** abstract base class
- 🏗️ Specific handlers: `MarkdownExportHandler`, `GitChangesHandler`, `FileSizeSummaryHandler`, `TestRunnerHandler`
- 🏗️ Each handler has single responsibility; delegates to utility classes
- 🏗️ Consistent `Execute(folders, config)` contract

#### Git Integration Enhancements
- 🏗️ **GitRunner** process execution with timeout support
- 🏗️ **Cancellation tokens** throughout async operations
- 🏗️ **RepoLocator** intelligent repository root detection
- 🏗️ **Timeout management** prevents hung operations

#### UI State Persistence
- 🏗️ **uiState.json** stores column widths, row heights, font sizes
- 🏗️ Restored on application startup
- 🏗️ Saved on application close via `Form.OnClosing()`
- 🏗️ Supports per-panel layout preferences

#### Structured Logging
- 🏗️ **LogCtx integration** throughout codebase
- 🏗️ Structured context via `using var _ = log.Ctx.Set(props)`
- 🏗️ NLog configuration via `Config/LogConfig.xml`
- 🏗️ SEQ integration ready for centralized logging

### 📦 Dependencies Changed

#### Added
| Package | Version | Purpose |
|---------|---------|---------|
| System.Configuration.ConfigurationManager | 9.0.10 | app.config support |
| LogCtx (submodule) | main | Structured logging wrapper |

#### Removed
| Package | Reason |
|---------|--------|
| QuestPDF | PDF export removed |
| OpenAI API | Vector store mgmt moved |

#### Updated
| Package | Old Version | New Version | Reason |
|---------|-------------|-------------|--------|
| GitignoreParserNet | 0.1.x | 0.2.0.14 | Full spec compliance |
| MAB.DotIgnore | 2.x | 3.0.2 | Latest features |

### 🧪 Testing Enhancements

#### Test Infrastructure
- ✅ **NUnit 4.x** with Shouldly assertions
- ✅ **STA Apartment** for WinForms compatibility
- ✅ **Single Test Worker** (LevelOfParallelism=1) prevents UI conflicts
- ✅ **AssemblyAttributes.cs** configures test environment

#### Test Coverage Expansion
- ✅ **Configuration Tests**: `RecentFilesConfigTests`, `VectorStoreConfigTests`, `UiStateConfigTests`
- ✅ **Handler Tests**: `FileSizeSummaryHandlerTests`, `ConvertSelectedFoldersToMDTests`, `GitChangesHandlerTests`
- ✅ **UI Tests**: `MainFormRecentFilesTabTests`, `RecentFilesPanelTests`, `ProgressPanelTests`
- ✅ **Architecture Tests**: `ConstantsArchitectureTests` validates Constants library

#### Test Fixtures
- ✅ **DocTestBase** provides common setup/teardown for handler tests
- ✅ **MockRecentFilesManager** facilitates handler testing
- ✅ **InMemoryAppSettingsReader** enables configuration testing
- ✅ **FakeClock** for deterministic time-based tests

### 📝 Documentation

#### New Documentation Files
- ✅ **VecTool-GUIDE.md** - Document usage guide with learning paths
- ✅ **VecTool-SUMMARY.md** - Architecture overview & design patterns
- ✅ **VecTool-BI.md** - Business logic, handlers, configuration
- ✅ **VecTool-UI.md** - WinForms UI, components, progress system
- ✅ **VecTool-TESTS.md** - Testing framework, fixtures, patterns

#### Documentation Organization
- 📖 Combined ~628 KB codebase documentation split into 5 focused documents
- 📖 ~71 KB total (~85-90% token reduction vs. full codebase)
- 📖 Cross-document references and learning paths
- 📖 Separate guides for each technical audience

### 🔄 Git Integration Updates

#### Git Changes Handler Improvements
- 🔄 Enhanced diff parsing for better formatting
- 🔄 Configurable AI prompts via `gitAiPrompt` app.config setting
- 🔄 Branch context preservation in output
- 🔄 Repository detection via `RepoLocator.FindRepoRoot()`

#### Git Timeout Handling
- 🔄 Configurable timeouts per Git operation
- 🔄 Cancellation token support throughout
- 🔄 Graceful error handling for hung processes

### 🖥️ UI/UX Enhancements

#### Menu System Refinement
- 🖥️ Consistent keyboard shortcuts (Ctrl+M, Ctrl+G, Ctrl+F, Ctrl+T)
- 🖥️ Clear menu grouping (File operations, Help)
- 🖥️ Future Repomix integration placeholder

#### Recent Files Panel
- 🖥️ DataGridView with sortable columns (FileName, Type, Date, Size, Folders)
- 🖥️ Drag-drop enabled for external tool integration
- 🖥️ Type filtering (Codebase MD, Git changes, test results)
- 🖥️ Vector store filtering for organized workflow

#### Progress Visualization
- 🖥️ ProgressBar with percentage display
- 🖥️ Status label with EMA time-remaining estimate
- 🖥️ Clean, uncluttered progress panel design

### ⚙️ Configuration Changes

#### app.config Enhancements
```xml
<!-- New settings -->
<add key="recentFilesMaxCount" value="200" />
<add key="recentFilesRetentionDays" value="30" />
<add key="recentFilesOutputPath" value="Generated" />
<add key="gitAiPrompt" value="..." />
```

#### Configuration File Additions
- ✅ `uiState.json` - UI layout persistence
- ✅ `vectorStoreFolders.json` - Store-to-folder associations
- ✅ `.vtignore` - VecTool-specific exclusion override

### 🔐 Security Improvements

- ✅ No hardcoded credentials in codebase
- ✅ Secure file permissions on generated output
- ✅ Input validation on all user-facing methods
- ✅ Null-safe operations throughout

### ⚡ Performance Optimizations

- ⚡ Lazy-loaded configuration (on-demand)
- ⚡ Cached exclusion patterns (reduces re-parsing)
- ⚡ Single file traversal pass (collects all metadata at once)
- ⚡ EMA progress calculation avoids expensive statistics

### 🐛 Bug Fixes

- 🐛 Fixed: Recent Files auto-cleanup now respects retention policy
- 🐛 Fixed: UI state persistence now survives application crashes
- 🐛 Fixed: Git timeout now prevents hung processes
- 🐛 Fixed: Folder selection dialog now remembers last location
- 🐛 Fixed: Exclusion patterns now properly handle forward/backslashes

### 📋 Versioning Schema

New hierarchical versioning scheme implemented:

```
AssemblyVersion: {Major}.0.0.0
FileVersion: {Major}.{PlanId}.{BuildPart}.{HHmm}
  where BuildPart = (PlanPhase * 1000) + DayOfYear
ApplicationDisplayVersion: {Major}.{PlanId}.p{PlanPhase}
```

**Version Breakdown for 1.25.1005**:
- **1** = Major version
- **25** = PlanId (25-phase development plan)
- **1005** = BuildPart (1 = phase 1; 005 = day 5 of year)
- **ApplicationDisplayVersion**: 1.25.p1

### 📚 Migration Guide

#### For Users Upgrading from v1.24
1. ❌ **DOCX/PDF Export**: Use Markdown export instead; benefits include better LLM compatibility
2. ❌ **Vector Store Upload**: Store associations in `vectorStoreFolders.json` (backward compatible)
3. ✅ **New Unit Test Runner**: Try Ctrl+T keyboard shortcut
4. ✅ **Recent Files**: Check new Recent Files tab for generated artifacts
5. ✅ **Exclusions**: May override with `.vtignore` if needed

#### For Developers
1. 🔄 Update project references (new Constants library required)
2. 🔄 Migrate custom handlers to inherit `FileHandlerBase`
3. 🔄 Use `IRecentFilesManager` for artifact tracking
4. 🔄 Update tests to use `InMemorySettingsStore`
5. 🔄 Add `#nullable enable` to new files

---

## [1.24.0925] - 2025-09-25

### 📊 Confidence: 8/10

Incremental release focusing on bug fixes and stabilization.

### ✅ Added
- Recent files timestamp tracking
- Git ignore pattern caching
- UI responsiveness improvements

### 🐛 Fixed
- Export handler null reference exception
- Configuration loading race condition
- File system watcher memory leak

### 🏗️ Changed
- Improved error messages for file exclusions
- Refactored GitRunner for better testability
- Updated NLog configuration format

---

## [1.23.0810] - 2025-08-10

### 📊 Confidence: 7/10

Initial release with core export functionality.

### ✅ Added
- Single-file Markdown export
- Git repository integration
- Recent files tracking
- File filtering with .gitignore support
- WinForms UI with tabbed interface

### 🏗️ Changed
- Project restructured into modular components
- Configuration moved to app.config
- Logging infrastructure refactored

---

## Unreleased (Next Phase)

### 🔮 Planned Features

#### Phase 2: Perplexity API Integration
- [ ] AI-assisted commit message generation
- [ ] Configurable prompt templates
- [ ] API key secure storage
- [ ] Response streaming UI

#### Phase 3: Settings UI
- [ ] Configuration review/editing interface
- [ ] Per-store custom settings
- [ ] UI for exclusion pattern management
- [ ] Retention policy configuration

#### Phase 4: Advanced Filtering
- [ ] Regular expression pattern support
- [ ] Composite filter rules
- [ ] Filter templates and presets
- [ ] Export filter configurations

#### Phase 5: Integrations
- [ ] GitHub API integration for pull requests
- [ ] GitLab API support
- [ ] Jira issue linking
- [ ] Slack notifications

#### Phase 6: Performance
- [ ] Async file traversal
- [ ] Progress streaming for large exports
- [ ] Caching layer for repeated exports
- [ ] Memory profiling and optimization

#### Phase 7: Enhanced Logging
- [ ] SEQ centralized logging integration
- [ ] Real-time log viewer in UI
- [ ] Structured metrics collection
- [ ] Performance profiling

### 🔮 Future Considerations

- macOS/Linux support (refactor to .NET MAUI)
- Cloud vector store integration
- Batch export operations
- CI/CD pipeline integration
- Automated release notes generation
- Web dashboard for project monitoring

---

## Version Numbering

VecTool follows a hierarchical versioning scheme to track both product maturity and development phase progress:

### Format
```
{Major}.{PlanId}.{BuildPart}
  where BuildPart = (PlanPhase * 1000) + DayOfYear
```

### Examples
- `1.25.1005` = Major 1, Plan 25, Phase 1, Day 5
- `1.25.2102` = Major 1, Plan 25, Phase 2, Day 102
- `1.26.3256` = Major 1, Plan 26, Phase 3, Day 256

### Release Stability

| Version Range | Status | Support |
|---------------|--------|---------|
| 1.25.xxxx | Production | Full support |
| 1.24.xxxx | Legacy | Bug fixes only |
| 1.23.xxxx | EOL | No support |

---

## Compatibility

### Supported Platforms
- Windows 7 SP1+ (WinForms)
- Windows 10 / 11 (recommended)
- .NET 8.0 runtime or later

### Supported .gitignore Features
- ✅ Basic glob patterns (`*.log`, `temp/*`)
- ✅ Negation patterns (`!important.log`)
- ✅ Directory matching (`bin/`, `obj/`)
- ✅ Comments (`# ignored`)
- ✅ Escaped special characters (`\#file.txt`)

### Git Operations
- Git 2.0+
- SSH and HTTPS protocols supported
- SSH agent support (for SSH keys)

---

## How to Report Issues

When reporting bugs or requesting features, please include:

1. **Version**: Show from Help → About (e.g., 1.25.1005)
2. **OS**: Windows version and architecture (e.g., Windows 11 x64)
3. **Steps**: Exact steps to reproduce
4. **Expected**: What should happen
5. **Actual**: What actually happens
6. **Logs**: Recent entries from Config/LogConfig.xml
7. **Attachments**: Screenshots or anonymized project structures

---

## Deprecation Policy

- **Announce**: Major version bump with clear communication
- **Support**: Maintain support for 2 major version cycles
- **Migrate**: Provide migration guide and tools
- **Remove**: Remove after 1+ year notice period

---

**Latest Version**: 4.5.25.1005  
**Last Updated**: November 1, 2025  
**Repository**: [GitHub - VecTool](https://github.com/your-repo/VecTool.git)