# Codebase Refactoring Plan (Zero Functional Change)

> **Main takeaway:** This plan is focused on making C\# codebase more readable, maintainable, and testable (the classic “it compiles, it runs, but damn it’s ugly!” dilemma) — WITHOUT changing any features or behaviors. All refactoring actions here are strictly non-functional.

***

## Headline Goals

- MUST adhere to Coding Convention document
- **Standardize organization** (SOLID, clear folder/file layout)
- **Improve naming clarity and intent**
- **Centralize configuration and constants**
- **Simplify class/namespace responsibility**
- **Document code structure and rules**
- **Prepare codebase for future functional changes**

***

## Refactor Plan

### 1. **Project and Directory Structure**

- **Audit current folders/files**: Map existing .cs files, project layouts, solution structure, and shared resources.[^1]
- **Define standard folders** for:
    - `Core` (business logic)
    - `UI` (WinForms)
    - `Handlers` (DocX, PDF, Markdown, etc.)
    - `Constants` (centralized XML tags and attrs)
    - `Tests` ([NUnit] specs with Shouldly assertions)
    - `Utils` (helpers, common logic)
- **Move files** to the right folders, keeping explicit namespace alignment (no auto-changes, include unchanged lines when code moves).

***

### 2. **Class, Method, and Namespace Responsibility**

- **Adopt SOLID**:
    - Each class should do one thing — if it’s a dumpster fire full of half-baked methods, split it.
- **Refactor ambiguous classes** to clarify responsibility (e.g., split file I/O vs. processing logic).[^1]
- **Align namespaces** with folder structure.
- **For partial WinForms classes**, check that UI and business logic are split.

***

### 3. **Consistent Naming and Comments**

- **Review all names**:
    - Class, method, parameter, and variable names follow *English, PascalCase/CamelCase* conventions.
    - No magic strings — use centralized constants (see below).
- **Standardize comments**:
    - File header comments (purpose, authorship)
    - XML doc comments for public classes and methods.
    - All comments in English!
- **Don’t remove** commented code, TODOs, or legacy notes, unless explicitly instructed (“Ez a csoda marad, amíg végleg ki nem dobjuk!”).

***

### 4. **Centralize Configuration, Constants, and Tags**

- **Move magic strings (XML tags, config keys)** to the `Constants` classes.[^1]
- **Update usages everywhere**:
    - E.g., replace `"file name"` with `Constants.Tags.FileName`.
    - Use constructors/helpers from `Constants.TagBuilder` for XML generation.
- **Configurable exclusions/settings** go in the right config files (app.config, json).
- **Add XML docs** for new constants (see README.md recommendations).

***

### 5. **Testing Structure Review**

- **Ensure all test files** use:
    - Methods in English, clear test names (“ConvertSelectedFoldersToDocxWorksAsExpected” type).
    - NUnit `[TestFixture]`, `[Test]`, and relevant setup/teardown patterns.
    - Assertions with Shouldly.
- **Organize tests** into folders aligned with main code (e.g., `CoreTests`, `HandlerTests`).[^1]
- **Retain all current test coverage!** No deletions, only move/update as necessary.

***

### 6. **Documentation and Readme Updates**

- **Document directory structure** in README.md.
- **Explain code organization principles** and contributions guidelines.
- **Show refactor rules** for future developers (“if you move something, you keep old lines in your change, reference back, és ne legyen szar!”).

***

### 7. **No Functional Change QA**

- **Run all current tests after each refactor step.**
- **Check file IO, UI, export behaviors, Git integrations remain unchanged.**
- **Document refactor steps and rationale in CHANGELOG.md.**
- **Peer review code moves and naming updates.**

***

## Examples of Refactor Actions

(Just to trigger your refactoring agyad, here are 3 common scenarios for this plan.)

**Example 1: Centralizing Constants**

- Move all hardcoded XML tag strings to `Constants/Tags.cs`.[^1]
- Update usages in handlers (DocXHandler, PdfHandler), replace "magic" with constants.

**Example 2: Splitting Monolithic Classes**

- If `FileHandlerBase` juggles everything, split file enumeration into `FolderScanner.cs`, processing logic into their respective handlers.

**Example 3: Folder Restructuring**

- Move code files:
from `Misc/DocXHandlerStuff.cs` → `Handlers/DocXHandler.cs`
from root test files → `UnitTests/Core/`, `UnitTests/Handlers/`

***

## Visual Example — Before Vs. After Folder Layout

| Before | After |
| :-- | :-- |
| src/DocXHandler.cs | Handlers/DocXHandler.cs[^1] |
| src/FileHandlerBase.cs | Handlers/FileHandlerBase.cs[^1] |
| src/Constants.cs | Constants/Tags.cs |
| src/UnitTests.cs | UnitTests/Handlers/DocXHandlerTests.cs |
| src/README.md | README.md (with directory breakdown) |


***

## Final Notes

- **No frills, no functional changes, no breaking the codebase.**
- If in doubt: don’t change the behavior, only clean, clarify, and organize.
- Ready for the szopás? Mert időnként a refactor az, főleg, ha nem te szartad össze!

***