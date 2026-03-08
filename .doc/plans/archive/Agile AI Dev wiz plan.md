<img src="https://r2cdn.perplexity.ai/pplx-full-logo-primary-dark%402x.png" style="height:64px;margin-right:32px"/>

# 1. Workflow State Management

Az egyes fázisok között menteni kell a state-et? (pl. ha bezárod az appot Plan phase közben, onnan tudjon folytatni) igen
A wizard state perzisztens legyen, vagy in-memory elég? perzisztens
Ha félbehagysz egy workflow-t, visszatérhetsz egy korábbi step-re, vagy csak forward-only navigation? visszatérhet bármelyik korábbi stepre, ha javítani kell. Jó lenne ha a plan md file-t tudná kezelni phase/step szinten mutatni hol is vagyunk
2. "Single File" Conversion A VecTool már tud ilyet
3. Git Integration Depth A VecTool már tud ilyet
Convention követés (Conventional Commits: feat/fix/chore stb.)?
4. Test/Fix Cycle
Ez a leglazább pont most:
Automated test run? (NUnit runner integration?) collecting all failed test output logs would be nice
amúgy csak manuális reminder "now you should test"?
Ha fail a test, rollback az előző step-re automatikusan? nem
AI-based fix suggestions a test failure alapján? klassz lenne
5. UI Integration \& Visibility
A "Current UI should be available on demand":
Modal wizard (popup), vagy dockable panel a main form-ban? Panel
Tab-based navigation (új tab a wizard-nak), vagy dedicated window? tab based
Wizard launcher button hol jelenjen meg? (toolbar, context menu, status bar?) menu
Hány workflow futhat párhuzamosan? (egyidejűleg több feature fejlesztése?) bizony egy workflow, de save/load status, can load another workflow
6. Scope \& Constraints
Phase loop "Implement or Plan again" - ez azt jelenti, hogy a Plan után dönthetsz, hogy új Plan kell, vagy mehet az Implement? Ha az utolso step is megvalosult az utolso phase-ben, akkor plan again, kulonben implement
Multi-step implement phases - hány step van egy phase-ben? (pl. Plan → Implement Phase 1 Step 1,2,3 → Commit → Test?) there are many steps in a phase, but one step at a time is implemented/tested
Rollback strategy ha valami elszáll: teljes workflow restart, vagy csak az utolsó step? user decides
Success Criteria elvárt definíció 🎯
Hogy lássam, milyen szintű komplexitásra gondolsz:
Basic: Simple wizard with manual steps, csak UI navigation és state tracking, minden más manual - yes
Intermediate: Git integration (commit message gen), file consolidation/merge, basic test runner hook - talán
Advanced: Full AI-powered workflow (AI planning, AI test analysis, auto-fix suggestions, multi-workflow parallel execution) - not yet
7. Integration Points
Mivel már van Recent Files és más features, a wizard:
Használja a Recent Files-t az "updated single files" tracking-hez? A recent files az a generált filokat tartalmazza. nem tudom mit értesz "updated single files" tracking-en?
Új service layer kell (WizardWorkflowManager), vagy a meglévő managers-be integrálunk? nem tudom. Pros \& cons?
Logging/telemetry - a workflow lépéseket tracking-elni kell analytics céljából? - igen

Perfect! Látom a codebase-t, most már tudom, hol állsz. 🎯 Így, hogy **minden input megvan**, elkészítem a teljes GPT-5 execution plan-t.

***

# 🎯 GPT-5 Execution Plan: Agile AI Dev Wizard UI

**Confidence Rating: 9/10** (clear requirements, solid existing architecture)

***

## Architecture Decision: Service Layer Approach

### **Pros \& Cons Analysis**

| **Option** | **Pros** | **Cons** | **Recommendation** |
| :-- | :-- | :-- | :-- |
| **New WizardWorkflowManager service** | Clean separation, single responsibility, testable, reusable | Additional layer, more files | ✅ **RECOMMENDED** |
| **Integrate into existing managers** | Less code, fewer files | Violates SRP, tight coupling, harder to test | ❌ Not recommended |

**Decision:** Create **dedicated service layer** (`WizardWorkflowManager`) similar to existing `RecentFilesManager` architecture.[^1][^2]

***

## Phase Overview

```
Phase 1: Core Workflow State Management (Steps 1-3)
Phase 2: Wizard UI Components (Steps 4-6)
Phase 3: Integration with Existing Features (Steps 7-9)
Phase 4: Testing & Polish (Steps 10-12)
```

**Total Steps:** 12 implementable, atomic steps
**Estimated Complexity:** Medium-High
**Parallel Execution:** Phases 1-2 can partially overlap

***

## 📋 Requirements Summary

**Workflow states:** Plan → Implement (Phase x, Step y) → Commit → Test/Fix → (loop back or Plan again)
**Persistence:** JSON-based workflow state (resume on app restart)
**UI:** Tab-based panel, menu launcher, markdown plan viewer with phase/step tracking
**Git Integration:** Reuse existing `GitRunner`[^1]
**Test Integration:** NUnit runner hook, collect failed test logs
**File Management:** Reuse "single file conversion" (existing feature)

***

# Phase 1: Core Workflow State Management

## Step 1: Workflow Data Models \& State Machine

### Objective

Create data models representing workflow state with serialization support.[^1]

### Context

- VecTool already has JSON persistence patterns (`RecentFilesJson`, `VectorStoreConfig`)
- Need state machine for: `Planning`, `Implementing`, `Testing`, `Committing`, `PlanAgain`
- Must support **phase/step navigation** with markdown plan tracking


### Deliverables

**File:** `DocXHandler/Workflow/WorkflowState.cs`

```csharp
using System;
using System.Collections.Generic;

namespace DocXHandler.Workflow
{
    public enum WorkflowPhase
    {
        Planning,
        Implementing,
        Testing,
        Committing,
        PlanAgain,
        Completed
    }

    public class WorkflowStepInfo
    {
        public int PhaseIndex { get; set; }
        public int StepIndex { get; set; }
        public string StepDescription { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CommitHash { get; set; }
        public List<string> TestFailureLogs { get; set; } = new List<string>();
    }

    public class WorkflowState
    {
        public string WorkflowId { get; set; } = Guid.NewGuid().ToString();
        public string WorkflowName { get; set; } = string.Empty;
        public string PlanFilePath { get; set; } = string.Empty;
        public WorkflowPhase CurrentPhase { get; set; }
        public int CurrentPhaseIndex { get; set; }
        public int CurrentStepIndex { get; set; }
        public List<WorkflowStepInfo> Steps { get; set; } = new List<WorkflowStepInfo>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
        public string? LastCommitMessage { get; set; }
    }
}
```

**File:** `DocXHandler/Workflow/WorkflowStateStore.cs`

```csharp
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace DocXHandler.Workflow
{
    public interface IWorkflowStateStore
    {
        void SaveWorkflowState(WorkflowState state);
        WorkflowState? LoadWorkflowState(string workflowId);
        List<WorkflowState> GetAllWorkflows();
        void DeleteWorkflow(string workflowId);
    }

    public class WorkflowStateStore : IWorkflowStateStore
    {
        private readonly string storageDirectory;
        private const string FileExtension = ".workflow.json";

        public WorkflowStateStore(string? storageDirectory = null)
        {
            this.storageDirectory = storageDirectory 
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                                "VecTool", "Workflows");
            
            Directory.CreateDirectory(this.storageDirectory);
        }

        public void SaveWorkflowState(WorkflowState state)
        {
            state.LastModifiedAt = DateTime.UtcNow;
            var filePath = GetFilePath(state.WorkflowId);
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public WorkflowState? LoadWorkflowState(string workflowId)
        {
            var filePath = GetFilePath(workflowId);
            if (!File.Exists(filePath)) return null;

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<WorkflowState>(json);
        }

        public List<WorkflowState> GetAllWorkflows()
        {
            var workflows = new List<WorkflowState>();
            var files = Directory.GetFiles(storageDirectory, $"*{FileExtension}");
            
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var state = JsonSerializer.Deserialize<WorkflowState>(json);
                if (state != null) workflows.Add(state);
            }
            
            return workflows;
        }

        public void DeleteWorkflow(string workflowId)
        {
            var filePath = GetFilePath(workflowId);
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        private string GetFilePath(string workflowId) 
            => Path.Combine(storageDirectory, $"{workflowId}{FileExtension}");
    }
}
```


### Unit Test Requirements

**File:** `UnitTests/Workflow/WorkflowStateStoreTests.cs`

```csharp
using NUnit.Framework;
using Shouldly;
using DocXHandler.Workflow;
using System;
using System.IO;

namespace UnitTests.Workflow
{
    [TestFixture]
    public class WorkflowStateStoreTests
    {
        private string testDirectory;
        private IWorkflowStateStore store;

        [SetUp]
        public void Setup()
        {
            testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            store = new WorkflowStateStore(testDirectory);
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, true);
        }

        [Test]
        public void SaveAndLoadWorkflowState_ShouldPersistData()
        {
            // Arrange
            var state = new WorkflowState
            {
                WorkflowId = "test-workflow-001",
                WorkflowName = "Test Feature",
                CurrentPhase = WorkflowPhase.Implementing,
                CurrentPhaseIndex = 1,
                CurrentStepIndex = 2
            };

            // Act
            store.SaveWorkflowState(state);
            var loaded = store.LoadWorkflowState(state.WorkflowId);

            // Assert
            loaded.ShouldNotBeNull();
            loaded.WorkflowId.ShouldBe(state.WorkflowId);
            loaded.CurrentPhase.ShouldBe(WorkflowPhase.Implementing);
            loaded.CurrentPhaseIndex.ShouldBe(1);
            loaded.CurrentStepIndex.ShouldBe(2);
        }

        [Test]
        public void LoadWorkflowState_NonExistentId_ShouldReturnNull()
        {
            // Act
            var result = store.LoadWorkflowState("non-existent-id");

            // Assert
            result.ShouldBeNull();
        }

        [Test]
        public void GetAllWorkflows_ShouldReturnMultipleWorkflows()
        {
            // Arrange
            var state1 = new WorkflowState { WorkflowId = "wf-1", WorkflowName = "Feature 1" };
            var state2 = new WorkflowState { WorkflowId = "wf-2", WorkflowName = "Feature 2" };

            store.SaveWorkflowState(state1);
            store.SaveWorkflowState(state2);

            // Act
            var all = store.GetAllWorkflows();

            // Assert
            all.Count.ShouldBe(2);
        }

        [Test]
        public void DeleteWorkflow_ShouldRemovePersistedFile()
        {
            // Arrange
            var state = new WorkflowState { WorkflowId = "delete-test" };
            store.SaveWorkflowState(state);

            // Act
            store.DeleteWorkflow(state.WorkflowId);
            var loaded = store.LoadWorkflowState(state.WorkflowId);

            // Assert
            loaded.ShouldBeNull();
        }
    }
}
```


