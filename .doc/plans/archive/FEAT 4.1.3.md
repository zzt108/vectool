Based on the plan and implementation status, here's **Plan 1.3 for Phase 3** of the WinUI 3 migration:

# Plan 1.3: Phase 3 – Testing, Parity Gates \& Rollout 🚀

**Duration:** Week 3-4 (targeting completion by 2025-10-26)
**Status:** Phase 2 ~70% complete; critical gaps in About, Settings persistence, DnD, CI/CD[^1][^2]

***

## Ground Rules

- **Zero new features** — fix Phase 2 gaps, validate parity, ship behind toggle[^1]
- **Parity gates mandatory** — layout equivalence, menu/accelerators, dialogs, DnD, Recent Files, status/progress, About/version content[^1]
- **A/B telemetry via Seq** — compare error rates, event sequences, property vocabulary between WinForms and WinUI[^1]
- **Keep both UIs building** — maintain WinForms for rollback; switch default only after gates pass[^1]
- **Smoke + integration tests** — NUnit/Shouldly for WinUI window creation, menu actions, handler invocations[^1]

***

## Phase 3 Objectives

### 3.1 Complete Phase 2 Gaps (Critical Blockers)

#### About Dialog with IVersionProvider

**Gap:** `AboutMenuClick` shows dummy `ContentDialog`; `AboutPage` + `AboutVersionAdapter` exist but not wired[^2]

**Tasks:**

1. Wire `IVersionProvider` (via `AssemblyVersionProvider`) to `AboutPage.xaml` data binding
2. Replace `AboutMenuClick` dummy dialog with navigation to `AboutPage` or inline data-bound `ContentDialog`
3. Replicate WinForms labels: **ApplicationName**, **AssemblyVersion**, **FileVersion**, **InformationalVersion**, **CommitShort**, **BuildTimestampUtc** (format: `Build {yyyy-MM-dd HH:mm} UTC`)[^1]
4. Add smoke test: `AboutDialogShouldShowVersionFields` asserting all label values match `IVersionProvider` output

**Acceptance:**

- `AboutPage` displays identical fields to WinForms `AboutForm.cs` (7 labels total)[^2]
- Smoke test passes with Shouldly assertions on version strings
- NLog logs `AboutPage` display event with structured properties

***

#### Settings Tab Persistence

**Gap:** `BtnSaveVsSettingsClick` and `BtnResetVsSettingsClick` are stubs[^2]

**Tasks:**

1. Implement `BtnSaveVsSettingsClick`:
    - Read `CmbSettingsVectorStore.SelectedItem` to get store name
    - Parse `TxtExcludedFiles.Text` and `TxtExcludedFolders.Text` (newline-separated)
    - Call `uiState.SetVectorStoreConfig(storeName, new VectorStoreConfig { UseCustomExcludedFiles = !ChkInheritExcludedFiles.IsChecked, CustomExcludedFiles = parsedFiles, ... })`
    - Log save event with store name + field counts
2. Implement `BtnResetVsSettingsClick`:
    - Clear custom flags, reload global defaults from `App.config`
    - Refresh UI controls (`ChkInheritExcludedFiles.IsChecked = true`, clear textboxes)
3. Add smoke test: `SettingsShouldPersistAndReload` — create config, save, reload UI, assert values match

**Acceptance:**

- Save/Reset buttons functional with NLog-tracked events
- Per-store exclusion patterns persist across app restarts via `UiStateConfig.json`
- Parity with WinForms settings panel behavior (checkboxes + textboxes)[^2]

***

#### Recent Files Drag-and-Drop

**Gap:** `RecentFilesPage` has Open/Delete buttons but **no DnD handlers**[^2]

**Tasks:**

1. Add `AllowDrop="True"` to `RecentFilesPage` XAML root element
2. Implement `DragOver` event: validate dragged file types (`.cs`, `.md`, etc.), set `e.AcceptedOperation = DataPackageOperation.Copy`
3. Implement `Drop` event: extract file paths from `e.DataView.GetStorageItemsAsync()`, register with `IRecentFilesManager.Add()`
4. Add outbound drag: wire `DragItemsStarting` on DataGrid to populate `DataPackage` with selected file paths
5. Add smoke test: `RecentFilesDragDropShouldRegisterFiles` — simulate drop, assert grid refresh + file count increase

**Acceptance:**

