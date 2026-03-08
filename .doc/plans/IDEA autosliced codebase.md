# only creates a subset for a user specified file. 
New Idea 2025 12 27

Automatic generation of separate UI, Backend, test parts. 
Probably 
- other big separateble parts?
- intelligent recognition of these. 


No readme and no mumbo jumbo. just the project sources containing the file.

## High-Level Solution Overview

Egy **Project Subset Generator** feature-t szeretnél, ami egy user-által megadott file alapján automatikusan csak a **releváns project fájlokat** gyűjti össze – **README és egyéb fluff nélkül**.

### Core Concept

A feature három fő komponensből állna:

**1. File → Project Resolver**

- User kiválaszt egy tetszőleges source file-t a codebase-ből
- A rendszer meghatározza, hogy melyik `.csproj` projekthez tartozik (traverse up directory hierarchy until `.csproj` found)
- Opcionális: detect transitive project dependencies (analyze `<ProjectReference>` elements)

**2. Smart Filter**

- Csak **source files** kerülnek be (`.cs`, `.csproj`, config files)
- **Exclude**: `README.md`, `ChangeLog.md`, `.git`, `bin/`, `obj/`, documentation
- Respect existing exclusion patterns (`excludedFiles`, `excludedFolders` from `app.config`)[^1_1]

**3. Output Generator**

- Reuse existing `MarkdownHandler` infrastructure[^1_1]
- Generate a single `.md` file with **project scope only**
- Add to **Recent Files** list with new type `RecentFileType.ProjectSubset`[^1_1]

***

## Questions for AI Context

Ahhoz, hogy az AI részletes implementációt tudjon adni, ezeket a kérdéseket kellene megválaszolnia:

### Architecture Decisions

1. **Scope Level**: Only the immediate project, or include transitive `<ProjectReference>` dependencies automatically?
2. **Entry Point**: New button in UI, context menu, or extend existing "Convert to MD" with a "Project Subset Mode" checkbox?
3. **File Selection**: FolderBrowserDialog → user picks any file, or integrate with existing folder selection mechanism?

### Technical Details

4. **Project Detection Logic**: Traverse parent directories until `.csproj` found, or use MSBuild APIs (`Microsoft.Build` NuGet)?
5. **Exclusion Strategy**: Hardcode common patterns (`*.md`, `bin/`, `obj/`), or make it configurable via new `app.config` setting?
6. **Dependency Resolution**: Parse `.csproj` XML manually, or use Roslyn/MSBuild workspace APIs?

### Integration Points

7. **Reuse vs. New Handler**: Extend `MarkdownHandler` with a `ProjectSubsetMode` flag, or create dedicated `ProjectSubsetHandler`?
8. **UI Flow**: Should Recent Files panel show "Project: `<ProjectName>`" instead of folder paths for this file type?
9. **Progress Reporting**: Integrate with existing `ProgressManager`, or simple status label?[^1_1]

***

## Minimal Implementation Path

A legegyszerűbb kezdés **Phase 1** lenne:


| Component | Approach |
| :-- | :-- |
| **Project Detection** | Directory.GetParent loop until `.csproj` found |
| **File Collection** | Enumerate project dir, filter by hardcoded extensions (`.cs`, `.csproj`, `.config`) |
| **Output** | Call existing `MdHandler.Convert()` with filtered file list |
| **UI** | Single button: "Export Project Subset" → file picker → output to `Generated/` |

**Later Phases** could add dependency graph analysis, customizable filters, UI polish.[^1_1]

***

Mit gondolsz erről a high-level tervről? Melyik kérdésekben kéne először dönteni, hogy az AI implementálni tudja? 🎯

<div align="center">⁂</div>

[^1_1]: VecToolDevMain_master_codebase.md


---

# Architecture Decisions

Scope Level: Only the immediate project, or include transitive <ProjectReference> dependencies automatically?
There should be a highlevel file about the contracts, contracts derived from tests, readme, rtc.

Entry Point: New button in UI, context menu, or extend existing "Convert to MD" with a "Project Subset Mode" checkbox?
a textbox accepting a file name which the AI asked for

File Selection: FolderBrowserDialog → user picks any file, or integrate with existing folder selection mechanism? see above

Technical Details
Project Detection Logic: Traverse parent directories until .csproj found, or use MSBuild APIs (Microsoft.Build NuGet)?
theoretically this should not be dotnet only feature. I use Dart/Kotlin also. but for now it can be dotnet focused as I use it 95%.

Exclusion Strategy: Hardcode common patterns (*.md, bin/, obj/), or make it configurable via new app.config setting?
per project settings currently in a limbo, it should be revived (UI is not showing them) and integrated enhanced to support this function

Dependency Resolution: Parse .csproj XML manually, or use Roslyn/MSBuild workspace APIs?
everything (or as much as possible) should be automatic