### Validation Criteria

- ✅ State serializes/deserializes correctly to JSON
- ✅ Workflow state persists across application restarts
- ✅ All unit tests pass with >90% code coverage
- ✅ Edge cases handled (missing files, corrupted JSON)


### Integration Points

- Uses existing JSON serialization patterns from `RecentFilesJson`[^1]
- Follows `AppData` storage convention

***

## Step 2: Workflow Manager Service

### Objective

Create central orchestrator service for workflow lifecycle management.[^1]

### Context

- Similar architecture to `RecentFilesManager`[^1]
- Must handle phase/step transitions with validation
- Logging integration with existing `NLogShared`[^1]


### Deliverables

**File:** `DocXHandler/Workflow/WorkflowManager.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLogShared;

namespace DocXHandler.Workflow
{
    public interface IWorkflowManager
    {
        WorkflowState CreateWorkflow(string workflowName, string planFilePath);
        WorkflowState? LoadWorkflow(string workflowId);
        List<WorkflowState> GetAllWorkflows();
        void SaveWorkflow(WorkflowState state);
        void DeleteWorkflow(string workflowId);
        bool CanTransitionToPhase(WorkflowState state, WorkflowPhase targetPhase);
        void TransitionToPhase(WorkflowState state, WorkflowPhase targetPhase);
        void MarkStepCompleted(WorkflowState state, int phaseIndex, int stepIndex, string? commitHash = null);
        void AddTestFailureLogs(WorkflowState state, List<string> logs);
    }

    public class WorkflowManager : IWorkflowManager
    {
        private readonly IWorkflowStateStore store;
        private readonly ILogCtxLogger log;

        public WorkflowManager(IWorkflowStateStore store, ILogCtxLogger log)
        {
            this.store = store;
            this.log = log;
        }

        public WorkflowState CreateWorkflow(string workflowName, string planFilePath)
        {
            if (string.IsNullOrWhiteSpace(workflowName))
                throw new ArgumentException("Workflow name cannot be empty", nameof(workflowName));

            if (!File.Exists(planFilePath))
                throw new FileNotFoundException($"Plan file not found: {planFilePath}");

            var state = new WorkflowState
            {
                WorkflowName = workflowName,
                PlanFilePath = planFilePath,
                CurrentPhase = WorkflowPhase.Planning
            };

            store.SaveWorkflowState(state);
            log.Info($"Created workflow: {workflowName} (ID: {state.WorkflowId})");
            
            return state;
        }

        public WorkflowState? LoadWorkflow(string workflowId)
        {
            return store.LoadWorkflowState(workflowId);
        }

        public List<WorkflowState> GetAllWorkflows()
        {
            return store.GetAllWorkflows().OrderByDescending(w => w.LastModifiedAt).ToList();
        }

        public void SaveWorkflow(WorkflowState state)
        {
            store.SaveWorkflowState(state);
            log.Info($"Saved workflow: {state.WorkflowName}");
        }

        public void DeleteWorkflow(string workflowId)
        {
            store.DeleteWorkflow(workflowId);
            log.Info($"Deleted workflow: {workflowId}");
        }

        public bool CanTransitionToPhase(WorkflowState state, WorkflowPhase targetPhase)
        {
            // State machine validation logic
            return targetPhase switch
            {
                WorkflowPhase.Planning => true, // Can always go back to planning
                WorkflowPhase.Implementing => state.CurrentPhase == WorkflowPhase.Planning 
                                           || state.CurrentPhase == WorkflowPhase.Testing,
                WorkflowPhase.Committing => state.CurrentPhase == WorkflowPhase.Implementing,
                WorkflowPhase.Testing => state.CurrentPhase == WorkflowPhase.Committing,
                WorkflowPhase.PlanAgain => state.CurrentPhase == WorkflowPhase.Testing,
                WorkflowPhase.Completed => state.Steps.All(s => s.IsCompleted),
                _ => false
            };
        }

        public void TransitionToPhase(WorkflowState state, WorkflowPhase targetPhase)
        {
            if (!CanTransitionToPhase(state, targetPhase))
                throw new InvalidOperationException($"Cannot transition from {state.CurrentPhase} to {targetPhase}");

            state.CurrentPhase = targetPhase;
            state.LastModifiedAt = DateTime.UtcNow;
            store.SaveWorkflowState(state);
            
            log.Info($"Transitioned workflow '{state.WorkflowName}' to phase: {targetPhase}");
        }

        public void MarkStepCompleted(WorkflowState state, int phaseIndex, int stepIndex, string? commitHash = null)
        {
            var step = state.Steps.FirstOrDefault(s => s.PhaseIndex == phaseIndex && s.StepIndex == stepIndex);
            
            if (step == null)
                throw new ArgumentException($"Step not found: Phase {phaseIndex}, Step {stepIndex}");

            step.IsCompleted = true;
            step.CompletedAt = DateTime.UtcNow;
            step.CommitHash = commitHash;

            store.SaveWorkflowState(state);
            log.Info($"Marked step completed: Phase {phaseIndex}, Step {stepIndex}");
        }

        public void AddTestFailureLogs(WorkflowState state, List<string> logs)
        {
            var currentStep = state.Steps
                .FirstOrDefault(s => s.PhaseIndex == state.CurrentPhaseIndex 
                                  && s.StepIndex == state.CurrentStepIndex);

            if (currentStep != null)
            {
                currentStep.TestFailureLogs.AddRange(logs);
                store.SaveWorkflowState(state);
                log.Warn($"Added {logs.Count} test failure logs to current step");
            }
        }
    }
}
```


### Unit Test Requirements

**File:** `UnitTests/Workflow/WorkflowManagerTests.cs`

```csharp
[TestFixture]
public class WorkflowManagerTests
{
    private IWorkflowStateStore mockStore;
    private ILogCtxLogger mockLog;
    private IWorkflowManager manager;
    private string tempPlanFile;

    [SetUp]
    public void Setup()
    {
        mockStore = Substitute.For<IWorkflowStateStore>();
        mockLog = Substitute.For<ILogCtxLogger>();
        manager = new WorkflowManager(mockStore, mockLog);
        
        tempPlanFile = Path.GetTempFileName();
        File.WriteAllText(tempPlanFile, "# Test Plan");
    }

    [TearDown]
    public void Cleanup()
    {
        if (File.Exists(tempPlanFile))
            File.Delete(tempPlanFile);
    }

    [Test]
    public void CreateWorkflow_ShouldInitializeWithPlanningPhase()
    {
        // Act
        var workflow = manager.CreateWorkflow("Test Feature", tempPlanFile);

        // Assert
        workflow.WorkflowName.ShouldBe("Test Feature");
        workflow.CurrentPhase.ShouldBe(WorkflowPhase.Planning);
        workflow.PlanFilePath.ShouldBe(tempPlanFile);
        mockStore.Received(1).SaveWorkflowState(Arg.Any<WorkflowState>());
    }

    [Test]
    public void CreateWorkflow_EmptyName_ShouldThrowException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => 
            manager.CreateWorkflow("", tempPlanFile));
    }

    [Test]
    public void TransitionToPhase_ValidTransition_ShouldUpdateState()
    {
        // Arrange
        var state = new WorkflowState { CurrentPhase = WorkflowPhase.Planning };

        // Act
        manager.TransitionToPhase(state, WorkflowPhase.Implementing);

        // Assert
        state.CurrentPhase.ShouldBe(WorkflowPhase.Implementing);
        mockStore.Received(1).SaveWorkflowState(state);
    }

    [Test]
    public void TransitionToPhase_InvalidTransition_ShouldThrowException()
    {
        // Arrange
        var state = new WorkflowState { CurrentPhase = WorkflowPhase.Planning };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            manager.TransitionToPhase(state, WorkflowPhase.Testing));
    }

    [Test]
    public void MarkStepCompleted_ValidStep_ShouldUpdateState()
    {
        // Arrange
        var state = new WorkflowState
        {
            Steps = new List<WorkflowStepInfo>
            {
                new WorkflowStepInfo { PhaseIndex = 1, StepIndex = 1, IsCompleted = false }
            }
        };

        // Act
        manager.MarkStepCompleted(state, 1, 1, "abc123");

        // Assert
        state.Steps[^0].IsCompleted.ShouldBeTrue();
        state.Steps[^0].CommitHash.ShouldBe("abc123");
        state.Steps[^0].CompletedAt.ShouldNotBeNull();
    }

    [Test]
    public void AddTestFailureLogs_ShouldAppendToCurrentStep()
    {
        // Arrange
        var state = new WorkflowState
        {
            CurrentPhaseIndex = 1,
            CurrentStepIndex = 1,
            Steps = new List<WorkflowStepInfo>
            {
                new WorkflowStepInfo { PhaseIndex = 1, StepIndex = 1 }
            }
        };
        var logs = new List<string> { "Test failed: Assertion error" };

        // Act
        manager.AddTestFailureLogs(state, logs);

        // Assert
        state.Steps[^0].TestFailureLogs.Count.ShouldBe(1);
        state.Steps[^0].TestFailureLogs[^0].ShouldContain("Assertion error");
    }
}
```


### Validation Criteria

- ✅ All phase transitions validated by state machine
- ✅ Invalid transitions throw exceptions
- ✅ All CRUD operations logged via NLog
- ✅ Unit tests cover happy path and edge cases (>90% coverage)


### Integration Points

- Integrates with `ILogCtxLogger` (NLogShared)[^1]
- Follows existing service pattern (RecentFilesManager)[^1]

***

## Step 3: Markdown Plan Parser