- Drag external files onto Recent Files tab → files appear in grid
- Drag files from grid → valid `DataPackage` with file paths (testable via UI automation harness)
- Parity with WinForms `RecentFilesPanel` DnD behavior[^2]

***

### 3.2 Parity Testing (Automated Smoke Suite)

#### Extend `MainWindowSmokeTests`

**Current:** Only checks menu/control **existence**, not enabled state or text parity[^2]

**Tasks:**

1. **Menu parity test:**

```csharp
[Test]
public void MenuItemsShouldMatchWinFormsText()
{
    var win = new MainWindow();
    var convertMenu = FindElementByName<MenuFlyoutItem>(win, "ConvertToMdMenu");
    convertMenu.Text.ShouldBe("Convert to MD");
    // Repeat for all menu items vs WinForms MainForm.Designer.cs
}
```

2. **Handler invocation test:**

```csharp
[Test]
public async Task ConvertToMdShouldProduceOutputFile()
{
    var win = new MainWindow();
    // Inject test folders + mock IUserInterface
    var handler = new MDHandler(mockUI, mockRecentFiles);
    handler.ExportSelectedFolders(testFolders, testOutputPath, testConfig);
    File.Exists(testOutputPath).ShouldBeTrue();
    // Assert file naming pattern matches WinForms output
}
```

3. **About version parity:**

```csharp
[Test]
public void AboutPageShouldShowIdenticalVersions()
{
    var provider = new AssemblyVersionProvider();
    var aboutPage = new AboutPage { DataContext = new AboutVersionAdapter(provider) };
    // Assert bindings match WinForms AboutForm label values
}
```


**Acceptance:**

- All smoke tests pass in CI (GitHub Actions or Azure Pipelines)
- NLog logs test execution with structured properties (test name, duration)
- Coverage: menu text, handler outputs, About labels, status bar updates

***

### 3.3 A/B Telemetry \& Event Sequencing

**Goal:** Compare WinForms vs WinUI behavior side-by-side using Seq dashboards[^1]

**Tasks:**

1. **Instrument event logging:**
    - Add `UiLogPatterns.ReportMenuAction(menuName, timestamp)` at all menu click handlers
    - Add `UiLogPatterns.ReportDialogOpen/Close(dialogName, durationMs)`
    - Add `UiLogPatterns.ReportHandlerExecution(handlerName, folderCount, outputPath, durationMs)`
2. **Run parallel sessions:**
    - Launch WinForms app → run "Convert to MD" → capture Seq events
    - Launch WinUI app → run "Convert to MD" with **identical inputs** → capture Seq events
    - Compare: event names, property keys (`{FolderCount}`, `{OutputPath}`), log levels, timestamps
3. **Create Seq dashboard:**
    - Chart: "Menu Actions by UI Framework" (group by `UiFramework` property: `WinForms` vs `WinUI`)
    - Chart: "Handler Execution Time Distribution" (histogram of `DurationMs` by handler)
    - Alert: "Missing Expected Events" (e.g., WinUI missing `ReportHandlerExecution` that WinForms emits)

**Acceptance:**

- Seq dashboard shows **<5% variance** in event counts and timing between WinForms/WinUI for identical workflows
- All WinForms event names/properties have WinUI equivalents (no vocabulary drift)
- NLog structured logging validated (no string interpolation, all message templates)[^1]

***

### 3.4 CI/CD Integration

**Gap:** No `.github/workflows` or `azure-pipelines.yml` visible; Windows App SDK workloads not configured[^2]

**Tasks:**

1. **Add GitHub Actions workflow** (`.github/workflows/winui-build.yml`):

```yaml
name: WinUI Build
on: [push, pull_request]
jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Install Windows App SDK workload
        run: dotnet workload install windows
      - name: Restore dependencies
        run: dotnet restore VecTool.sln
      - name: Build WinUI project
        run: dotnet build src/UI/VecTool.UI.WinUI/VecTool.UI.WinUI.csproj --configuration Release
      - name: Run smoke tests
        run: dotnet test tests/VecTool.WinUI.Tests/VecTool.WinUI.Tests.csproj --logger "console;verbosity=detailed"
      - name: Publish artifacts
        run: dotnet publish src/UI/VecTool.UI.WinUI/VecTool.UI.WinUI.csproj -c Release -r win-x64 --self-contained -o artifacts/winui
      - uses: actions/upload-artifact@v4
        with:
          name: VecTool-WinUI
          path: artifacts/winui
```

2. **Validate SourceLink + symbols:**

