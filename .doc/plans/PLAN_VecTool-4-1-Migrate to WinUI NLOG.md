# 🎯 VecTool WinUI 3 Migration - Plan 1.0

**Phases:** 1 of 4 | **Progress:** 75%
**Status:** Active
**Confidence Rating:** 9/10 🔥

***

## Quick Reference

| Attribute | Value |
| :-- | :-- |
| Plan Version | 4.1 |
| Parent Plan | - (Master Plan) |
| Total Phases | 4 |
| Current Phase | 4.1.3 |
| Phase Progress | Phase 3 of 4  |
| App Version Mapping | 4.1.p3 |
| Last Updated | 2025-10-12 |
| Next Phase | 1.3  |


***

## Phase Roadmap

- 🔄 **Phase 1.1:** Master Plan \& Architecture
- ⏳ **Phase 1.2:** Core UI Implementation \& Logging
- ⏳ **Phase 1.3:** Parity Testing \& CI/CD (Current)
- ⏳ **Phase 1.4:** Rollout \& Decommission

***

## Objective

A four-week, zero-behavior-change migration will move the current WinForms UI to a side-by-side WinUI 3 app, enforce exclusive NLog + Seq logging, and ship after parity gates and telemetry confirm no UX or logging drift.[^2]

Scope stays strictly "like-for-like": same flows, dialogs, shortcuts, persistence, and About/version info, with the UI tech swapped to WinUI 3 and all UI logging standardized to NLog message templates.[^2]

## **Non-Goals**

- ❌ Do **NOT** touch WinForms app (`OaiUI`) - it is deprecated[^1]
***

## Ground Rules

**UX Preservation:**

- Preserve all UX flows, menu structure, keyboard accelerators, file dialogs, drag-and-drop, status/progress, recent files, and About/version content without re-organization or new features.[^2]

**Logging Standardization:**

- Enforce exclusive NLog with Seq target across the UI surface; remove LogCtx/Serilog from UI code and use message-template structured logging with async/buffering configuration.[^2]

**Domain Contracts:**

- Keep UI-thread behavior and domain orchestration the same by maintaining all domain contracts and surface-level event timing, only changing the UI framework.[^2]

***

## Phase 1.0: Architecture \& Foundation

### Step 1: Project Setup

**Create WinUI 3 Project:**

- Add a new WinUI 3 project side-by-side, e.g., Vectool.UI.WinUI, targeting .NET 8 and Windows App SDK to enable parallel development and A/B parity testing.[^2]
- Start unpackaged for parity and local runs; consider MSIX only after parity and CI readiness if packaging is part of the release flow.[^2]

**Version Alignment:**

- Copy over assembly metadata and version mapping so About dialog and diagnostics show the same AssemblyVersion, FileVersion, InformationalVersion, and display labels.[^2]
- Map Plan 1.0 → App Version 2.1.p1 per GUIDE-1.2 integration standards.[^1]

**Success Criteria:**

- WinUI 3 project compiles and launches empty window
- Assembly versions match existing WinForms app
- VersionConsistencyTests pass for new project

***

### Step 2: UI Mapping Strategy

**Control Translation Matrix:**

- Map WinForms MainForm → WinUI Window/Page, AboutForm → ContentDialog/Page, StatusStrip → StatusBar/InfoBar equivalents, and keep control names, labels, and persisted state semantics intact.[^2]
- Recreate Recent Files as a UserControl via ListView/GridView with the same columns, filters, and persisted widths/heights, keeping the same config keys and file formats.[^2]
- Port drag-and-drop, including outbound drags and file-type filters, ensuring the same refresh points and event sequencing.[^2]

**Threading Model:**

- Replace Control.Invoke/BeginInvoke with DispatcherQueue.TryEnqueue at the same call sites to preserve timing and the perceived responsiveness.[^2]
- For file/folder pickers and modal dialogs, set XamlRoot to the current Window/Page to maintain modality and ownership parity.[^2]
- Keep long-running operations cancellable and progress semantics identical by adapting the existing progress publisher to WinUI controls without changing event signatures.[^2]

**Success Criteria:**

- Complete mapping document for all WinForms controls
- Thread marshaling strategy documented
- Dialog ownership pattern defined

***

### Step 3: NLog Bootstrap \& Configuration

**Logging Infrastructure:**