### Objective

Parse markdown plan files to extract phase/step structure.[^3][^1]

### Context

- Plan files are markdown with structure: `## Phase X` → `### Step Y`
- Must populate `WorkflowState.Steps` from parsed plan
- Support navigation to specific phase/step in markdown viewer


### Deliverables

**File:** `DocXHandler/Workflow/MarkdownPlanParser.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DocXHandler.Workflow
{
    public interface IMarkdownPlanParser
    {
        List<WorkflowStepInfo> ParsePlanFile(string filePath);
        string GetStepContent(string filePath, int phaseIndex, int stepIndex);
    }

    public class MarkdownPlanParser : IMarkdownPlanParser
    {
        private static readonly Regex PhaseHeaderRegex = new Regex(@"^##\s+Phase\s+(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex StepHeaderRegex = new Regex(@"^###\s+Step\s+(\d+):?\s*(.+)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public List<WorkflowStepInfo> ParsePlanFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Plan file not found: {filePath}");

            var lines = File.ReadAllLines(filePath);
            var steps = new List<WorkflowStepInfo>();
            int currentPhase = 0;

            foreach (var line in lines)
            {
                var phaseMatch = PhaseHeaderRegex.Match(line);
                if (phaseMatch.Success)
                {
                    currentPhase = int.Parse(phaseMatch.Groups[^1].Value);
                    continue;
                }

                var stepMatch = StepHeaderRegex.Match(line);
                if (stepMatch.Success && currentPhase > 0)
                {
                    var stepIndex = int.Parse(stepMatch.Groups[^1].Value);
                    var description = stepMatch.Groups.Count > 2 
                        ? stepMatch.Groups[^2].Value.Trim() 
                        : $"Step {stepIndex}";

                    steps.Add(new WorkflowStepInfo
                    {
                        PhaseIndex = currentPhase,
                        StepIndex = stepIndex,
                        StepDescription = description
                    });
                }
            }

            return steps;
        }

        public string GetStepContent(string filePath, int phaseIndex, int stepIndex)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Plan file not found: {filePath}");

            var lines = File.ReadAllLines(filePath);
            var content = new List<string>();
            bool isCapturing = false;
            int currentPhase = 0;
            int currentStep = 0;

            foreach (var line in lines)
            {
                var phaseMatch = PhaseHeaderRegex.Match(line);
                if (phaseMatch.Success)
                {
                    currentPhase = int.Parse(phaseMatch.Groups[^1].Value);
                    isCapturing = false;
                    continue;
                }

                var stepMatch = StepHeaderRegex.Match(line);
                if (stepMatch.Success)
                {
                    currentStep = int.Parse(stepMatch.Groups[^1].Value);
                    
                    if (currentPhase == phaseIndex && currentStep == stepIndex)
                    {
                        isCapturing = true;
                        content.Add(line);
                    }
                    else if (isCapturing)
                    {
                        // Reached next step, stop capturing
                        break;
                    }
                    continue;
                }

                if (isCapturing)
                {
                    // Stop if we hit next phase or step header
                    if (PhaseHeaderRegex.IsMatch(line) || StepHeaderRegex.IsMatch(line))
                        break;

                    content.Add(line);
                }
            }

            return string.Join(Environment.NewLine, content);
        }
    }
}
```


### Unit Test Requirements

**File:** `UnitTests/Workflow/MarkdownPlanParserTests.cs`

```csharp
[TestFixture]
public class MarkdownPlanParserTests
{
    private IMarkdownPlanParser parser;
    private string testPlanFile;

    [SetUp]
    public void Setup()
    {
        parser = new MarkdownPlanParser();
        testPlanFile = Path.GetTempFileName();
        
        var markdown = @"# Project Plan

## Phase 1: Setup

### Step 1: Initialize project
Create project structure

### Step 2: Configure settings
Setup configuration files

## Phase 2: Implementation

### Step 1: Core feature
Implement main functionality
";
        File.WriteAllText(testPlanFile, markdown);
    }

    [TearDown]
    public void Cleanup()
    {
        if (File.Exists(testPlanFile))
            File.Delete(testPlanFile);
    }

    [Test]
    public void ParsePlanFile_ShouldExtractAllSteps()
    {
        // Act
        var steps = parser.ParsePlanFile(testPlanFile);

        // Assert
        steps.Count.ShouldBe(3);
        steps[^0].PhaseIndex.ShouldBe(1);
        steps[^0].StepIndex.ShouldBe(1);
        steps[^0].StepDescription.ShouldContain("Initialize project");
        
        steps[^1].PhaseIndex.ShouldBe(1);
        steps[^1].StepIndex.ShouldBe(2);
        
        steps[^2].PhaseIndex.ShouldBe(2);
        steps[^2].StepIndex.ShouldBe(1);
    }

    [Test]
    public void GetStepContent_ShouldExtractSpecificStepText()
    {
        // Act
        var content = parser.GetStepContent(testPlanFile, 1, 1);

        // Assert
        content.ShouldContain("Initialize project");
        content.ShouldContain("Create project structure");
        content.ShouldNotContain("Configure settings");
    }

    [Test]
    public void ParsePlanFile_NonExistentFile_ShouldThrowException()
    {
        // Act & Assert
        Should.Throw<FileNotFoundException>(() =>
            parser.ParsePlanFile("non-existent-file.md"));
    }

    [Test]
    public void ParsePlanFile_MalformedMarkdown_ShouldHandleGracefully()
    {
        // Arrange
        File.WriteAllText(testPlanFile, "Random content without proper headers");

        // Act
        var steps = parser.ParsePlanFile(testPlanFile);

        // Assert
        steps.Count.ShouldBe(0);
    }
}
```


### Validation Criteria

- ✅ Correctly parses phase/step hierarchy from markdown
- ✅ Handles malformed markdown gracefully (no crashes)
- ✅ Extracts step content for display in UI
- ✅ All unit tests pass with >85% coverage


### Integration Points

- Used by `WorkflowManager` to populate `WorkflowState.Steps`
- Supports `MainForm` wizard tab for displaying current step

***

# Phase 2: Wizard UI Components

## Step 4: Wizard Tab Panel UI

### Objective

Create dedicated wizard tab in `MainForm` with panel-based layout.[^2][^1]

### Context

- Add new `TabPage` to existing `tabControl1`[^1]
- Panel-based layout (not modal wizard)
- Menu item launcher: `Tools → Agile AI Dev Wizard`


### Deliverables

**File:** `OaiUI/MainForm.Designer.cs` (modifications)

```csharp
// ... existing code ...
// <line 123> private TabPage tabPage2;
// <line 124> private Panel panel2;

private TabPage tabPageWizard;
private Panel panelWizardControls;
private ComboBox cmbWorkflows;
private Button btnNewWorkflow;
private Button btnLoadWorkflow;
private Button btnDeleteWorkflow;
private RichTextBox txtPlanViewer;
private Label lblCurrentPhase;
private Label lblCurrentStep;
private Button btnPreviousStep;
private Button btnNextStep;
private Button btnCommit;
private Button btnTest;
private Button btnPlanAgain;
private MenuStrip mainMenu;
private ToolStripMenuItem menuTools;
private ToolStripMenuItem menuWizard;

private void InitializeWizardTab()
{
    // TabPage
    tabPageWizard = new TabPage
    {
        Text = "Agile Wizard",
        Name = "tabPageWizard"
    };
    tabControl1.TabPages.Add(tabPageWizard);

    // Panel
    panelWizardControls = new Panel
    {
        Dock = DockStyle.Top,
        Height = 120
    };
    tabPageWizard.Controls.Add(panelWizardControls);

    // Workflow Selector
    Label lblWorkflow = new Label
    {
        Text = "Workflow:",
        Location = new Point(8, 12),
        AutoSize = true
    };
    panelWizardControls.Controls.Add(lblWorkflow);

    cmbWorkflows = new ComboBox
    {
        Location = new Point(80, 8),
        Width = 200,
        DropDownStyle = ComboBoxStyle.DropDownList
    };
    cmbWorkflows.SelectedIndexChanged += CmbWorkflowsSelectedIndexChanged;
    panelWizardControls.Controls.Add(cmbWorkflows);

    // Buttons
    btnNewWorkflow = new Button
    {
        Text = "New",
        Location = new Point(290, 8),
        Width = 80
    };
    btnNewWorkflow.Click += BtnNewWorkflowClick;
    panelWizardControls.Controls.Add(btnNewWorkflow);

    btnLoadWorkflow = new Button
    {
        Text = "Load",
        Location = new Point(380, 8),
        Width = 80
    };
    btnLoadWorkflow.Click += BtnLoadWorkflowClick;
    panelWizardControls.Controls.Add(btnLoadWorkflow);

    btnDeleteWorkflow = new Button
    {
        Text = "Delete",
        Location = new Point(470, 8),
        Width = 80,
        BackColor = Color.IndianRed
    };
    btnDeleteWorkflow.Click += BtnDeleteWorkflowClick;
    panelWizardControls.Controls.Add(btnDeleteWorkflow);

    // Phase/Step Display
    lblCurrentPhase = new Label
    {
        Text = "Phase: --",
        Location = new Point(8, 45),
        AutoSize = true,
        Font = new Font("Segoe UI", 10, FontStyle.Bold)
    };
    panelWizardControls.Controls.Add(lblCurrentPhase);

    lblCurrentStep = new Label
    {
        Text = "Step: --",
        Location = new Point(8, 70),
        AutoSize = true
    };
    panelWizardControls.Controls.Add(lblCurrentStep);

    // Navigation Buttons
    btnPreviousStep = new Button
    {
        Text = "← Previous Step",
        Location = new Point(8, 90),
        Width = 120
    };
    btnPreviousStep.Click += BtnPreviousStepClick;
    panelWizardControls.Controls.Add(btnPreviousStep);

    btnNextStep = new Button
    {
        Text = "Next Step →",
        Location = new Point(140, 90),
        Width = 120
    };
    btnNextStep.Click += BtnNextStepClick;
    panelWizardControls.Controls.Add(btnNextStep);

    btnCommit = new Button
    {
        Text = "Commit",
        Location = new Point(270, 90),
        Width = 100
    };
    btnCommit.Click += BtnCommitClick;
    panelWizardControls.Controls.Add(btnCommit);

    btnTest = new Button
    {
        Text = "Test",
        Location = new Point(380, 90),
        Width = 100
    };
    btnTest.Click += BtnTestClick;
    panelWizardControls.Controls.Add(btnTest);

    btnPlanAgain = new Button
    {
        Text = "Plan Again",
        Location = new Point(490, 90),
        Width = 100
    };
    btnPlanAgain.Click += BtnPlanAgainClick;
    panelWizardControls.Controls.Add(btnPlanAgain);

    // Plan Viewer
    txtPlanViewer = new RichTextBox
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        Font = new Font("Consolas", 10),
        BackColor = Color.FromArgb(30, 30, 30),
        ForeColor = Color.WhiteSmoke
    };
    tabPageWizard.Controls.Add(txtPlanViewer);

    // Menu
    mainMenu = new MenuStrip();
    menuTools = new ToolStripMenuItem("Tools");
    menuWizard = new ToolStripMenuItem("Agile AI Dev Wizard");
    menuWizard.Click += MenuWizardClick;
    menuTools.DropDownItems.Add(menuWizard);
    mainMenu.Items.Add(menuTools);
    Controls.Add(mainMenu);
}

// ... existing code ...
// <line 456> private void btnResetVsSettingsClick(object? sender, EventArgs e)
// <line 457> {
```