```
- Ensure `Directory.Build.props` includes `<DebugType>embedded</DebugType>` + `<EmbedUntrackedSources>true</EmbedUntrackedSources>`
```

    - Run `VersionConsistencyTests` in CI to catch version drift[^1]
3. **Add Seq validation step:**
    - CI job sends test log to Seq staging instance
    - Assert expected event count via Seq API query (`POST /api/events/signal?filter=@Level='Information' AND Component='WinUI'`)

**Acceptance:**

- CI builds WinUI project + runs all NUnit tests without errors
- Publish artifacts include `.pdb` files + reproducible build metadata
- Seq receives structured logs from CI test runs (validated via dashboard)

***

### 3.5 Rollout Strategy \& Toggle Mechanism

**Goal:** Ship WinUI behind toggle, maintain WinForms for rollback[^1]

**Tasks:**

1. **Add launch toggle** (environment variable or command-line arg):

```csharp
// In VecTool launcher (e.g., Program.cs or shell script)
var useWinUi = Environment.GetEnvironmentVariable("VECTOOL_USE_WINUI") == "1";
if (useWinUi)
{
    var winuiApp = new VecTool.UI.WinUI.App();
    winuiApp.Start();
}
else
{
    Application.EnableVisualStyles();
    Application.Run(new VecTool.OaiUI.MainForm());
}
```

2. **Pilot rollout plan:**
    - **Week 1:** Internal testing with `VECTOOL_USE_WINUI=1` (5 users, report Seq events daily)
    - **Week 2:** Staged rollout to 25% of users (A/B test via telemetry)
    - **Week 3:** Full rollout if parity gates pass; deprecate WinForms launch by Week 4
3. **Rollback mechanism:**
    - Keep `VecTool.OaiUI.csproj` buildable in solution for 1 additional release cycle
    - Document revert command: `set VECTOOL_USE_WINUI=0`

**Acceptance:**

- WinUI app ships as `VecTool.UI.WinUI.exe` alongside `VecTool.OaiUI.exe`
- Toggle tested in CI (build + test both binaries in same workflow)
- Seq dashboard tracks adoption rate (`UiFramework=WinUI` vs `WinForms` event counts)

***

## Deliverables Checklist

| Deliverable | Owner | Due Date | Status |
| :-- | :-- | :-- | :-- |
| About dialog with `IVersionProvider` | Dev | 2025-10-14 | ❌ TODO |
| Settings Save/Reset implementation | Dev | 2025-10-14 | ❌ TODO |
| Recent Files DnD (inbound + outbound) | Dev | 2025-10-15 | ❌ TODO |
| Extended smoke tests (menu, handlers, About) | QA | 2025-10-16 | ❌ TODO |
| A/B telemetry dashboard in Seq | DevOps | 2025-10-17 | ❌ TODO |
| CI/CD workflow with Windows App SDK | DevOps | 2025-10-18 | ❌ TODO |
| Rollout toggle + pilot plan | PM | 2025-10-19 | ❌ TODO |
| Parity gates validation (all green) | QA | 2025-10-22 | ❌ TODO |
| Production rollout (staged 25% → 100%) | PM | 2025-10-26 | ❌ TODO |


***

## Risk Mitigations

| Risk | Mitigation |
| :-- | :-- |
| **DnD fidelity differs across Windows versions** | Test on Windows 10 19041, 11 22H2, 11 23H2; log OS version in Seq events[^1] |
| **Seq downtime breaks logging** | NLog async buffer + file fallback already configured; smoke test validates graceful degradation[^2] |
| **CI Windows App SDK install fails** | Pin workload version (`dotnet workload install windows --version 8.0.100`), cache in GitHub Actions |
| **Parity gates fail in pilot** | Rollback via toggle; defer default switch until variance <5%[^1] |


***

## Success Criteria

✅ **About dialog shows all 7 version fields** matching WinForms `AboutForm`
✅ **Settings Save/Reset persist per-store config** via `UiStateConfig.json`
✅ **Recent Files DnD functional** (inbound + outbound) with smoke test coverage
✅ **Smoke tests pass in CI** (menu text, handler outputs, About labels, status updates)
✅ **Seq dashboard shows <5% variance** in event counts/timing WinForms vs WinUI
✅ **CI builds + publishes WinUI artifacts** with symbols + SourceLink
✅ **Rollout toggle tested** (both UIs launch and log to Seq)
✅ **Parity gates validated** in pilot (identical outputs for identical inputs)

***
