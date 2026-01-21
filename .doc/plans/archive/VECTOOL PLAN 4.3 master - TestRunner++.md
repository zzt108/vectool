# 🎯 Two-Phase Approach: MVP + Future JSON

## **Phase 1: MVP - Basic Exit Code Feedback (TODAY)**

**Goal:** Simple, human-readable status message based on exit code only.
**Effort:** 1-2 hours
**Zero new files, zero parsers, zero JSON.**

### What you get:

```
✅ Tests passed (exit code 0)
❌ Tests failed (exit code 1 or 2)
⚠️ No tests found (exit code 8)
❌ Infrastructure error (exit code 10)
```


### Changes needed:

1. **TestRunnerHandler.cs** – add `MapExitCodeToMessage(int code)` method.[^2]
2. **MainForm.RunTests.cs** – show message box with outcome + exit code.[^1]
3. **LogCtx** – log Operation=RunTests, ExitCode, Message.[^4]

### Code (MVP):

```csharp
// ✅ NEW - Handlers/TestRunnerHandler.cs (partial)
public async Task<string> RunTestsAsync(string solutionPath, CancellationToken ct)
{
    using var log = new CtxLogger();
    log.Ctx.Set(new Props().Add("Operation", "RunTests").Add("Solution", solutionPath));
    
    var proc = await _processRunner.RunAsync(
        "dotnet", 
        $"test \"{solutionPath}\" --no-build --verbosity minimal", 
        null, 
        ct
    );
    
    var message = MapExitCodeToMessage(proc.ExitCode);
    log.Ctx.Set(new Props().Add("ExitCode", proc.ExitCode).Add("Message", message));
    
    if (proc.ExitCode != 0)
    {
        log.Warn($"Tests completed with exit code {proc.ExitCode}. {message}");
    }
    else
    {
        log.Info("Tests passed successfully.");
    }
    
    return message;
}

private static string MapExitCodeToMessage(int code)
{
    return code switch
    {
        0 => "✅ All tests passed.",
        1 => "❌ One or more tests failed (classic VSTest).",
        2 => "❌ One or more tests failed (MTP).",
        3 => "⚠️ Test run aborted.",
        8 => "⚠️ No tests discovered. Check your test project.",
        10 => "❌ Test adapter/infrastructure failure.",
        _ => $"⚠️ Unknown exit code {code}. See output for details."
    };
}
```

```csharp
// 🔄 MODIFY - OaiUI/MainForm.RunTests.cs
private async void OnRunTests(object sender, EventArgs e)
{
    using var log = new CtxLogger();
    log.Ctx.Set(new Props().Add("Operation", "OnRunTests"));
    
    try
    {
        var sln = FindSolutionFile(); // your existing logic
        var message = await _testRunner.RunTestsAsync(sln, CancellationToken.None);
        
        MessageBox.Show(
            message, 
            "Test Results", 
            MessageBoxButtons.OK, 
            message.StartsWith("✅") ? MessageBoxIcon.Information : MessageBoxIcon.Warning
        );
    }
    catch (Exception ex)
    {
        log.Error("Test run failed", ex);
        MessageBox.Show($"❌ Test execution failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
```


***

## **Phase 2: JSON Report for AI (LATER)**

**Goal:** Structured JSON with test details for AI consumption.
**Effort:** 4-6 hours
**Requires:** JunitXml.TestLogger NuGet package.[^3]

### What you get:

```json
{
  "ExitCode": 2,
  "Outcome": "TestFailures",
  "Summary": {
    "Total": 132,
    "Passed": 127,
    "Failed": 3,
    "Skipped": 2,
    "Duration": "00:00:43"
  },
  "Tests": [
    {
      "Name": "VecTool.Tests.HandlerTests.ShouldHandleNullInput",
      "Outcome": "Failed",
      "ErrorMessage": "Expected non-null, but was null",
      "StackTrace": "at VecTool.Handlers...",
      "Duration": "00:00:00.123"
    }
  ]
}
```


### Why JSON is enough (MD optional):

- **AI can parse JSON natively** – LLMs are trained on JSON structures.[^5]
- **MD adds human readability** – nice for you to review, but AI doesn't need pretty formatting.[^6]
- **Token efficiency** – JSON is more compact than MD for the same data.[^5]

**My recommendation:** Phase 1 MVP first (simple exit code feedback), then Phase 2 JSON when you need detailed test failure analysis for AI. Skip MD unless you personally want to read test reports in a nice format.[^3][^5]

***

## 📋 Revised Lean Plan

### **Phase 4.30.1 – Basic Exit Code Feedback (NOW)**