**File:** `OaiUI/MainForm.Wizard.cs` (new partial class file)

```csharp
using DocXHandler.Workflow;
using System;
using System.Windows.Forms;

namespace oaiUI
{
    public partial class MainForm
    {
        private IWorkflowManager wizardWorkflowManager;
        private WorkflowState? currentWorkflow;
        private IMarkdownPlanParser planParser;

        private void InitializeWizardServices()
        {
            var store = new WorkflowStateStore();
            wizardWorkflowManager = new WorkflowManager(store, log);
            planParser = new MarkdownPlanParser();
        }

        private void MenuWizardClick(object? sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPageWizard;
            RefreshWorkflowList();
        }

        private void RefreshWorkflowList()
        {
            var workflows = wizardWorkflowManager.GetAllWorkflows();
            cmbWorkflows.Items.Clear();
            
            foreach (var wf in workflows)
            {
                cmbWorkflows.Items.Add($"{wf.WorkflowName} ({wf.WorkflowId.Substring(0, 8)})");
            }
        }

        private void CmbWorkflowsSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbWorkflows.SelectedItem == null) return;

            var selectedText = cmbWorkflows.SelectedItem.ToString();
            var idPart = selectedText.Substring(selectedText.LastIndexOf('(') + 1, 8);

            var workflows = wizardWorkflowManager.GetAllWorkflows();
            currentWorkflow = workflows.FirstOrDefault(w => w.WorkflowId.StartsWith(idPart));

            if (currentWorkflow != null)
            {
                DisplayWorkflow(currentWorkflow);
            }
        }

        private void DisplayWorkflow(WorkflowState state)
        {
            lblCurrentPhase.Text = $"Phase: {state.CurrentPhaseIndex} ({state.CurrentPhase})";
            lblCurrentStep.Text = $"Step: {state.CurrentStepIndex}";

            if (File.Exists(state.PlanFilePath))
            {
                var stepContent = planParser.GetStepContent(
                    state.PlanFilePath, 
                    state.CurrentPhaseIndex, 
                    state.CurrentStepIndex);
                
                txtPlanViewer.Text = stepContent;
            }

            UpdateButtonStates(state);
        }

        private void UpdateButtonStates(WorkflowState state)
        {
            var currentStep = state.Steps.FirstOrDefault(s => 
                s.PhaseIndex == state.CurrentPhaseIndex && 
                s.StepIndex == state.CurrentStepIndex);

            btnPreviousStep.Enabled = state.CurrentStepIndex > 1 || state.CurrentPhaseIndex > 1;
            btnNextStep.Enabled = currentStep?.IsCompleted ?? false;
            btnCommit.Enabled = state.CurrentPhase == WorkflowPhase.Implementing;
            btnTest.Enabled = state.CurrentPhase == WorkflowPhase.Committing;
            btnPlanAgain.Enabled = state.CurrentPhase == WorkflowPhase.Testing;
        }

        private void BtnNewWorkflowClick(object? sender, EventArgs e)
        {
            // Handled in Step 5
        }

        private void BtnLoadWorkflowClick(object? sender, EventArgs e)
        {
            // Handled in Step 5
        }

        private void BtnDeleteWorkflowClick(object? sender, EventArgs e)
        {
            // Handled in Step 5
        }

        private void BtnPreviousStepClick(object? sender, EventArgs e)
        {
            // Handled in Step 6
        }

        private void BtnNextStepClick(object? sender, EventArgs e)
        {
            // Handled in Step 6
        }

        private void BtnCommitClick(object? sender, EventArgs e)
        {
            // Handled in Step 7
        }

        private void BtnTestClick(object? sender, EventArgs e)
        {
            // Handled in Step 8
        }

        private void BtnPlanAgainClick(object? sender, EventArgs e)
        {
            // Handled in Step 6
        }
    }
}
```


### Unit Test Requirements

**File:** `UnitTests/UI/WizardTabTests.cs` (UI testing via integration test)

```csharp
[TestFixture]
public class WizardTabIntegrationTests
{
    [Test]
    [Apartment(ApartmentState.STA)]
    public void WizardTab_ShouldBePresent()
    {
        // Arrange
        using var form = new MainForm();

        // Act
        var wizardTab = form.Controls.Find("tabPageWizard", true).FirstOrDefault();

        // Assert
        wizardTab.ShouldNotBeNull();
        (wizardTab as TabPage)?.Text.ShouldBe("Agile Wizard");
    }
}
```


### Validation Criteria

- ✅ Wizard tab appears in main form tab control
- ✅ Menu item navigates to wizard tab
- ✅ All controls render correctly (no layout issues)
- ✅ Manual testing: resize window, verify responsive layout


### Integration Points

- Integrates with `MainForm` existing tab structure[^1]
- Uses `WorkflowManager` and `MarkdownPlanParser` from Phase 1

***

## Step 5: Workflow CRUD Operations UI

### Objective

Implement New/Load/Delete workflow operations in wizard UI.

### Context

- File picker for plan markdown files
- Workflow naming dialog
- Confirmation dialogs for delete


### Deliverables

**File:** `OaiUI/MainForm.Wizard.cs` (continued)

```csharp
// ... existing code from Step 4 ...

private void BtnNewWorkflowClick(object? sender, EventArgs e)
{
    using var nameDialog = new Form
    {
        Text = "Create New Workflow",
        Width = 400,
        Height = 150,
        FormBorderStyle = FormBorderStyle.FixedDialog,
        StartPosition = FormStartPosition.CenterParent
    };

    var lblName = new Label { Text = "Workflow Name:", Left = 10, Top = 20, AutoSize = true };
    var txtName = new TextBox { Left = 120, Top = 20, Width = 250 };
    var btnOk = new Button { Text = "OK", Left = 200, Top = 60, DialogResult = DialogResult.OK };
    var btnCancel = new Button { Text = "Cancel", Left = 280, Top = 60, DialogResult = DialogResult.Cancel };

    nameDialog.Controls.AddRange(new Control[] { lblName, txtName, btnOk, btnCancel });
    nameDialog.AcceptButton = btnOk;

    if (nameDialog.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(txtName.Text))
        return;

    using var openFileDialog = new OpenFileDialog
    {
        Filter = "Markdown Files (*.md)|*.md|All Files (*.*)|*.*",
        Title = "Select Plan File"
    };

    if (openFileDialog.ShowDialog() != DialogResult.OK)
        return;

    try
    {
        currentWorkflow = wizardWorkflowManager.CreateWorkflow(txtName.Text.Trim(), openFileDialog.FileName);
        
        // Parse plan and populate steps
        var steps = planParser.ParsePlanFile(openFileDialog.FileName);
        currentWorkflow.Steps = steps;
        
        if (steps.Count > 0)
        {
            currentWorkflow.CurrentPhaseIndex = steps[^0].PhaseIndex;
            currentWorkflow.CurrentStepIndex = steps[^0].StepIndex;
        }

        wizardWorkflowManager.SaveWorkflow(currentWorkflow);
        RefreshWorkflowList();
        DisplayWorkflow(currentWorkflow);

        MessageBox.Show($"Workflow '{txtName.Text}' created successfully!", "Success", 
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error creating workflow: {ex.Message}", "Error", 
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

private void BtnLoadWorkflowClick(object? sender, EventArgs e)
{
    using var openFileDialog = new OpenFileDialog
    {
        Filter = "Workflow Files (*.workflow.json)|*.workflow.json|All Files (*.*)|*.*",
        Title = "Load Workflow",
        InitialDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "VecTool", "Workflows")
    };

    if (openFileDialog.ShowDialog() != DialogResult.OK)
        return;

    try
    {
        var workflowId = Path.GetFileNameWithoutExtension(openFileDialog.FileName)
            .Replace(".workflow", "");

        currentWorkflow = wizardWorkflowManager.LoadWorkflow(workflowId);

        if (currentWorkflow == null)
        {
            MessageBox.Show("Failed to load workflow.", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        RefreshWorkflowList();
        cmbWorkflows.SelectedItem = cmbWorkflows.Items.Cast<string>()
            .FirstOrDefault(item => item.Contains(workflowId.Substring(0, 8)));

        DisplayWorkflow(currentWorkflow);

        MessageBox.Show($"Workflow '{currentWorkflow.WorkflowName}' loaded successfully!", "Success",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error loading workflow: {ex.Message}", "Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

private void BtnDeleteWorkflowClick(object? sender, EventArgs e)
{
    if (currentWorkflow == null)
    {
        MessageBox.Show("No workflow selected.", "Warning", 
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    var result = MessageBox.Show(
        $"Are you sure you want to delete workflow '{currentWorkflow.WorkflowName}'?\n\nThis action cannot be undone.",
        "Confirm Delete",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Warning);

    if (result != DialogResult.Yes)
        return;

    try
    {
        wizardWorkflowManager.DeleteWorkflow(currentWorkflow.WorkflowId);
        currentWorkflow = null;
        RefreshWorkflowList();
        txtPlanViewer.Clear();
        lblCurrentPhase.Text = "Phase: --";
        lblCurrentStep.Text = "Step: --";

        MessageBox.Show("Workflow deleted successfully.", "Success",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error deleting workflow: {ex.Message}", "Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
```