- Remove UI references to LogCtx and Serilog, and acquire a per-class NLog logger via LogManager; encode structured properties via message templates or LogEventInfo.Properties for queryable fields in Seq.[^2]
- Initialize NLog once at startup with a safe bootstrap that never throws, uses async/buffering for Seq, and falls back to console/file if config is missing.[^2]
- Bring forward NLog.config (Seq + console targets, buffering wrappers, machine/thread/environment enrichers) colocated with the WinUI app so the bootstrap can pick it up.[^2]

**Bootstrap Implementation:**

```csharp
// ✅ FULL FILE VERSION.
// Required Imports Template
using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging

using NLog.Config;
using NLog.Targets;

internal static class NLogBootstrap
{
    private static bool _initialized;

    public static void Init()
    {
        if (_initialized) return;

        try
        {
            LogManager.Setup().LoadConfigurationFromFile("NLog.config");
        }
        catch
        {
            var cfg = new LoggingConfiguration();

            var console = new ConsoleTarget("console");
            cfg.AddRuleForAllLevels(console);

            var seq = new SeqTarget("seq")
            {
                ServerUrl = "http://localhost:5341",
            };

            // Buffering wrapper to avoid UI stalls
            var buffer = new BufferingTargetWrapper(seq, 1000, 2000);
            cfg.AddRule(LogLevel.Info, LogLevel.Fatal, buffer);

            LogManager.Configuration = cfg;
        }

        _initialized = true;
    }
}
```

**Message Template Patterns:**

```csharp
// ✅ FULL FILE VERSION.
// Required Imports Template
using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging

namespace VecTool.UI.Logging
{
    public static class UiLogPatterns
    {
        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public static void ReportExport(string rootPath, int count)
        {
            Log.Info("Export started for {RootPath} with {Count} items", rootPath, count);
        }

        public static void ReportWarning(string fileName, string reason)
        {
            var evt = new LogEventInfo(LogLevel.Warning, Log.Name, "Excluded file encountered");
            evt.Properties["FileName"] = fileName;
            evt.Properties["Reason"] = reason;
            Log.Log(evt);
        }
    }
}
```

**Success Criteria:**

- NLog bootstrap initializes without exceptions
- Seq target receives structured log events
- Console fallback works when Seq unavailable
- Message templates replace all string interpolation

***

### Step 4: Configuration \& Assets Parity

**Settings Migration:**

- Preserve all app settings keys and defaults (e.g., vector store folders, excluded files/folders) so behavior remains unchanged in config-driven flows.[^2]
- Wire the existing IVersionProvider to the WinUI About surface to display identical ApplicationName, AssemblyVersion, FileVersion, InformationalVersion, CommitShort, and BuildTimestampUtc.[^2]
- Keep icons/images and their locations consistent; apply the same About labels and formats used today.[^2]

**Version Provider Integration:**

```csharp
public class PlanVersionProvider : IVersionProvider
{
    public string PlanVersion => "1.0";
    public string MasterPlan => "1.0";
    public string InformationalVersion => $"2.1.p{PlanVersion}";
}
```

**Success Criteria:**

- All config keys loaded identically
- About dialog shows matching version info
- Asset paths resolve correctly

***

## Phase 1.1 Preview: Core UI Implementation

**Scope:**

- Implement MainForm → Window/Page conversion
- Build Recent Files UserControl with DnD
- Create StatusBar/InfoBar equivalents
- Wire menu structure and keyboard accelerators

**Deliverables:**

- Functional main window with full menu structure
- Recent files list with drag-and-drop
- Status/progress indicators
- All existing keyboard shortcuts working

***

## Phase 1.2 Preview: Parity Testing \& CI/CD

**Scope:**

- Define explicit parity gates: layout equivalence, menus/accelerators, file dialogs, DnD flows, recent files behavior, status/progress updates, and About/version content.[^2]
- Add automation smoke checks for WinUI: window creation, menu actions, and dialog invocations in the same sequences as before, while keeping existing headless tests green.[^2]
- Run A/B sessions with the same inputs and validate identical outputs and log events, including property names and levels in Seq.[^2]

**CI/CD Updates:**

- Add Windows App SDK workloads to CI, build the new WinUI app unpackaged first, and maintain Source Link/versioning checks and reproducible builds already enforced by VersionConsistencyTests.[^2]
- Publish artifacts with symbols and validate that Seq continues receiving the same event names and property vocabulary for dashboards and alerts.[^2]
- Keep both UIs building for one cycle while telemetry compares error rates and performance against the baseline.[^2]

***

## Phase 1.3 Preview: Rollout \& Decommission

