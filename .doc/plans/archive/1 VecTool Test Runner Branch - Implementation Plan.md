**PlanID:** 1
**Objective:** Complete the `test_results` feature branch with automatic test execution, intelligent error reporting, and seamless Recent Files integration[^1]

***

## Phase 1: Test Execution Infrastructure 🔧

### Step 1.1: Refactor TestRunnerHandler for Dependency Injection

**Goal:** Make the handler testable and SOLID-compliant[^1]

**Implementation:**

- Create `IGitRunner` interface in `Core` project with `GetCurrentBranchAsync(string workingDir)` method
- Create `IProcessRunner` interface to wrap `Process.Start` for mocking
- Inject both interfaces into `TestRunnerHandler` constructor
- Update `RunDotnetTestAsync` to use `IProcessRunner` instead of direct `Process` instantiation

**Success criteria:**

- All existing tests pass
- Constructor accepts `IGitRunner` and `IProcessRunner` parameters
- No direct `new Process()` or `new GitRunner()` calls in handler

**Dependencies:** None

***

### Step 1.2: Implement Custom Exception Types

**Goal:** Replace generic exceptions with domain-specific types for better error handling[^1]

**Implementation:**

- Create `TestExecutionException : Exception` in `Handlers` project with properties:
    - `int ExitCode`
    - `string StandardOutput`
    - `string StandardError`
- Create `GitBranchDetectionException : Exception` for branch resolution failures
- Update `TestRunnerHandler.RunDotnetTestAsync` to throw `TestExecutionException` on non-zero exit codes
- Update `GitRunner.GetCurrentBranchAsync` to throw `GitBranchDetectionException` on failures

**Success criteria:**

- Custom exceptions preserve full stack traces
- Exception messages include actionable guidance (e.g., "Check git installation")
- All catch blocks log with appropriate NLog levels per GUIDE 251002

**Dependencies:** 1.1

***

### Step 1.3: Standardize Error Messages

**Goal:** Ensure consistent, user-friendly error reporting[^1]

**Implementation:**

- Define error message templates in `Constants` project:
    - `TestStrings.NoSolutionFileFound`
    - `TestStrings.DotnetTestFailed`
    - `TestStrings.GitBranchUnknown`
    - `TestStrings.FileWritePermissionDenied`
- Update `TestRunnerHandler` to use constant error messages
- Implement "NO FILE" fallback when branch is unknown and test writes nothing
- Add actionable guidance to all MessageBox error prompts

**Success criteria:**

- No hardcoded error strings in handler code
- MessageBox captions and icons match specification
- Error messages include next steps for user

**Dependencies:** 1.2

***

## Phase 2: Robust Output File Management 📄

### Step 2.1: Implement Safe File Writer with Error Recovery

**Goal:** Handle all file I/O failure scenarios gracefully[^1]

**Implementation:**

- Extend `DatedFileWriter` with:
    - `TryWriteFile(string content, out string? errorMessage)` method
    - Retry logic for transient failures (3 attempts with exponential backoff)
    - Fallback to temp directory if configured output path is inaccessible
- Add validation for output file path format: `TestResults-{store}.{branch}.txt`
- Log all file operations with full exception context

**Success criteria:**

- File write failures return detailed error messages without throwing
- Unauthorized access scenarios are handled with fallback path
- All file operations logged at appropriate NLog levels

**Dependencies:** None

***

### Step 2.2: Enhance Output File Content Structure

**Goal:** Ensure output files contain comprehensive diagnostic information[^1]

**Implementation:**

- Define output file structure in `Constants.TestStrings`:
    - Header: Timestamp, branch, solution path
    - Section 1: Test execution summary (passed/failed/total)
    - Section 2: Full stdout
    - Section 3: Full stderr
    - Section 4: Exit code and duration
- Update `TestRunnerHandler.RunTestsAsync` to format output using template
- Include git commit hash in header when available

**Success criteria:**

- Output files are parseable by external tools (structured format)
- All relevant diagnostic info present (branch, timing, exit code)
- Failed tests include full stack traces

**Dependencies:** 2.1

***

## Phase 3: Recent Files Integration 🗂️

### Step 3.1: Automatic Registration After Test Run

**Goal:** Seamlessly add test results to Recent Files grid[^1]

**Implementation:**

