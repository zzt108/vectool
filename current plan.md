<img src="https://r2cdn.perplexity.ai/pplx-full-logo-primary-dark%402x.png" style="height:64px;margin-right:32px"/>

# Gap Analysis Report: Feature “Run Unit Tests”

**Main Takeaway:** The **“Run Unit Tests”** feature is largely implemented, but gaps remain in test coverage, error handling consistency, configuration flexibility, and adherence to coding conventions. Prioritize adding tests for edge cases, refining exception messages, and aligning code with established standards.

***

## 1. Feature Requirements vs. Current Status

| **Requirement** | **Current Status** | **Gaps Identified** |
| :-- | :-- | :-- |
| Detect Git branch name or fallback “unknown” | Implemented in `CoreGitRunner.GetCurrentBranchAsync` returning “unknown” on errors. | No unit tests verifying fallback behavior when `git` not available or returns blank. |
| Extend `RecentFileType` enum with `TestResults` | Added `TestResults` enum value. | Enum addition exists, but serialization/usage in persisting recent-files list not covered by tests. |
| Invoke `dotnet test` programmatically | `TestRunnerHandler.RunDotnetTestAsync` launches process and captures output. | No tests simulating successful vs. failing `dotnet test` cases—requires mocking of `Process`. |
| Write output to file `TestResults-{store}.{branch}.txt` | File naming and writing logic in `RunTestsAsync`. | Missing validation of output file path format; no tests for file‐I/O failures (e.g., write permission issues). |
| Register generated file in Recent Files grid | Conditional `recentFilesManager.RegisterGeneratedFile` call exists. | No integration/UI tests verifying the Recent Files grid updates after test run. |
| Show MessageBox on error or success | `ui.ShowMessage` used in catch and success branches. | Message texts and icons not validated by automated tests; inconsistent error messages vs plan (“NO FILE” fallback missing). |
| Add “Run Tests” menu item with **Ctrl+T** shortcut | Menu item and handler wired in Designer (`runTestsToolStripMenuItem`). | No UI automation tests ensuring the shortcut invokes the feature. |


***

## 2. Coding Standards \& Maintainability

**Gaps:**

- **Logging:** Use of `NLog` is present, but logging levels and message templates aren’t aligned with the team’s logging conventions (see GUIDE 251002).
- **Async Usage:** `RunDotnetTestAsync` reads both streams with `Task.WhenAll`, but swallow errors by throwing generic `InvalidOperationException`—should define a custom exception type.
- **Error Handling:** Catch blocks wrap all exceptions without preserving stack traces or inner exception context according to conventions.
- **Dependency Injection:** `TestRunnerHandler` directly instantiates `GitRunner`; better to inject via constructor interface for testability and SOLID compliance.

***

## 3. Test Coverage \& Quality

1. **Unit Tests Missing:**
    - `GetCurrentBranchAsync` fallback scenarios.
    - `RunDotnetTestAsync` behavior for non-zero exit codes and mixed stdout/stderr outputs.
    - File write failures (e.g., unauthorized access).
2. **Integration Tests Needed:**
    - UI tests (Selenium/Playwright) to confirm that clicking **Ctrl+T** or menu item runs tests and updates Recent Files grid.
    - End-to-end scenario with a sample `.sln`, validating the full workflow.

***

## 4. Recommendations \& Next Steps

1. **Expand Unit Tests**
    - Mock `Process` to simulate success/failure outputs.
    - Simulate empty or invalid `git` responses for branch detection.
    - Induce file write exceptions to verify error UI.
2. **Implement Integration/UI Automation**
    - Add Playwright tests to press **Ctrl+T** and assert Recent Files entry.
    - Validate MessageBox captions, icons, and button texts match spec.
3. **Refactor for Testability \& Standards**
    - Introduce interfaces (`IGitRunner`, `IProcessRunner`) and inject dependencies.
    - Create custom exception types (e.g., `TestExecutionException`).
    - Align logging messages with `GUIDE 251002` templates.
4. **Enhance Error Messages**
    - Standardize fallback message “NO FILE” when branch unknown and test writes nothing.
    - Include actionable guidance in MessageBoxes (e.g., “Check your git installation”).
5. **Documentation Updates**
    - Update plan.md to reflect refactored interfaces and additional test tasks.
    - Document the behavior and custom exceptions in XML comments and developer guide.

***

**Certainty:** 9/10
The analysis covers implementation artifacts and identifies concrete gaps based on attached documents.
<span style="display:none">[^1]</span>

<div align="center">⁂</div>

[^1]: current-plan.md