**Rollout Strategy:**

- Ship the WinUI 3 app behind a toggle or as a separate executable to internal users for parity validation before switching defaults.[^2]
- Maintain a rollback plan by keeping the WinForms artifacts for one additional cycle after default switch.[^2]
- Switch defaults only after parity gates hold steady and Seq telemetry confirms stable error/perf profiles.[^2]

**Cleanup:**

- Remove LogCtx usages in UI (e.g., RecentFilesPanel DnD, UI-only Serilog adapters, and any LogCtx folders/config).[^2]
- Decommission WinForms projects after stable rollout period
- Archive migration documentation

***

## Concrete UI Mapping Examples

### Example 1: Main Form Conversion

**WinForms → WinUI:**

- MainForm → Window with a Page and MenuBar or NavigationView, preserving menu text, accelerators, and handlers.[^2]
- StatusStrip (ToolStripStatusLabel + ToolStripProgressBar) → InfoBar/StatusBar with TextBlock + ProgressBar, with identical progress semantics and labels.[^2]
- AboutForm → ContentDialog/Page bound to IVersionProvider to keep identical labels and value formats.[^2]

***

### Example 2: Structured Logging Replacements

**LogCtx → NLog:**

- Replace LogCtx.With(component, area, count) with NLog message templates: logger.Info("Processed {Count} items in {Area}", count, area).[^2]
- Use LogEventInfo.Properties for extra fields needed in Seq dashboards when not part of the main message template.[^2]
- Ensure exceptions are logged with properties at the same call sites to preserve sequence and severity for A/B telemetry.[^2]

***

### Example 3: Parity Smoke Tests

**NUnit + Shouldly Pattern:**

```csharp
// ✅ FULL FILE VERSION.
// Required Imports Template
using NUnit.Framework;
using Shouldly;
using System;
using NLog; // NLog is mandatory for structured logging

namespace VecTool.WinUI.Tests
{
    [TestFixture]
    public sealed class SmokeTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            NLogBootstrap.Init();
        }

        [Test]
        public void Should_Start_MainWindow_Without_Errors()
        {
            // Placeholder: create app/window via UITest harness or App SDK host
            true.ShouldBeTrue("WinUI harness should start the main window without exceptions.");
        }
    }
}
```

**Test Coverage:**

- Launch main window and assert menu items exist and are enabled with the same names.[^2]
- Invoke "Convert to MD" via command binding and assert output file presence and identical naming pattern.[^2]
- Open About surface and assert labels/values match Assembly/File/Informational/CommitShort/BuildTimestampUtc.[^2]

***

## Risks \& Mitigations

| Risk | Impact | Mitigation |
| :-- | :-- | :-- |
| Drag-and-drop fidelity varies across desktops | High | Targeted exploratory testing on same datasets/paths used today [^2] |
| UI-thread marshaling changes introduce timing diffs | Medium | Centralize DispatcherQueue usage and keep raise points identical [^2] |
| Packaging changes affect file system access | Medium | Start unpackaged and gate MSIX post-parity [^2] |
| NLog config missing at runtime | Low | Safe bootstrap with console/file fallback [^2] |
| Version drift between plans and assemblies | Low | VersionConsistencyTests enforce alignment [^1] |


***

## Rough Timeline

| Week | Focus | Deliverables |
| :-- | :-- | :-- |
| Week 1 | Inventory and freeze, create WinUI shell, NLog bootstrap and Seq connectivity, About/version parity [^2] | Empty WinUI app, NLog configured, version parity |
| Week 2 | Map main window, status/progress, recent files, DnD, dialogs; wire commands and persistence unchanged [^2] | Functional UI with all controls mapped |
| Week 3 | Parity tests, automation smoke, A/B telemetry via Seq, perf check, packaging decision, CI updates [^2] | Passing parity gates, CI pipeline updated |
| Week 4 | Pilot rollout, fix parity gaps, switch default, decommission old UI logging and LogCtx references [^2] | Production-ready WinUI app |


***

## What to Remove vs Add

**Remove:**

- LogCtx usages in UI (e.g., RecentFilesPanel DnD, UI-only Serilog adapters, and any LogCtx folders/config)[^2]

**Add:**

- NLog.config with Seq + async buffering, a safe bootstrap, and unified logger acquisition in UI classes[^2]

**Keep Unchanged:**

- Domain interfaces and contracts (IUserInterface, recent files services, progress manager, config keys, and file formats)[^2]

***