Integration Points
Reuse vs. New Handler: Extend MarkdownHandler with a ProjectSubsetMode flag, or create dedicated ProjectSubsetHandler?
I guess dedicated one descending from md handler

UI Flow: Should Recent Files panel show "Project: <ProjectName>" instead of folder paths for this file type? It should be clear which vectorstore owns this project, otherwise seems ok.

Progress Reporting: Integrate with existing ProgressManager, or simple status label?​ integrate.

📊 **Confidence: 9/10**

Perfect! Köszönöm a részleteket. Most már világos kép van, hogyan épül fel az architektúra. Itt a high-level solution overview, ami alapján az AI implementálni tudja.[^2_1]

***

## 🎯 Feature: **Project Subset Generator**

### Purpose

User megad egy file path-t (textbox), a feature automatikusan detektálja a parent `.csproj`-t, összegyűjti a project **source files-t**, és generál egy **AI-optimized Markdown** subset-et – **contracts, interfaces, test-derived contracts, README** kiemeléssel.[^2_1]

***

## Architecture Overview

### Component Hierarchy

```
ProjectSubsetHandler : MDHandler
├─ IProjectDetector (interface)
│  └─ CsProjDetector (impl: traverse up until .csproj)
│     └─ Future: DartPubspecDetector, KotlinGradleDetector
├─ IContractExtractor (interface)
│  └─ CSharpContractExtractor (impl: parse interfaces, public APIs)
├─ ProjectFileCollector
│  └─ Reuses FileSystemTraverser from MDHandler
└─ RecentFilesManager integration (RecentFileType.ProjectSubsetMd)
```


### Key Design Decisions

| Aspect | Decision | Rationale |
| :-- | :-- | :-- |
| **Scope** | Single project only (no transitive deps) | Simplified Phase 1; transitive refs can be Phase 2[^2_1] |
| **Entry Point** | TextBox `txtTargetFilePath` + Button `btnExportProjectSubset` | User pastes any file path; AI can ask for it explicitly[^2_1] |
| **Project Detection** | Directory.GetParent() loop until `.csproj` found | .NET-focused MVP; extensible via `IProjectDetector` for Dart/Kotlin[^2_1] |
| **Exclusion Strategy** | **Revive PerVectorStoreSettings UI + extend for project-level config** | Settings tab already exists but hidden; needs UI wiring + new `ProjectSubsetConfig` section[^2_1] |
| **Auto-detection** | Parse `.csproj` XML manually (no MSBuild) | Avoids heavy dependencies; XDocument for `<Compile>`, `<Content>` tags[^2_1] |
| **Handler Type** | `ProjectSubsetHandler : MDHandler` | Inherits AI context generation, delegates to FileSystemTraverser[^2_1] |
| **Recent Files** | Show as `"Project: <ProjectName> [VectorStore: <Name>]"` | Clear ownership; `RecentFileType.ProjectSubsetMd`[^2_1] |
| **Progress** | Use existing `ProgressManager` via `IUserInterface.WorkStart/UpdateProgress/WorkFinish`[^2_1] |  |


***

## Implementation Phases

### Phase 1: Core Project Detection \& Export

**Goal:** User enters file path → exports project sources as Markdown.[^2_1]


| \# | Component | Type | Why |
| :-- | :-- | :-- | :-- |
| 1 | **IProjectDetector** | New Interface | Abstract project file detection (`.csproj`, `.pubspec.yaml`, `build.gradle.kts`) |
| 2 | **CsProjDetector** | New Class | Traverse parent dirs until `.csproj` found; return project root + name |
| 3 | **ProjectSubsetHandler** | New Handler | Extends `MDHandler`; uses `CsProjDetector` + reuses `FileSystemTraverser`[^2_1] |
| 4 | **UI Controls** | MainForm Extension | `txtTargetFilePath` (TextBox), `btnExportProjectSubset` (Button), validation logic |
| 5 | **RecentFileType.ProjectSubsetMd** | Enum Addition | Add to `RecentFileType` enum for tracking[^2_1] |

**Expected Output:**

```markdown
# Project: VecTool.Core

## Contracts & Interfaces
- IProjectDetector.cs
- IRecentFilesManager.cs

## Implementation
- RecentFilesManager.cs
- FileSystemTraverser.cs

## Tests (Derived Contracts)
- RecentFilesManagerTests.cs (verifies contract)
```


***

### Phase 2: Contract Extraction \& High-Level Summary

**Goal:** Prepend **AI-guidance section** with contracts, public APIs, README.[^2_1]


| \# | Component | Type | Why |
| :-- | :-- | :-- | :-- |
| 6 | **IContractExtractor** | New Interface | Parse C\# files for `interface`, `public class`, `public method` signatures |
| 7 | **CSharpContractExtractor** | New Class | Uses Roslyn `CSharp.Syntax` to extract contracts (avoid regex) |
| 8 | **ProjectSubsetHandler Enhancement** | Modify Existing | Inject contract summary at start of MD file |

