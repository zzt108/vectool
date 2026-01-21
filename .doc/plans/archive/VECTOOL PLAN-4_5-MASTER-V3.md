# ⚙️ VECTOOL 4.5 - Exclusion System Overhaul (MASTER PLAN v3.0)

**Last Updated:** 2025-10-31 21:10 CET  
**Status:** 🎯 Phase 4.5.1 COMPLETE → Phase 4.5.2 LOCKED + Phase 4.5.X DESIGNED  
**For use in:** Multiple threads / implementation cycles

---

## 📌 Executive Summary

VECTOOL 4.5 modernizes the exclusion system with three integrated capabilities:

1. **Patterns-based filtering** (Phase 4.5.2): Handler-independent, FileSystemTraverser-exclusive
2. **File-level markers** (Phase 4.5.X): Per-file exclusion + space documentation references
3. **Automatic detail levels** (Phase 4.5.X): Token-based intelligent splitting (L1: 300k + L2: remainder)

**Key Metrics:**
- Foundation: 100% complete (Phase 4.5.1)
- Architecture: Locked (handler independence + auto-levels)
- Remaining effort: ~14 hours (phases 4.5.2, 4.5.X, 4.5.3-6)
- Expected codebase.md: Two files (L1: ~300k, L2: remainder) + 50%+ size reduction

---

## 🏗️ Architecture Principles

### 1. Handler Independence (LOCKED)
All handlers must be independent of exclusion logic. Only FileSystemTraverser decides.

### 2. Exclusive Exclusion Authority
FileSystemTraverser is the ONLY place exclusion decisions are made.

### 3. Multi-Layer Exclusion Strategy
**Layer 1 (Patterns):** .gitignore/.vtignore patterns (fast, folder-level)
**Layer 2 (File Markers):** [VECTOOL:EXCLUDE] comments (granular, file-level)
**Layer 3 (Auto-Split):** Token-based L1/L2 split (intelligent, size-based)

### 4. Multi-Language Support
Language-agnostic marker format: `[VECTOOL:EXCLUDE:...]` or `<<VECTOOL:EXCLUDE:...>>`

---

## 📊 Data Flow (UPDATED)

```
┌──────────────────────────────────────────────────────────┐
│  Various Handlers (MDHandler, etc.)                       │
│  • Pass folders to traverser (exclusion-unaware)         │
└────────────────┬─────────────────────────────────────────┘
                 │
                 ▼
┌──────────────────────────────────────────────────────────┐
│  FileSystemTraverser (EXCLUSIVE AUTHORITY)               │
│  • Checks .gitignore patterns (Layer 1)                  │
│  • Scans file headers for [VECTOOL:EXCLUDE] (Layer 2)   │
│  • Counts tokens and auto-splits (Layer 3)               │
│  • Logs all decisions via LogCtx                         │
└────────────────┬─────────────────────────────────────────┘
                 │
          ┌──────┴──────┬──────────────┐
          │             │              │
          ▼             ▼              ▼
      Excluded    codebase_L1.md   codebase_L2.md
      (logged)    (≤300k tokens)   (remainder)
```

---

## 📋 Phase Breakdown

### Phase 4.5.1 - Exclusion Facade & Library Adapters ✅ COMPLETE

**Status:** DELIVERED AND VALIDATED

**Deliverables:**
- ✅ `IIgnorePatternMatcher` interface (facade)
- ✅ `MabDotIgnoreAdapter` implementation (ACTIVE - Primary)
- ✅ `GitignoreParserNetAdapter` implementation (DEPRECATED - tests removed)
- ✅ `IgnoreMatcherFactory` for library switching
- ✅ `IgnoreLibraryType` enum (MabDotIgnore is default)
- ✅ NuGet: MAB.DotIgnore v3.0.2 integrated
- ✅ LogCtx integrated throughout

No changes. Foundation is solid. ✅

---

### Phase 4.5.2 - FileSystemTraverser as Exclusive Authority ⏳ NEXT