**Git Branch:** `feature/4.mvp.1-exit-code-feedback`
**Effort:** 1-2 hours

#### Tasks

1. Add `MapExitCodeToMessage(int code)` to TestRunnerHandler with 0/1/2/3/8/10 handling.[^2]
2. Implement `RunTestsAsync(string solutionPath, CancellationToken ct)`: run dotnet test, capture exit code, return message.[^1]
3. Wire `OnRunTests()` in MainForm to call handler and show MessageBox with emoji + message.[^1]
4. Add LogCtx instrumentation (Operation=RunTests, ExitCode, Message).[^4]
5. Add 2 NUnit tests: exitCode 0 → success message, exitCode 2 → failure message.[^7]

#### Deliverables

- Handlers/TestRunnerHandler.cs (new)[^8]
- OaiUI/MainForm.RunTests.cs (new partial class)[^9]
- UnitTests/TestRunnerHandlerTests.cs (new)[^10]


#### Acceptance Criteria

- User sees "✅ All tests passed" on exit 0, "❌ Tests failed" on exit 1/2.[^2]
- LogCtx Props include Operation, ExitCode, Message.[^4]
- Tests verify correct message for 0, 1, 2, 8, 10 exit codes.[^2]

***

### **Phase 4.30.1 – Structured JSON Report (FUTURE)**

**Git Branch:** `feature/4.json.1-structured-report`
**Effort:** 4-6 hours
**Blocked by:** MVP complete + JunitXml.TestLogger added to test projects.[^3]

#### Tasks

1. Add `JunitXml.TestLogger` NuGet to all test projects.[^3]
2. Update `BuildDotnetTestArgs()`: append `--logger:"junit;LogFilePath=TestResults.xml"`.[^3]
3. Implement `JUnitXmlParser.Parse(string xmlPath)` → TestResult[].[^3]
4. Implement `JsonReportGenerator.Generate(TestResult[] tests, string jsonPath)`.[^5]
5. Update `RunTestsAsync()`: parse XML, generate JSON, add to Recent Files.[^1]
6. Update UI: add "Open JSON Report" button.[^1]
7. Add NUnit tests for parser + generator.[^7]

#### Deliverables

- Parsers/JUnitXmlParser.cs[^11]
- Generators/JsonReportGenerator.cs[^12]
- Handlers/TestRunnerHandler.cs (updated)[^13]
- OaiUI/MainForm.RunTests.cs (updated)[^14]
- [8-9] UnitTests for parser/generator


#### Acceptance Criteria

- JSON file generated with Summary + Tests[] structure.[^5]
- JSON added to Recent Files as TestResults type.[^1]
- UI button opens JSON in default editor (VS Code/notepad).[^1]

***

## 🚀 Immediate Next Steps (MVP)

Reply **"approve mvp"** and I'll provide:

1. Full `TestRunnerHandler.cs` with `MapExitCodeToMessage()` and `RunTestsAsync()`.
2. Full `MainForm.RunTests.cs` with MessageBox integration.
3. Full `TestRunnerHandlerTests.cs` with exit code scenarios.

**Estimated time to working feature:** 1-2 hours (no parsers, no JSON, just exit code + message).[^2][^1]

<div align="center">⁂</div>

[^1]: VecToolDevMaster.master.md

[^2]: https://github.com/dotnet/docs/blob/main/docs/core/testing/microsoft-testing-platform-exit-codes.md

[^3]: https://stackoverflow.com/questions/71626444/how-to-log-dotnet-test-output-in-junit-format

[^4]: LogCtx-1.2.md

[^5]: https://www.tesults.com/blog/json-test-results-data-format-for-test-frameworks

[^6]: https://www.reddit.com/r/ChatGPTCoding/comments/1hg8m52/best_practices_for_converting_documentation_to/

[^7]: GUIDE-1.8-CodingConvention-WinForms-LogCtx.md

[^8]: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test

[^9]: https://teamcity-support.jetbrains.com/hc/en-us/community/posts/7906965281298--NET-command-dotnet-test-fails-with-exit-code-1-and-does-not-recognize-any-tests-after-update-from-NET-6-to-NET-7-rc1

[^10]: https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-exit-codes

[^11]: https://github.com/microsoft/vstest/issues/1624

[^12]: https://travis-ci.community/t/dotnet-test-exit-code-1/2238

[^13]: https://stackoverflow.com/questions/39468428/dotnet-test-exit-code-0-when-test-project-doesnt-compile

[^14]: https://stackoverflow.com/questions/68716134/vstest-console-exe-does-not-return-any-exit-code