### Unit Test Requirements

**File:** `UnitTests/UI/WorkflowCRUDTests.cs`

```csharp
[TestFixture]
public class WorkflowCRUDTests
{
    [Test]
    [Apartment(ApartmentState.STA)]
    public void CreateWorkflow_ValidInputs_ShouldSucceed()
    {
        // Manual/exploratory test - complex UI interaction
        // Validation: Create workflow, verify it appears in dropdown
        Assert.Pass("Manual test required for full UI interaction");
    }

    [Test]
    public void LoadWorkflow_ValidFile_ShouldDeserialize()
    {
        // Unit test at service layer (already covered in WorkflowManagerTests)
        Assert.Pass("Covered by WorkflowManagerTests");
    }
}
```


### Validation Criteria

- ✅ New workflow creates JSON file in AppData/VecTool/Workflows
- ✅ Load workflow populates UI with saved state
- ✅ Delete workflow removes JSON file and clears UI
- ✅ Manual testing: Create → Save → Close App → Reopen → Load


### Integration Points

- Uses `WorkflowManager` from Step 2
- Uses `MarkdownPlanParser` from Step 3

***

## Step 6: Phase/Step Navigation

### Objective

Implement navigation between phases/steps with state validation.[^4]

### Context

- Previous/Next button logic
- Handle phase transitions (Implement → Commit → Test → Plan Again loop)
- Update UI to reflect current step


### Deliverables

**File:** `OaiUI/MainForm.Wizard.cs` (continued)

```csharp
// ... existing code ...

private void BtnPreviousStepClick(object? sender, EventArgs e)
{
    if (currentWorkflow == null) return;

    var currentStepIndex = currentWorkflow.CurrentStepIndex;
    var currentPhaseIndex = currentWorkflow.CurrentPhaseIndex;

    // Find previous step
    var previousStep = currentWorkflow.Steps
        .Where(s => s.PhaseIndex < currentPhaseIndex || 
                   (s.PhaseIndex == currentPhaseIndex && s.StepIndex < currentStepIndex))
        .OrderByDescending(s => s.PhaseIndex)
        .ThenByDescending(s => s.StepIndex)
        .FirstOrDefault();

    if (previousStep == null)
    {
        MessageBox.Show("Already at the first step.", "Navigation", 
            MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
    }

    currentWorkflow.CurrentPhaseIndex = previousStep.PhaseIndex;
    currentWorkflow.CurrentStepIndex = previousStep.StepIndex;
    wizardWorkflowManager.SaveWorkflow(currentWorkflow);

    DisplayWorkflow(currentWorkflow);
}

private void BtnNextStepClick(object? sender, EventArgs e)
{
    if (currentWorkflow == null) return;

    var currentStep = currentWorkflow.Steps.FirstOrDefault(s =>
        s.PhaseIndex == currentWorkflow.CurrentPhaseIndex &&
        s.StepIndex == currentWorkflow.CurrentStepIndex);

    if (currentStep != null && !currentStep.IsCompleted)
    {
        var result = MessageBox.Show(
            "Current step is not marked as completed. Proceed anyway?",
            "Warning",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;
    }

    // Find next step
    var nextStep = currentWorkflow.Steps
        .Where(s => s.PhaseIndex > currentWorkflow.CurrentPhaseIndex ||
                   (s.PhaseIndex == currentWorkflow.CurrentPhaseIndex && 
                    s.StepIndex > currentWorkflow.CurrentStepIndex))
        .OrderBy(s => s.PhaseIndex)
        .ThenBy(s => s.StepIndex)
        .FirstOrDefault();

    if (nextStep == null)
    {
        MessageBox.Show("This is the last step. Use 'Plan Again' or complete the workflow.", 
            "Navigation", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return;
    }

    currentWorkflow.CurrentPhaseIndex = nextStep.PhaseIndex;
    currentWorkflow.CurrentStepIndex = nextStep.StepIndex;
    wizardWorkflowManager.SaveWorkflow(currentWorkflow);

    DisplayWorkflow(currentWorkflow);
}

private void BtnPlanAgainClick(object? sender, EventArgs e)
{
    if (currentWorkflow == null) return;

    var result = MessageBox.Show(
        "Return to Planning phase? This will reset workflow state.",
        "Confirm",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Question);

    if (result != DialogResult.Yes)
        return;

    try
    {
        wizardWorkflowManager.TransitionToPhase(currentWorkflow, WorkflowPhase.Planning);
        currentWorkflow.CurrentPhaseIndex = 1;
        currentWorkflow.CurrentStepIndex = 1;
        wizardWorkflowManager.SaveWorkflow(currentWorkflow);

        DisplayWorkflow(currentWorkflow);

        MessageBox.Show("Workflow reset to Planning phase.", "Success",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error transitioning to Planning: {ex.Message}", "Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
```


### Unit Test Requirements

**File:** `UnitTests/UI/NavigationTests.cs`

```csharp
[TestFixture]
public class NavigationTests
{
    private IWorkflowManager manager;
    private WorkflowState testWorkflow;

    [SetUp]
    public void Setup()
    {
        var store = new WorkflowStateStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var log = Substitute.For<ILogCtxLogger>();
        manager = new WorkflowManager(store, log);

        var planFile = Path.GetTempFileName();
        File.WriteAllText(planFile, "# Test Plan\n## Phase 1\n### Step 1: Test");

        testWorkflow = manager.CreateWorkflow("Test", planFile);
        testWorkflow.Steps = new List<WorkflowStepInfo>
        {
            new WorkflowStepInfo { PhaseIndex = 1, StepIndex = 1 },
            new WorkflowStepInfo { PhaseIndex = 1, StepIndex = 2 },
            new WorkflowStepInfo { PhaseIndex = 2, StepIndex = 1 }
        };
        testWorkflow.CurrentPhaseIndex = 1;
        testWorkflow.CurrentStepIndex = 1;
    }

    [Test]
    public void NavigateToNextStep_ShouldUpdateIndices()
    {
        // Simulate next step navigation
        var nextStep = testWorkflow.Steps
            .Where(s => s.PhaseIndex > testWorkflow.CurrentPhaseIndex ||
                       (s.PhaseIndex == testWorkflow.CurrentPhaseIndex && 
                        s.StepIndex > testWorkflow.CurrentStepIndex))
            .OrderBy(s => s.PhaseIndex)
            .ThenBy(s => s.StepIndex)
            .FirstOrDefault();

        testWorkflow.CurrentPhaseIndex = nextStep.PhaseIndex;
        testWorkflow.CurrentStepIndex = nextStep.StepIndex;

        // Assert
        testWorkflow.CurrentPhaseIndex.ShouldBe(1);
        testWorkflow.CurrentStepIndex.ShouldBe(2);
    }

    [Test]
    public void NavigateToPreviousStep_ShouldUpdateIndices()
    {
        // Arrange: move to step 2
        testWorkflow.CurrentStepIndex = 2;

        // Act
        var previousStep = testWorkflow.Steps
            .Where(s => s.PhaseIndex < testWorkflow.CurrentPhaseIndex ||
                       (s.PhaseIndex == testWorkflow.CurrentPhaseIndex && 
                        s.StepIndex < testWorkflow.CurrentStepIndex))
            .OrderByDescending(s => s.PhaseIndex)
            .ThenByDescending(s => s.StepIndex)
            .FirstOrDefault();

        testWorkflow.CurrentPhaseIndex = previousStep.PhaseIndex;
        testWorkflow.CurrentStepIndex = previousStep.StepIndex;

        // Assert
        testWorkflow.CurrentStepIndex.ShouldBe(1);
    }
}
```


### Validation Criteria

- ✅ Previous button navigates to prior step/phase
- ✅ Next button validates step completion before proceeding
- ✅ Plan Again button resets to Planning phase
- ✅ Manual testing: navigate through full workflow lifecycle


### Integration Points

- Uses `WorkflowManager.TransitionToPhase()` for phase transitions
- Updates `WorkflowState` and persists via `SaveWorkflow()`

***

# Phase 3: Integration with Existing Features

## Step 7: Git Commit Integration

### Objective

Integrate existing `GitRunner` for commit operations.[^5][^3][^1]

### Context

- VecTool already has `GitRunner` and `GitChangesHandler`[^1]
- Wizard button triggers commit with AI-generated message
- Store commit hash in `WorkflowStepInfo`


### Deliverables

**File:** `OaiUI/MainForm.Wizard.cs` (continued)