**Expected AI Guidance Section:**

```markdown
## AI Context: Project Contracts

### Public Interfaces
- `IRecentFilesManager` (5 methods)
- `IUserInterface` (4 methods)

### Key Public Classes
- `RecentFilesManager : IRecentFilesManager`
- `FileHandlerBase` (abstract)

### Test-Derived Contracts
- `RecentFilesManagerTests` validates `IRecentFilesManager` contract
```


***

### Phase 3: Settings Revival \& Project-Level Config

**Goal:** Restore Settings tab UI, add per-project exclusion config.[^2_1]


| \# | Component | Type | Why |
| :-- | :-- | :-- | :-- |
| 9 | **Settings Tab UI Wiring** | Fix Existing | `MainForm.SettingsTab.cs` exists but hidden; wire `cmbSettingsVectorStore` change events[^2_1] |
| 10 | **ProjectSubsetConfig** | New Class | Per-project exclusions (e.g., `exclude: ["*.Designer.cs", "bin/", "obj/"]`) |
| 11 | **VectorStoreConfig Extension** | Modify Existing | Add `ProjectSubsetConfig? ProjectSubsetSettings` property[^2_1] |

**Configuration Flow:**

```
app.config (global)
  └─ vectorStoreConfig.json (per-store, already exists)
       └─ ProjectSubsetConfig (new, nested)
```


***

## UI Layout (Sketch for AI)

### New Controls on MainForm

**Tab: "Project Subset" (new tab next to Recent Files)**

```
┌─────────────────────────────────────────────────┐
│ Target File Path:                               │
│ [______________________________________] [Browse]│
│                                                  │
│ [Export Project Subset]                         │
│                                                  │
│ ☐ Include Test Projects                         │
│ ☐ Include Contract Summary (Phase 2)           │
└─────────────────────────────────────────────────┘
```

**Workflow:**

1. User pastes file path into `txtTargetFilePath` (or clicks Browse)
2. Clicks `btnExportProjectSubset`
```
3. Handler detects parent `.csproj`, collects files, exports to `Generated/yyyy-MM-dd/<vsName>-<branch>-project-subset.md`[^2_1]
```

4. Registers with `RecentFilesManager` → appears in Recent Files panel with `"Project: VecTool.Core"`[^2_1]

***

## Questions for AI to Answer

### Critical Path

1. **File Selection UX**: Should Browse button use `OpenFileDialog` (single file) or `FolderBrowserDialog` (project root)?
    - **Preferred:** OpenFileDialog → user picks any `.cs` file, handler traverses up
    - **Alternative:** TextBox only → AI asks for path explicitly
2. **Contract Extraction Depth**: Should Phase 2 parse **only interfaces**, or also extract `public` method signatures from classes?
    - **Preferred:** Both (interfaces + public class summaries)
    - **Output:** Top of MD file with `## Contracts` section
3. **Test Detection Logic**: How to identify "test-derived contracts"?
    - **Option A:** Files in `*Tests.csproj` projects that reference interfaces
    - **Option B:** Files with `[TestFixture]` or `[Test]` attributes
    - **Preferred:** Option A (project-based)

### Integration Points

4. **Settings Tab Revival**: Should per-project config be **inline in Settings tab** (new section), or **separate "Project Config" dialog**?
    - **Preferred:** Inline section: `grpProjectSubsetSettings` under existing `grpExclusionSettings`[^2_1]
5. **Output Path Pattern**: Follow existing `DatedFileWriter` pattern?

```
- **Yes:** `Generated/2025-10-27/<vsName>-<branch>-project-subset.md`[^2_1]
```

6. **Vector Store Linking**: Should project subset **auto-link to current vector store** on export?
    - **Yes:** Use `lastSelection.GetLastSelectedVectorStore()` to tag file[^2_1]

***

## Success Criteria

### Phase 1 Complete When:

- [ ] User enters file path → handler finds `.csproj`
- [ ] Exports **only project files** (no README from repo root)
- [ ] Registers in Recent Files as `RecentFileType.ProjectSubsetMd`
- [ ] Progress bar updates via `IUserInterface.UpdateProgress()`[^2_1]


### Phase 2 Complete When:

- [ ] Exported MD starts with **Contracts section** (interfaces listed)
- [ ] Includes **README.md from project folder** if present
- [ ] Test files marked with `## Test-Derived Contracts` subsection


### Phase 3 Complete When:

- [ ] Settings tab shows per-project exclusion UI
- [ ] Config saves/loads from `VectorStoreConfig.ProjectSubsetSettings`
- [ ] Manual exclusion patterns work (e.g., `*.Designer.cs`)