- Update `TestRunnerHandler.RunTestsAsync` to call `recentFilesManager.RegisterGeneratedFile` on success
- Pass `RecentFileType.TestResults` enum value
- Link test result file to current vector store if one is selected
- Handle registration failures gracefully (log but don't block test run)

**Success criteria:**

- Recent Files grid updates immediately after test run completes
- Test result files appear with correct icon and metadata
- Double-click on Recent Files entry opens file in default editor

**Dependencies:** 2.2

***

### Step 3.2: Recent Files Filtering and Persistence

**Goal:** Ensure TestResults file type integrates with existing filter system[^1]

**Implementation:**

- Verify `RecentFileType.TestResults` serialization in `RecentFilesJson`
- Add UI filter button for TestResults in `RecentFilesPanel`
- Ensure `UiStateConfig` persists TestResults filter state
- Test Recent Files drag-drop with TestResults files

**Success criteria:**

- Filter dropdown includes "Test Results" option
- Filter state persists across application restarts
- Drag-drop of TestResults files to external apps works correctly

**Dependencies:** 3.1

***

## Phase 4: Comprehensive Test Coverage 🧪

### Step 4.1: Unit Tests for Edge Cases

**Goal:** Cover all failure scenarios identified in Gap Analysis[^1]

**Implementation:**

- Add tests to `TestRunnerHandlerTests.cs`:
    - `RunTestsAsync_WithNonZeroExitCode_ThrowsTestExecutionException`
    - `RunTestsAsync_WithEmptyStdout_WritesStderrOnly`
    - `RunTestsAsync_WhenFileWriteFails_ReturnsErrorMessage`
    - `GetCurrentBranchAsync_WhenGitUnavailable_ThrowsGitBranchDetectionException`
- Mock `IProcessRunner` to simulate success/failure outputs
- Mock `IGitRunner` to simulate branch detection failures

**Success criteria:**

- All edge cases from Gap Analysis covered
- Tests use mocks instead of real processes
- Code coverage for `TestRunnerHandler` exceeds 90%

**Dependencies:** 1.1, 1.2

***

### Step 4.2: Integration Tests with Real dotnet CLI

**Goal:** Validate end-to-end workflow with actual test execution[^1]

**Implementation:**

- Create `TestRunnerIntegrationTests.cs` in `UnitTests` project
- Add test that:
    - Creates minimal test project in temp directory
    - Executes `dotnet test` via `TestRunnerHandler`
    - Verifies output file generation
    - Confirms Recent Files registration
- Mark tests with `[Category("Integration")]` for CI filtering

**Success criteria:**

- Integration tests pass on clean machine with .NET SDK installed
- Tests clean up temp directories after execution
- Tests run in under 30 seconds

**Dependencies:** 3.1

***

### Step 4.3: UI Automation with WinForms Testing

**Goal:** Verify Ctrl+T shortcut and menu interaction[^1]

**Implementation:**

- Add `MainFormTestRunnerTests.cs` with tests:
    - `CtrlT_ShortcutInvokesTestRunner`
    - `FileMenuRunTests_InvokesTestRunner`
    - `TestRunCompletion_UpdatesRecentFilesGrid`
- Use `SendKeys` to simulate keyboard shortcuts
- Use `Application.DoEvents()` to process UI messages

**Success criteria:**

- UI tests run without human interaction
- Tests verify MessageBox appears on completion
- Tests confirm Recent Files grid row count increases

**Dependencies:** 4.2

***

## Phase 5: Documentation \& Cleanup 📝

### Step 5.1: Update Developer Documentation

**Goal:** Document new interfaces, exceptions, and workflow[^1]

**Implementation:**

- Add XML comments to all public interfaces and exceptions
- Update `README.md` with Test Runner feature details
- Document error message constants in `Constants/README.md`
- Add architecture diagram showing TestRunner dependencies

**Success criteria:**

- All public APIs have complete XML documentation
- README includes screenshot of test results in Recent Files
- Architecture docs explain DI pattern usage

**Dependencies:** None (parallel with other phases)

***

### Step 5.2: Code Review Checklist \& PR Preparation

**Goal:** Ensure code meets quality standards before merge[^1]

**Implementation:**

- Run architecture tests to verify Constants usage
- Validate all logging statements follow GUIDE 251002 conventions
- Confirm no magic strings remain in TestRunnerHandler
- Run full test suite (unit + integration + UI)
- Update `Changelog.md` with v1.25.10xx changes

**Success criteria:**

- All tests pass (0 failures)
- No ReSharper or SonarLint warnings
- Changelog entry includes breaking changes and new features

**Dependencies:** 4.3

***