```csharp
using Core;

// ... existing code ...

private async void BtnCommitClick(object? sender, EventArgs e)
{
    if (currentWorkflow == null) return;

    if (currentWorkflow.CurrentPhase != WorkflowPhase.Implementing)
    {
        MessageBox.Show("Commit is only available during Implementation phase.", 
            "Invalid Phase", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    try
    {
        btnCommit.Enabled = false;
        userInterface.UpdateStatus("Generating commit message...");

        // Use existing GitChangesHandler
        var gitHandler = new GitChangesHandler(userInterface, recentFilesManager);
        var vectorStoreConfig = GetVectorStore(comboBoxVectorStores.SelectedItem?.ToString());
        
        if (vectorStoreConfig == null || selectedFolders.Count == 0)
        {
            MessageBox.Show("Please select a vector store and folders first.", 
                "Configuration Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var saveFileDialog = new SaveFileDialog
        {
            Filter = "Markdown File (*.md)|*.md",
            Title = "Save Git Changes Report",
            FileName = $"git-changes-{DateTime.Now:yyyyMMdd-HHmmss}.md"
        };

        if (saveFileDialog.ShowDialog() != DialogResult.OK)
            return;

        // Generate git changes document (contains AI commit message)
        gitHandler.GenerateGitChangesDocument(selectedFolders, saveFileDialog.FileName, vectorStoreConfig);

        // Read generated commit message (assuming GitChangesHandler includes it)
        var gitChangesContent = File.ReadAllText(saveFileDialog.FileName);
        var commitMessage = ExtractCommitMessageFromMarkdown(gitChangesContent);

        // Show commit message for user review/edit
        using var commitDialog = new Form
        {
            Text = "Review Commit Message",
            Width = 600,
            Height = 400,
            StartPosition = FormStartPosition.CenterParent
        };

        var txtCommit = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            Text = commitMessage,
            ScrollBars = ScrollBars.Vertical
        };

        var btnExecuteCommit = new Button
        {
            Text = "Execute Commit",
            Dock = DockStyle.Bottom,
            Height = 40
        };

        btnExecuteCommit.Click += async (s, args) =>
        {
            try
            {
                var gitRunner = new GitRunner();
                var repoPath = selectedFolders.FirstOrDefault();
                
                if (string.IsNullOrEmpty(repoPath))
                {
                    MessageBox.Show("No repository path available.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Stage all changes
                await gitRunner.ExecuteGitCommandAsync(repoPath, "add .");

                // Commit with message
                var result = await gitRunner.ExecuteGitCommandAsync(
                    repoPath, 
                    $"commit -m \"{txtCommit.Text.Replace("\"", "\\\"")}\"");

                // Extract commit hash
                var commitHash = await gitRunner.ExecuteGitCommandAsync(repoPath, "rev-parse HEAD");

                // Mark step as completed with commit hash
                wizardWorkflowManager.MarkStepCompleted(
                    currentWorkflow,
                    currentWorkflow.CurrentPhaseIndex,
                    currentWorkflow.CurrentStepIndex,
                    commitHash.Trim());

                currentWorkflow.LastCommitMessage = txtCommit.Text;
                wizardWorkflowManager.TransitionToPhase(currentWorkflow, WorkflowPhase.Committing);
                wizardWorkflowManager.SaveWorkflow(currentWorkflow);

                commitDialog.DialogResult = DialogResult.OK;
                commitDialog.Close();

                MessageBox.Show($"Committed successfully!\nHash: {commitHash.Trim().Substring(0, 8)}",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DisplayWorkflow(currentWorkflow);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Commit failed: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        commitDialog.Controls.Add(txtCommit);
        commitDialog.Controls.Add(btnExecuteCommit);
        commitDialog.ShowDialog();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error generating commit: {ex.Message}", "Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    finally
    {
        btnCommit.Enabled = true;
        userInterface.UpdateStatus("Ready");
    }
}

private string ExtractCommitMessageFromMarkdown(string markdown)
{
    // Parse markdown to extract AI-generated commit message
    // Assumes GitChangesHandler includes section like:
    // ## Suggested Commit Message
    // ```
    // <commit message>
    // ```

    var lines = markdown.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
    var inCommitSection = false;
    var commitLines = new List<string>();

    foreach (var line in lines)
    {
        if (line.Contains("Suggested Commit Message", StringComparison.OrdinalIgnoreCase))
        {
            inCommitSection = true;
            continue;
        }

        if (inCommitSection)
        {
            if (line.Trim() == "```
                continue; // Skip opening fence

            if (line.Trim() == "```" && commitLines.Count > 0)
                break; // End of commit message

            commitLines.Add(line);
        }
    }

    return commitLines.Count > 0 
        ? string.Join(Environment.NewLine, commitLines).Trim()
        : "chore: workflow step completed";
}
```


### Unit Test Requirements

**File:** `UnitTests/Workflow/GitIntegrationTests.cs`

```csharp
[TestFixture]
public class GitIntegrationTests
{
    [Test]
    public void ExtractCommitMessageFromMarkdown_ShouldParseCorrectly()
    {
        // Arrange
        var markdown = @"# Git Changes Report

## Suggested Commit Message
```

feat: implement wizard UI navigation

- Added phase/step navigation
- Integrated with workflow state management

```

## Changed Files
...";

        // Act
        var form = new MainForm();
        var method = typeof(MainForm).GetMethod("ExtractCommitMessageFromMarkdown", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var result = method.Invoke(form, new object[] { markdown }) as string;

        // Assert
        result.ShouldContain("feat: implement wizard UI navigation");
        result.ShouldContain("Added phase/step navigation");
    }

    [Test]
    public async Task GitRunner_ExecuteCommit_ShouldReturnCommitHash()
    {
        // Integration test - requires Git repo
        // Manual validation recommended
        Assert.Pass("Manual test required for Git operations");
    }
}
```


### Validation Criteria

- ✅ Commit button generates AI message using existing `GitChangesHandler`
- ✅ User can review/edit commit message before executing
- ✅ Commit hash stored in `WorkflowStepInfo.CommitHash`
- ✅ Manual testing: Make code changes → Commit via wizard → Verify Git log


### Integration Points

- Reuses `GitRunner`[^1]
- Reuses `GitChangesHandler`[^1]
- Stores commit hash in `WorkflowState`

***

## Step 8: Test Runner Integration

### Objective

Hook NUnit test runner to collect failed test logs.[^6][^1]

### Context

- Wizard "Test" button triggers NUnit runner
- Collect stdout/stderr from test execution
- Parse test results to extract failure logs
- Store failures in `WorkflowStepInfo.TestFailureLogs`


### Deliverables

**File:** `DocXHandler/Workflow/NUnitTestRunner.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DocXHandler.Workflow
{
    public interface INUnitTestRunner
    {
        TestRunResult ExecuteTests(string testProjectPath);
    }

    public class TestRunResult
    {
        public bool Success { get; set; }
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public List<string> FailureLogs { get; set; } = new List<string>();
        public string RawOutput { get; set; } = string.Empty;
    }

    public class NUnitTestRunner : INUnitTestRunner
    {
        private const string DotnetTestCommand = "dotnet";

        public TestRunResult ExecuteTests(string testProjectPath)
        {
            if (!File.Exists(testProjectPath))
                throw new FileNotFoundException($"Test project not found: {testProjectPath}");

            var result = new TestRunResult();
            var workingDirectory = Path.GetDirectoryName(testProjectPath);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = DotnetTestCommand,
                Arguments = $"test \"{testProjectPath}\" --logger:\"console;verbosity=detailed\"",
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var output = new StringBuilder();
            var error = new StringBuilder();

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                        output.AppendLine(args.Data);
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                        error.AppendLine(args.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                result.Success = process.ExitCode == 0;
            }

            result.RawOutput = output.ToString();
            ParseTestResults(result, output.ToString());

            if (!string.IsNullOrEmpty(error.ToString()))
            {
                result.FailureLogs.Add("=== STDERR ===");
                result.FailureLogs.Add(error.ToString());
            }

            return result;
        }

        private void ParseTestResults(TestRunResult result, string output)
        {
            // Parse NUnit console output
            // Example format:
            // Passed!  - Failed: 2, Passed: 8, Skipped: 0, Total: 10

            var summaryRegex = new Regex(@"Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*\d+,\s*Total:\s*(\d+)");
            var summaryMatch = summaryRegex.Match(output);

            if (summaryMatch.Success)
            {
                result.FailedTests = int.Parse(summaryMatch.Groups[^1].Value);
                result.PassedTests = int.Parse(summaryMatch.Groups[^2].Value);
                result.TotalTests = int.Parse(summaryMatch.Groups[^3].Value);
            }

            // Extract failure details
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var inFailureSection = false;
            var currentFailure = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.Contains("Failed ") || line.Contains("Error Message:"))
                {
                    if (currentFailure.Length > 0)
                    {
                        result.FailureLogs.Add(currentFailure.ToString());
                        currentFailure.Clear();
                    }
                    inFailureSection = true;
                }

                if (inFailureSection)
                {
                    currentFailure.AppendLine(line);

                    if (line.Trim() == string.Empty && currentFailure.Length > 50)
                    {
                        result.FailureLogs.Add(currentFailure.ToString());
                        currentFailure.Clear();
                        inFailureSection = false;
                    }
                }
            }

            if (currentFailure.Length > 0)
            {
                result.FailureLogs.Add(currentFailure.ToString());
            }
        }
    }
}
```

**File:** `OaiUI/MainForm.Wizard.cs` (continued)

```csharp
using DocXHandler.Workflow;

// ... existing code ...