**Objective:** Make FileSystemTraverser the ONLY place exclusion decisions are made.

**Architecture Principle:** Handler Independence

**Deliverables:**

**A. FileSystemTraverser Constructor (NEW)**
```csharp
public FileSystemTraverser(
    string? rootPath = null)  // Repo root for pattern detection
{
    _rootPath = rootPath ?? Environment.CurrentDirectory;
    _matcher = null; // Lazy-initialized
}
```

**B. FileSystemTraverser.ProcessFolder() (REVISED)**
- Lazy-initializes matcher on first call
- Checks patterns FIRST (before legacy config)
- Falls back to legacy if patterns unavailable
- Logs all decisions via LogCtx

**C. Handlers Become Exclusion-Unaware (Refactoring)**
- MDHandler has NO exclusion logic
- MDHandler has NO calls to FileValidator exclusion methods
- All handlers pass folders to FileSystemTraverser
- Handlers trust traverser's pre-filtered results

**Success Criteria:**
- ✅ FileSystemTraverser creates and owns matcher
- ✅ Patterns checked FIRST (before legacy config)
- ✅ All exclusion decisions logged via LogCtx
- ✅ All handlers are exclusion-unaware
- ✅ Legacy fallback works if patterns unavailable
- ✅ Performance: < 200ms for 1000 files
- ✅ No scattered exclusion checks in codebase

**Estimated Effort:** 2 hours

**Branch:** `feature/4.5.2-traverser-exclusive-authority`

---

### Phase 4.5.X - File Markers + Automatic Detail Levels ⏳ PARALLEL/AFTER 4.5.2

**NEW PHASE: Intelligent multi-layer exclusion + token-based splitting**

**Objective:** Add file-level exclusion markers and automatic L1/L2 splitting.

**Key Innovation:** Token-based auto-split (no user decisions required)

**Deliverables:**

**A. File Exclusion Marker (Language-Agnostic)**
```
[VECTOOL:EXCLUDE:reason_text:@space_reference]
or
<<VECTOOL:EXCLUDE:reason_text:@space_reference>>

Components:
- reason_text: Free text (XSD_GENERATED, VENDOR, TEMP, etc.)
- @space_reference: Optional, refers to space documentation
```

**Example Usage (Multi-Language):**

```csharp
// C#
// [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs]
public class GeneratedClass { }
```

```python
# Python
# [VECTOOL:EXCLUDE:vendor_library:@Third-Party-Guide]
def external_api():
    pass
```

```xml
<!-- XML -->
<!-- [VECTOOL:EXCLUDE:xsd_schema:@XML-Reference] -->
<xs:schema>
```

```javascript
// JavaScript
// [VECTOOL:EXCLUDE:generated_by_tool:@Code-Generator-Docs]
const autoGenerated = {};
```

**B. Token-Based Auto-Split**
- Count tokens in all included files
- Split at configurable limit (default 300k)
- Creates: `codebase_L1.md` (~300k) + `codebase_L2.md` (remainder)
- If project fits in L1: Only L1 created, L2 skipped

**C. Configurable Token Limit (Per Project)**
```
In .vtignore or config file:
[VECTOOL]
token_limit_L1 = 300000  # Configurable: 200k, 300k, 400k, etc.
```

**D. File Scanning Order (Deterministic)**
- Alphabetical depth-first
- Same result every run
- Reproducible for CI/CD

**E. Multi-Language Support**
- Scans all files except JSON
- Generic regex: `[VECTOOL:EXCLUDE:...]` or `<<VECTOOL:EXCLUDE:...>>`
- Works in any comment syntax

**F. Structured Logging (LogCtx)**
```csharp
For each excluded file:
ctx.Add("file_path", "path/to/file.cs");
ctx.Add("exclusion_reason", "reason_text");
ctx.Add("space_reference", "@doc_name");
ctx.Add("source", "file_marker");  // vs "pattern"
_log.Info("File excluded from codebase export", ctx);
```

