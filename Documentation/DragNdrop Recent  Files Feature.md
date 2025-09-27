
# Recent Files Feature – GPT-5 Step-by-Step Implementation Plan

## **Overview**

Design and implement a feature to track, display, and manage “recently generated files,” allowing the user to drag and drop files into a web browser. Each step includes validation and test requirements to ensure reliability.

***

## **Requirements**

- Recent Period: **15 days** (configurable)
- Max Files: **10 recent files** (configurable)
- Storage: **%AppData%/VecTool/Generated** (not in any repo)
- Metadata file: `recentFiles.json` (file path, type, gen date, source folder list, etc.)
- App: WinForms, C\#, .NET, NUnit, Shouldly, standard config patterns

***

## **Implementation Phases/Steps**


***

### **Step 1: Configuration Infrastructure**

**Objective:**
Introduce config settings for recent files list.

**Deliverables**

- Add `recentFilesMaxCount`, `recentFilesRetentionDays`, `recentFilesOutputPath` to app.config
- Ensure VectorStoreConfig or equivalent loads them

**Validation**

- Manual test: configs load with correct values and defaults
- NUnit test: config keys read, edge values covered

***

### **Step 2: RecentFileInfo Model**

**Objective:**
Data structure for tracking a file’s metadata

**Deliverables**

- Model with: FilePath, GeneratedAt, FileType, SourceFolders, FileSizeBytes, IsValid
- Serialization to/from JSON

**Validation**

- NUnit: round-trip JSON
- Validate handling of missing/corrupt properties
- File existence check returns expected results

***

### **Step 3: RecentFilesManager Core Logic**

**Objective:**
Central logic for managing the tracked files

**Deliverables**

- Methods: RegisterGeneratedFile, GetRecentFiles, CleanupExpiredFiles, Save/LoadMetadata
- Thread safety \& exception handling

**Validation**

- NUnit: register file, handle dups, persistence ok, expiry logic correct
- Simulate concurrent access

***

### **Step 4: Output Directory Management**

**Objective:**
Ensure output directory exists, with fallback/error handling

**Deliverables**

- Ensure %AppData%/VecTool/Generated exists
- Cleanup logic for old files

**Validation**

- NUnit: create dir, handle permission errors
- Files older than retention period are deleted

***

### **Step 5: Export Handler Integration**

**Objective:**
Ensure all export operations record their files to tracking

**Deliverables**

- Modify DocX, PDF, Git, summary export methods to register file

**Validation**

- NUnit or integration: verify registering works across all handlers
- No breakage of original export behavior

***

### **Step 6: Recent Files UI Panel**

**Objective:**
UI display of recent files, filterable and refreshable

**Deliverables**

- New UserControl or TabPage
- ListView with columns: Name, Type, Date, Size, Source
- Refresh and filter controls

**Validation**

- Manual: visible and responsive UI
- NUnit/UI: ListView populates correctly, filters work

***

### **Step 7: Drag-and-Drop Support**

**Objective:**
Enable dragging files from the UI panel to browser for upload

**Deliverables**

- Mouse event handlers, DataObject with FileDrop, multi-select
- Block drag if files missing

**Validation**

- Manual: browser accepts files dropped
- NUnit/UI: simulate drag events, block on missing files

***

### **Step 8: Main UI Integration**

**Objective:**
Add the UI panel as a tab or main navigation entry

**Deliverables**

- Modification to MainForm, TabControl, etc.

**Validation**

- Manual: tab visible, nav works
- NUnit/UI: panel refreshes on tab select

***

### **Step 9: File Operations Context Menu**

**Objective:**
Files can be opened, shown in Explorer, deleted, or path copied

**Deliverables**

- Context menu for ListView with shell actions

**Validation**

- Manual: all menu ops act as expected
- NUnit/UI: handle error cases, e.g., file missing

***

### **Step 10: Automatic Cleanup Logic**

**Objective:**
Purge expired or excess files on startup and periodically

**Deliverables**

- Cleanup on app start, periodic, and when adding files

**Validation**

- NUnit: assert list never > max, no expired files

***

### **Step 11: Comprehensive Unit Testing**

**Objective:**
Ensure high coverage and reliability

**Deliverables**

- All core logic \& model code is covered by unit/integration/UI tests
- Mock/stub filesystem for isolation

**Validation**

- >90% test coverage by NUnit/Shouldly
- Test edge, error, and high-load cases

***

### **Step 12: Error Handling \& Logging**

**Objective:**
Robust logs and user feedback

**Deliverables**

- Comprehensive try/catch, NLog usage (or current logging pattern)
- User-visible error messages for critical cases

**Validation**

- Tests: logs on errors
- Manual: app recovers gracefully after simulated failures

***

## **Dependencies / Execution Order**

1. Steps 1–4: Foundation, can run mostly in parallel but config/model first
2. Steps 5–6: Handlers/UI depend on foundation
3. Steps 7–9: Full UX, follows UI integration
4. Steps 10–12: Reliability and production polish

***

**Success Criteria for Each Step**

- All new functionality covered by NUnit tests, with clear pass/fail signals
- Step passes all validation items before proceeding to next phase

***

*End of plan*