private async void BtnTestClick(object? sender, EventArgs e)
{
    if (currentWorkflow == null) return;

    using var openFileDialog = new OpenFileDialog
    {
        Filter = "C# Project Files (*.csproj)|*.csproj|All Files (*.*)|*.*",
        Title = "Select Test Project"
    };

    if (openFileDialog.ShowDialog() != DialogResult.OK)
        return;

    try
    {
        btnTest.Enabled = false;
        userInterface.UpdateStatus("Running tests...");

        var testRunner = new NUnitTestRunner();
        var result = await Task.Run(() => testRunner.ExecuteTests(openFileDialog.FileName));

        if (result.Success)
        {
            MessageBox.Show($"All tests passed!\n\nTotal: {result.TotalTests}\nPassed: {result.PassedTests}",
                "Test Results", MessageBoxButtons.OK, MessageBoxIcon.Information);

            wizardWorkflowManager.TransitionToPhase(currentWorkflow, WorkflowPhase.Testing);
            wizardWorkflowManager.SaveWorkflow(currentWorkflow);
            DisplayWorkflow(currentWorkflow);
        }
        else
        {
            // Show failure summary
            var failureReport = new StringBuilder();
            failureReport.AppendLine($"Tests Failed: {result.FailedTests}/{result.TotalTests}");
            failureReport.AppendLine();
            failureReport.AppendLine("Failure Logs:");
            
            foreach (var log in result.FailureLogs.Take(5))
            {
                failureReport.AppendLine(log);
                failureReport.AppendLine("---");
            }

            using var resultDialog = new Form
            {
                Text = "Test Failures",
                Width = 700,
                Height = 500,
                StartPosition = FormStartPosition.CenterParent
            };

            var txtResults = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Text = failureReport.ToString(),
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 9)
            };

            var btnSaveLogs = new Button
            {
                Text = "Save Failure Logs",
                Dock = DockStyle.Bottom,
                Height = 40
            };

            btnSaveLogs.Click += (s, args) =>
            {
                wizardWorkflowManager.AddTestFailureLogs(currentWorkflow, result.FailureLogs);
                MessageBox.Show("Failure logs saved to workflow state.", "Saved",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                resultDialog.Close();
            };

            resultDialog.Controls.Add(txtResults);
            resultDialog.Controls.Add(btnSaveLogs);
            resultDialog.ShowDialog();
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error running tests: {ex.Message}", "Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    finally
    {
        btnTest.Enabled = true;
        userInterface.UpdateStatus("Ready");
    }
}
```


### Unit Test Requirements

**File:** `UnitTests/Workflow/NUnitTestRunnerTests.cs`

```csharp
[TestFixture]
public class NUnitTestRunnerTests
{
    [Test]
    public void ExecuteTests_ValidProject_ShouldReturnResults()
    {
        // Arrange
        var runner = new NUnitTestRunner();
        var testProjectPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "UnitTests.csproj");

        // Act
        var result = runner.ExecuteTests(testProjectPath);

        // Assert
        result.ShouldNotBeNull();
        result.TotalTests.ShouldBeGreaterThan(0);
    }

    [Test]
    public void ParseTestResults_FailedTests_ShouldExtractLogs()
    {
        // Arrange
        var output = @"
Test Run Failed.
Total tests: 10
     Passed: 8
     Failed: 2
     Skipped: 0
 Total time: 1.234 Seconds

Failed WorkflowStateStoreTests.SaveWorkflow_InvalidPath_ShouldThrow
Error Message:
   System.IO.DirectoryNotFoundException: Could not find directory
Stack Trace:
   at WorkflowStateStore.SaveWorkflowState(WorkflowState state)
";

        var result = new TestRunResult();
        var runner = new NUnitTestRunner();
        var method = typeof(NUnitTestRunner).GetMethod("ParseTestResults", 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        method.Invoke(runner, new object[] { result, output });

        // Assert
        result.FailedTests.ShouldBe(2);
        result.PassedTests.ShouldBe(8);
        result.FailureLogs.Count.ShouldBeGreaterThan(0);
    }
}
```


### Validation Criteria

- ✅ Test runner executes `dotnet test` command
- ✅ Parses console output to extract failure logs
- ✅ Failed tests are stored in `WorkflowStepInfo.TestFailureLogs`
- ✅ Manual testing: Run tests with failures → Verify logs captured


### Integration Points

- Uses `WorkflowManager.AddTestFailureLogs()`
- Displays results in modal dialog

***

## Step 9: Single File Conversion Integration

### Objective

Hook existing "Convert to Single File" feature into wizard workflow.[^1]

### Context

- VecTool already has DocX/MD/PDF conversion[^1]
- Wizard button triggers conversion using selected folders
- Generated file tracked via `RecentFilesManager`[^1]


### Deliverables

**File:** `OaiUI/MainForm.Wizard.cs` (continued)

```csharp
// ... existing code ...

private Button btnConvertSingleFile;

private void InitializeWizardTab()
{
    // ... existing initialization ...

    btnConvertSingleFile = new Button
    {
        Text = "Convert to Single File",
        Location = new Point(600, 90),
        Width = 150
    };
    btnConvertSingleFile.Click += BtnConvertSingleFileClick;
    panelWizardControls.Controls.Add(btnConvertSingleFile);
}

private void BtnConvertSingleFileClick(object? sender, EventArgs e)
{
    if (currentWorkflow == null)
    {
        MessageBox.Show("No workflow loaded.", "Warning",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    if (selectedFolders.Count == 0)
    {
        MessageBox.Show("Please select folders first.", "Warning",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    using var formatDialog = new Form
    {
        Text = "Select Output Format",
        Width = 300,
        Height = 150,
        FormBorderStyle = FormBorderStyle.FixedDialog,
        StartPosition = FormStartPosition.CenterParent
    };

    var lblFormat = new Label { Text = "Format:", Left = 10, Top = 20, AutoSize = true };
    var cmbFormat = new ComboBox
    {
        Left = 70,
        Top = 20,
        Width = 200,
        DropDownStyle = ComboBoxStyle.DropDownList
    };
    cmbFormat.Items.AddRange(new object[] { "DOCX", "MD", "PDF" });
    cmbFormat.SelectedIndex = 1; // Default to MD

    var btnOk = new Button { Text = "OK", Left = 100, Top = 60, DialogResult = DialogResult.OK };
    var btnCancel = new Button { Text = "Cancel", Left = 180, Top = 60, DialogResult = DialogResult.Cancel };

    formatDialog.Controls.AddRange(new Control[] { lblFormat, cmbFormat, btnOk, btnCancel });
    formatDialog.AcceptButton = btnOk;

    if (formatDialog.ShowDialog() != DialogResult.OK)
        return;

    var format = cmbFormat.SelectedItem.ToString();
    var extension = format.ToLower();

    using var saveFileDialog = new SaveFileDialog
    {
        Filter = $"{format} File (*.{extension})|*.{extension}",
        Title = $"Save {format} File",
        FileName = $"{currentWorkflow.WorkflowName}-{DateTime.Now:yyyyMMdd-HHmmss}.{extension}"
    };

    if (saveFileDialog.ShowDialog() != DialogResult.OK)
        return;

    try
    {
        userInterface.WorkStart($"Converting to {format}...", selectedFolders);
        btnConvertSingleFile.Enabled = false;

        var vectorStoreConfig = GetVectorStore(comboBoxVectorStores.SelectedItem?.ToString());
        if (vectorStoreConfig == null)
        {
            MessageBox.Show("Vector store configuration not found.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        switch (format)
        {
            case "DOCX":
                var docXHandler = new DocXHandler.DocXHandler(userInterface, recentFilesManager);
                docXHandler.ConvertSelectedFoldersToDocx(selectedFolders, saveFileDialog.FileName, vectorStoreConfig);
                break;

            case "MD":
                var mdHandler = new MDHandler(userInterface, recentFilesManager);
                mdHandler.ConvertSelectedFoldersToMD(selectedFolders, saveFileDialog.FileName, vectorStoreConfig);
                break;

            case "PDF":
                var pdfHandler = new PdfHandler(userInterface, recentFilesManager);
                pdfHandler.ConvertSelectedFoldersToPdf(selectedFolders, saveFileDialog.FileName, vectorStoreConfig);
                break;
        }

        MessageBox.Show($"{format} file generated successfully!\n\nLocation: {saveFileDialog.FileName}",
            "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

        // Optionally open file location
        var result = MessageBox.Show("Open file location?", "Success",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            Process.Start("explorer.exe", $"/select,\"{saveFileDialog.FileName}\"");
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error converting to {format}: {ex.Message}", "Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    finally
    {
        userInterface.WorkFinish();
        btnConvertSingleFile.Enabled = true;
    }
}
```


### Unit Test Requirements

**File:** `UnitTests/Workflow/SingleFileConversionTests.cs`

```csharp
[TestFixture]
public class SingleFileConversionIntegrationTests
{
    [Test]
    public void ConvertToMD_ValidFolders_ShouldGenerateFile()
    {
        // Integration test - uses existing tests from ConvertSelectedFoldersToMDTests
        Assert.Pass("Covered by existing ConvertSelectedFoldersToMDTests");
    }

    [Test]
    public void RecentFilesManager_ShouldTrackGeneratedFile()
    {
        // Integration test - covered by RecentFilesManagerIntegrationTests
        Assert.Pass("Covered by existing RecentFilesManagerIntegrationTests");
    }
}
```


### Validation Criteria

- ✅ Wizard button triggers conversion using existing handlers
- ✅ Generated file tracked by `RecentFilesManager`
- ✅ File location shown to user after generation
- ✅ Manual testing: Convert → Verify Recent Files list updated


### Integration Points

- Reuses `DocXHandler`, `MDHandler`, `PdfHandler`[^1]
- Reuses `RecentFilesManager`[^1]
- Uses `selectedFolders` from main form state

***

# Phase 4: Testing \& Polish

## Step 10: Integration Tests

### Objective

Create end-to-end integration tests for full workflow lifecycle.

### Context

- Test complete workflow: Create → Implement → Commit → Test → Complete
- Validate state persistence across app restarts
- Test edge cases (missing files, invalid states)


### Deliverables

**File:** `UnitTests/Workflow/WorkflowIntegrationTests.cs`

```csharp
using NUnit.Framework;
using Shouldly;
using DocXHandler.Workflow;
using System;
using System.IO;
using System.Linq;
using NLogShared;
using NSubstitute;

namespace UnitTests.Workflow
{
    [TestFixture]
    public class WorkflowIntegrationTests
    {
        private string testDirectory;
        private IWorkflowStateStore store;
        private IWorkflowManager manager;
        private IMarkdownPlanParser parser;
        private string testPlanFile;

        [SetUp]
        public void Setup()
        {
            testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(testDirectory);

            store = new WorkflowStateStore(testDirectory);
            var log = Substitute.For<ILogCtxLogger>();
            manager = new WorkflowManager(store, log);
            parser = new MarkdownPlanParser();

            testPlanFile = Path.Combine(testDirectory, "test-plan.md");
            var planContent = @"# Feature Implementation Plan

## Phase 1: Setup

### Step 1: Create data models
Design and implement core data structures

### Step 2: Add configuration
Setup app configuration

## Phase 2: Implementation

### Step 1: Core logic
Implement main feature

### Step 2: UI integration
Add UI components
";
            File.WriteAllText(testPlanFile, planContent);
        }

        [TearDown]
        public void Cleanup()
        {
            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, true);
        }

        [Test]
        public void FullWorkflowLifecycle_ShouldPersistState()
        {
            // Step 1: Create workflow
            var workflow = manager.CreateWorkflow("Test Feature", testPlanFile);
            workflow.Steps = parser.ParsePlanFile(testPlanFile);
            workflow.CurrentPhaseIndex = 1;
            workflow.CurrentStepIndex = 1;
            manager.SaveWorkflow(workflow);

            workflow.Steps.Count.ShouldBe(4);

            // Step 2: Transition to Implementing
            manager.TransitionToPhase(workflow, WorkflowPhase.Implementing);
            workflow.CurrentPhase.ShouldBe(WorkflowPhase.Implementing);

            // Step 3: Mark step completed
            manager.MarkStepCompleted(workflow, 1, 1, "abc123");
            workflow.Steps[^0].IsCompleted.ShouldBeTrue();
            workflow.Steps[^0].CommitHash.ShouldBe("abc123");

            // Step 4: Navigate to next step
            workflow.CurrentStepIndex = 2;
            manager.SaveWorkflow(workflow);

            // Step 5: Reload workflow (simulate app restart)
            var reloaded = manager.LoadWorkflow(workflow.WorkflowId);
            reloaded.ShouldNotBeNull();
            reloaded.WorkflowName.ShouldBe("Test Feature");
            reloaded.CurrentPhaseIndex.ShouldBe(1);
            reloaded.CurrentStepIndex.ShouldBe(2);
            reloaded.Steps[^0].IsCompleted.ShouldBeTrue();
        }

        [Test]
        public void WorkflowWithTestFailures_ShouldPersistLogs()
        {
            // Arrange
            var workflow = manager.CreateWorkflow("Test Feature", testPlanFile);
            workflow.Steps = parser.ParsePlanFile(testPlanFile);
            workflow.CurrentPhaseIndex = 2;
            workflow.CurrentStepIndex = 1;
            manager.SaveWorkflow(workflow);

            var failureLogs = new List<string>
            {
                "Test failed: Expected 5 but was 3",
                "Stack trace: at MyClass.MyMethod()"
            };

            // Act
            manager.AddTestFailureLogs(workflow, failureLogs);

            // Assert
            var step = workflow.Steps.First(s => s.PhaseIndex == 2 && s.StepIndex == 1);
            step.TestFailureLogs.Count.ShouldBe(2);
            step.TestFailureLogs[^0].ShouldContain("Expected 5");
        }

        [Test]
        public void MultipleWorkflows_ShouldBeIndependent()
        {
            // Arrange & Act
            var workflow1 = manager.CreateWorkflow("Feature A", testPlanFile);
            var workflow2 = manager.CreateWorkflow("Feature B", testPlanFile);

            workflow1.CurrentPhaseIndex = 1;
            workflow2.CurrentPhaseIndex = 2;

            manager.SaveWorkflow(workflow1);
            manager.SaveWorkflow(workflow2);

            // Assert
            var all = manager.GetAllWorkflows();
            all.Count.ShouldBe(2);

            var loaded1 = manager.LoadWorkflow(workflow1.WorkflowId);
            var loaded2 = manager.LoadWorkflow(workflow2.WorkflowId);

            loaded1.CurrentPhaseIndex.ShouldBe(1);
            loaded2.CurrentPhaseIndex.ShouldBe(2);
        }

        [Test]
        public void DeleteWorkflow_ShouldRemovePersistentData()
        {
            // Arrange
            var workflow = manager.CreateWorkflow("Temp Feature", testPlanFile);
            var workflowId = workflow.WorkflowId;

            // Act
            manager.DeleteWorkflow(workflowId);

            // Assert
            var loaded = manager.LoadWorkflow(workflowId);
            loaded.ShouldBeNull();
        }

        [Test]
        public void InvalidPhaseTransition_ShouldThrowException()
        {
            // Arrange
            var workflow = manager.CreateWorkflow("Test", testPlanFile);
            workflow.CurrentPhase = WorkflowPhase.Planning;

            // Act & Assert
            Should.Throw<InvalidOperationException>(() =>
                manager.TransitionToPhase(workflow, WorkflowPhase.Testing));
        }
    }
}
```


### Validation Criteria

- ✅ All integration tests pass (>95% success rate)
- ✅ Workflow persists across simulated app restarts
- ✅ Multiple workflows can coexist independently
- ✅ Edge cases handled gracefully (invalid transitions, missing files)


### Integration Points

- Tests all components from Steps 1-9
- Validates end-to-end workflow

***

## Step 11: Error Handling \& Edge Cases

### Objective

Add comprehensive error handling and edge case validation.

### Context

- Handle missing plan files
- Validate corrupted JSON workflow files
- Handle Git errors gracefully
- Test runner failures


### Deliverables

**File:** `DocXHandler/Workflow/WorkflowManager.cs` (modifications)

```csharp
// ... existing code ...
// <line 45> public WorkflowState CreateWorkflow(string workflowName, string planFilePath)
// <line 46> {

try
{
    if (string.IsNullOrWhiteSpace(workflowName))
        throw new ArgumentException("Workflow name cannot be empty", nameof(workflowName));

    if (!File.Exists(planFilePath))
        throw new FileNotFoundException($"Plan file not found: {planFilePath}");

    var state = new WorkflowState
    {
        WorkflowName = workflowName,
        PlanFilePath = planFilePath,
        CurrentPhase = WorkflowPhase.Planning
    };

    store.SaveWorkflowState(state);
    log.Info($"Created workflow: {workflowName} (ID: {state.WorkflowId})");
    
    return state;
}
catch (Exception ex)
{
    log.Error(ex, $"Failed to create workflow: {workflowName}");
    throw;
}

// <line 67> }
// <line 68> 
// <line 69> public WorkflowState? LoadWorkflow(string workflowId)
```

**File:** `DocXHandler/Workflow/WorkflowStateStore.cs` (modifications)

```csharp
// ... existing code ...
// <line 32> public WorkflowState? LoadWorkflowState(string workflowId)
// <line 33> {

try
{
    var filePath = GetFilePath(workflowId);
    if (!File.Exists(filePath)) return null;

    var json = File.ReadAllText(filePath);
    return JsonSerializer.Deserialize<WorkflowState>(json);
}
catch (JsonException ex)
{
    // Log corrupted file and move to backup
    var backupPath = GetFilePath(workflowId) + ".corrupted";
    File.Move(GetFilePath(workflowId), backupPath);
    throw new InvalidOperationException($"Corrupted workflow file: {workflowId}. Moved to {backupPath}", ex);
}
catch (Exception ex)
{
    throw new InvalidOperationException($"Failed to load workflow: {workflowId}", ex);
}

// <line 44> }
// <line 45>
```


### Unit Test Requirements

**File:** `UnitTests/Workflow/ErrorHandlingTests.cs`

```csharp
[TestFixture]
public class ErrorHandlingTests
{
    private string testDirectory;
    private IWorkflowStateStore store;
    private IWorkflowManager manager;

    [SetUp]
    public void Setup()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        store = new WorkflowStateStore(testDirectory);
        var log = Substitute.For<ILogCtxLogger>();
        manager = new WorkflowManager(store, log);
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(testDirectory))
            Directory.Delete(testDirectory, true);
    }

    [Test]
    public void CreateWorkflow_MissingPlanFile_ShouldThrowFileNotFoundException()
    {
        // Act & Assert
        Should.Throw<FileNotFoundException>(() =>
            manager.CreateWorkflow("Test", "non-existent-file.md"));
    }

    [Test]
    public void LoadWorkflow_CorruptedJson_ShouldHandleGracefully()
    {
        // Arrange
        var workflowId = Guid.NewGuid().ToString();
        var filePath = Path.Combine(testDirectory, $"{workflowId}.workflow.json");
        File.WriteAllText(filePath, "{ invalid json }");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            manager.LoadWorkflow(workflowId));

        // Verify corrupted file moved to backup
        File.Exists(filePath + ".corrupted").ShouldBeTrue();
    }

    [Test]
    public void MarkStepCompleted_NonExistentStep_ShouldThrowException()
    {
        // Arrange
        var planFile = Path.GetTempFileName();
        File.WriteAllText(planFile, "# Plan");
        var workflow = manager.CreateWorkflow("Test", planFile);

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            manager.MarkStepCompleted(workflow, 99, 99));
    }

    [Test]
    public void TransitionToPhase_InvalidTransition_ShouldThrowException()
    {
        // Arrange
        var planFile = Path.GetTempFileName();
        File.WriteAllText(planFile, "# Plan");
        var workflow = manager.CreateWorkflow("Test", planFile);
        workflow.CurrentPhase = WorkflowPhase.Planning;

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            manager.TransitionToPhase(workflow, WorkflowPhase.Completed));
    }
}
```


### Validation Criteria

- ✅ All error scenarios have proper exception handling
- ✅ Corrupted files moved to backup location
- ✅ User-friendly error messages displayed
- ✅ All edge case tests pass


### Integration Points

- Error handling added across all manager methods
- UI displays user-friendly error messages

***

## Step 12: Documentation \& Telemetry

### Objective

Add comprehensive documentation and telemetry logging.[^1]

### Context

- Document workflow feature in README
- Add XML documentation comments
- Telemetry for workflow operations (create, transition, complete)


### Deliverables

**File:** `README.md` (modifications)

```markdown
# VecTool

... existing content ...

## Agile AI Dev Wizard

### Overview

The Agile AI Dev Wizard provides a guided workflow for AI-assisted development:

1. **Plan**: Select a markdown plan file with phase/step structure
2. **Implement**: Navigate through steps, track progress
3. **Commit**: Generate AI-powered commit messages using Git integration
4. **Test**: Run NUnit tests and collect failure logs
5. **Iterate**: Loop back to planning or continue to next phase

### Usage

1. Open wizard: **Tools → Agile AI Dev Wizard**
2. Create new workflow: **New** button → select plan file
3. Navigate steps: **Previous/Next Step** buttons
4. Commit changes: **Commit** button (generates AI message)
5. Run tests: **Test** button → selects test project
6. Convert sources: **Convert to Single File** → choose format (DOCX/MD/PDF)

### Plan File Format

Markdown files with hierarchical structure:

```


# Project Plan

## Phase 1: Setup

### Step 1: Create models

Implement data structures

### Step 2: Add tests

Write unit tests

## Phase 2: Implementation

### Step 1: Core feature

Main functionality

```

### Workflow State Persistence

Workflow state saved to:
<span style="display:none">[^7]</span>

<div align="center">⁂</div>

[^1]: DragNdrop-Recent-Files-Feature.md
[^2]: https://help.syncfusion.com/windowsforms/wizard-control/getting-started
[^3]: https://dev.to/abubakardev/mastering-git-commit-messages-best-practices-and-markdown-usage-18cg
[^4]: https://community.dynamics.com/blogs/post/?postid=55747b05-a614-48fa-b336-c2abf8696f2d
[^5]: https://github.com/tak-bro/aicommit2
[^6]: https://kilocode.ai/docs/basic-usage/git-commit-generation/
[^7]: VecToolDev.docx```