**Implementation Steps:**

1. Implement file marker parser (generic regex for both syntaxes)
2. Add token counter (generic library)
3. Implement auto-split logic
4. Integrate LogCtx for all decisions
5. Update FileSystemTraverser to use markers
6. Add configuration for token limit
7. Unit tests (language-agnostic)
8. Documentation with examples

**Success Criteria:**

- ✅ Files with [VECTOOL:EXCLUDE] are excluded
- ✅ Space references (@name) extracted and logged
- ✅ Free-text reasons supported (no enum)
- ✅ Automatic split at token limit
- ✅ Both L1/L2 files generated (L2 empty if under limit)
- ✅ Works across all file types (except JSON)
- ✅ Language-agnostic (no C#-specific logic)
- ✅ Deterministic file ordering
- ✅ All decisions logged (reason + reference)
- ✅ Configurable token limit per project
- ✅ AI-friendly @ notation for space references

**Estimated Effort:** 4-6 hours

**Branch:** `feature/4.5.X-file-markers-auto-levels`

**Phase Status:** 📅 DESIGNED & READY - Can run parallel with 4.5.2 or after

---

### Phase 4.5.3 - MainForm Exclusions Tab UI 📅 PLANNED

**Objective:** Add new tab to MainForm showing exclusion status.

**Estimated Effort:** 2.5 hours

---

### Phase 4.5.4 - .vtignore Migration & Auto-Creation 📅 PLANNED

**Objective:** Auto-create `.vtignore` from `app.config` on first run.

**Estimated Effort:** 1 hour

---

### Phase 4.5.5 - Testing & Validation 📅 PLANNED

**Objective:** Comprehensive testing for all phases.

**Estimated Effort:** 3 hours

---

### Phase 4.5.6 - Documentation & Cleanup 📅 PLANNED

**Objective:** Update documentation for new architecture.

**Estimated Effort:** 1.5 hours

---

## 🌿 Git Branches (COMPLETE REFERENCE)

| Phase | Branch Name | Purpose | Status |
|:------|:------------|:--------|:-------|
| 4.5.1 | (merged) | Facade + Adapters | ✅ Complete |
| **4.5.2** | **`feature/4.5.2-traverser-exclusive-authority`** | **Handler independence** | **⏳ Next** |
| **4.5.X** | **`feature/4.5.X-file-markers-auto-levels`** | **File markers + auto split** | **⏳ Parallel** |
| 4.5.3 | `feature/4.5.3-mainform-exclusions-tab` | UI visualization | 📅 Later |
| 4.5.4 | `feature/4.5.4-vtignore-migration` | Auto-creation | 📅 Later |
| 4.5.5 | `feature/4.5.5-testing-validation` | Comprehensive tests | 📅 Later |
| 4.5.6 | `feature/4.5.6-documentation-cleanup` | Docs | 📅 Later |
| Final | `release/4.5` | Integration before v4.5.0 | 📅 Final |

---

## 📊 Implementation Status

| Phase | Component | Status | Hours |
|:------|:----------|:-------|:-----:|
| 4.5.1 | Facade + Adapters | ✅ COMPLETE | - |
| **4.5.2** | **Traverser Authority** | **⏳ NEXT** | **2** |
| **4.5.X** | **File Markers + Auto-Levels** | **⏳ READY** | **4-6** |
| 4.5.3 | MainForm UI Tab | 📅 PLANNED | 2.5 |
| 4.5.4 | .vtignore Migration | 📅 PLANNED | 1 |
| 4.5.5 | Testing & Validation | 📅 PLANNED | 3 |
| 4.5.6 | Documentation | 📅 PLANNED | 1.5 |
| **TOTAL** | | | **14-16 hours** |

---

## 🎯 Key Design Decisions (FINALIZED)

| Decision | Specification |
|:---------|:--------------|
| **File marker syntax** | `[VECTOOL:EXCLUDE:reason:@ref]` (language-agnostic) |
| **Split strategy** | Token-based auto-split (no user decisions) |
| **L1 size target** | ~300k tokens (configurable per project) |
| **File marker position** | After language-required headers (XML decl, etc.) |
| **Reason format** | Free-text (no enum) |
| **Space reference** | @notation (semantic, AI-friendly) |
| **Scanning order** | Alphabetical depth-first (deterministic) |
| **Token counter** | Generic library (fastest to implement) |
| **.vtignore** | 100% .gitignore compatible (no extensions) |
| **Multi-language** | All languages except JSON |
| **Empty L2** | Skip creation if project under limit |
| **Logging** | Structured LogCtx (reason + @reference) |

---

## 💾 .vtignore Compatibility

.vtignore remains **100% .gitignore compatible**:
- Standard pattern syntax only
- No metadata extensions
- File markers stay in code comments
- Configuration (token limit) in separate section if needed

---

## 🚀 Implementation Roadmap

**Week 1:**
- Phase 4.5.2: FileSystemTraverser authority + handler independence (2h)

**Week 1-2 (Parallel):**
- Phase 4.5.X: File markers + auto-levels (4-6h)

**Week 2-3:**
- Phase 4.5.3: MainForm UI (2.5h)
- Phase 4.5.4: .vtignore migration (1h)
- Phase 4.5.5: Testing (3h)
- Phase 4.5.6: Documentation (1.5h)

**Total:** ~14-16 hours to complete VECTOOL 4.5

---

## 📖 How to Use This Plan in Other Threads

**For Phase 4.5.2 Implementation:**
- Reference: Phase 4.5.2 section
- Key: Handler Independence Requirements
- Branch: `feature/4.5.2-traverser-exclusive-authority`

**For Phase 4.5.X Implementation:**
- Reference: Phase 4.5.X section
- Key: File marker specification + token-based split logic
- Examples: Multi-language usage samples
- Branch: `feature/4.5.X-file-markers-auto-levels`

**For Phase 4.5.3+ Implementation:**
- Reference: Corresponding phase section
- Key: Architecture principles (handler independence + auto-levels)
- Use: As foundation for UI/testing/docs

**For Code Review:**
- Verify: Handler independence (no exclusion logic in handlers)
- Verify: File markers properly formatted
- Verify: LogCtx traces include reason + reference
- Verify: Deterministic file ordering

**For Release/Documentation:**
- Reference: Success metrics + git branches
- Key: Both L1 and L2 files generated
- Verify: Multi-language support working

---

## 🔑 Success Metrics (FINAL)

**Before (Current):**
- Exclusion logic scattered across multiple places
- Patterns never used
- Hard to track why files excluded
- No detail level support

**After (Target - Phase 4.5 Complete):**
- ✅ Single exclusion authority (FileSystemTraverser)
- ✅ Handlers completely independent
- ✅ Two codebase files: L1 (~300k) + L2 (remainder)
- ✅ Multi-language file markers with space references
- ✅ Deterministic, reproducible exports
- ✅ Full audit trail (reason + reference for each exclusion)
- ✅ 50%+ effective codebase.md reduction
- ✅ Zero breaking changes (legacy fallback works)

---

## 📝 Plan Version History

| Version | Date | Key Changes | Status |
|:--------|:-----|:------------|:-------|
| 1.0 | 2025-10-30 | Initial plan | Planning |
| 2.0 | 2025-10-31 | Phase 4.5.1 complete; tests removed | Updated |
| 2.1 | 2025-10-31 20:00 | Handler independence locked | Locked |
| **3.0** | **2025-10-31 21:10** | **Phase 4.5.X: File markers + auto-levels designed** | **CURRENT** |

---

**This master plan is now the source of truth for VECTOOL 4.5 implementation across all threads.**

Next action: Start Phase 4.5.2 implementation using this master plan as reference. ✅
