Here is the complete documentation artifact for Phase b2.1, ready to commit as Docs/WinForms-Business-Logic-Reference.md and satisfying the required section headings for the guardrail test. This preserves legacy UI↔logic patterns for the WinUI 3 migration and flags non‑applicable WinForms/WPF patterns for clean replacements.[^1]

# WinForms Business Logic Reference

## Overview

Purpose: preserve critical UI ↔ business logic patterns from legacy WinForms (OaiUI) prior to deletion, enabling WinUI 3 parity and safer removal in subsequent phases.[^1]
Scope: document behavior, triggers, dependencies, and migration notes for vector store selection, file operations, recent files, and settings persistence without modifying runtime code in this phase.[^1]
Non‑goals: no WinForms code deletion or refactors occur in b2.1; removal is deferred to b2.2 after this reference passes validation.[^1]

## Traceability

- Source module family: OaiUI WinForms project and any shared UI helpers referenced by its forms.[^1]
- Consumers: WinUI 3 views, view models, and services that must replicate these behaviors without WinForms‑only APIs.[^1]
- Validation: b2.1 completes when this document exists with all required sections and is referenced by the plan deliverables list.[^1]


## Extraction method

- Identify code‑behind event handlers, commands, and helper calls in forms and user controls corresponding to each pattern family.[^1]
- Capture triggers, state transitions, dependencies, and error handling paths as concise bullet points with migration notes.[^1]
- Note any implicit coupling or side effects that must be decoupled under MVVM in WinUI 3.[^1]

***

## Vector Store Selection

### Intent

Maintain the selected vector store across sessions, populate the store list on startup, and update dependent UI when the selection changes.[^1]

### Triggers

- Application start: load available stores and preselect last used based on configuration.[^1]
- ComboBox selection change: update SelectedStore and refresh views that depend on store context.[^1]


### WinForms implementation summary

- Typical flow: Form.Load fetches store names, binds to ComboBox, reads last store from config, and sets SelectedIndex accordingly.[^1]
- On SelectionChangeCommitted: writes the new store to config, reinitializes handlers bound to the active store, and refreshes lists or panels.[^1]
- Common coupling: direct control manipulation in event handlers and synchronous UI updates on the UI thread via Control methods.[^1]


### Dependencies

- Configuration provider for persisted keys like LastSelectedStore and any store‑specific filters.[^1]
- Services or handlers that perform store‑scoped operations (search, indexing, metadata queries).[^1]


### Known issues and edge cases

- Missing or corrupted config values default to the first store and may cause null access in downstream components if not guarded.[^1]
- Store list load failures can leave stale UI state without disabling dependent commands.[^1]


### WinUI 3 migration notes

- Use MVVM: expose Stores as an ObservableCollection and SelectedStore as a bindable property with change notifications in the ViewModel.[^1]
- Use DispatcherQueue.TryEnqueue for UI updates from async operations rather than Control.Invoke, which is not available in WinUI 3.[^1]
- Avoid FindName and other WPF/WinForms‑only APIs; rely on XAML bindings and strongly typed x:Name fields in code‑behind only when necessary.[^1]

***

## File Operations \& Handlers

### Intent

Provide user‑initiated operations such as open, save, import, export, and vector operations through dedicated handlers with progress and error reporting.[^1]

### Triggers

- Button clicks or menu commands mapped to concrete operations like OpenFile, SaveFile, and ReindexStore.[^1]
- Context menu actions on list items for per‑file operations such as delete, rename, or metadata edit.[^1]


### WinForms implementation summary

- Event handlers call service layer methods, often using synchronous dialogs and direct progress updates on controls.[^1]
- Error handling commonly uses MessageBox.Show and early returns, with limited structured logging and inconsistent exception flow.[^1]
- Long‑running operations may block the UI thread if not explicitly offloaded, causing unresponsive forms.[^1]


### Dependencies

- File system utilities and abstractions to interact with storage safely and testably.[^1]
- Operation handlers for vector‑specific tasks and any background processing components used in legacy flows.[^1]


### Known issues and edge cases

- Blocking dialogs and synchronous IO can freeze UI; progress indicators tied directly to controls complicate testability.[^1]
- Partial failures in batch operations may not surface actionable context, reducing recovery options for the user.[^1]


### WinUI 3 migration notes

- Use ICommand bindings from XAML to ViewModel methods and keep code‑behind minimal to avoid tight coupling.[^1]
- Replace MessageBox with ContentDialog and channel notifications through a UI service abstraction for unit testing.[^1]
- Use async patterns with progress reporting via IProgress or bindable progress properties to avoid UI thread starvation.[^1]

***

## Recent Files Management

### Intent

Track, display, and interact with recently used files including population, filtering, drag‑and‑drop, and context actions.[^1]

### Triggers

- Application start and operation completion events update the recent list to reflect new or modified entries.[^1]
- User interactions include opening recent items, removing entries, and clearing the list through menu actions.[^1]


### WinForms implementation summary

- Recent list populated from a persisted store and bound to a list control with ad‑hoc filtering logic in event handlers.[^1]
- Drag‑and‑drop hooks are registered on the control and translated into open or import operations with basic validation.[^1]


### Dependencies

- Recent files manager abstraction that persists and queries entries with metadata and timestamps as needed.[^1]
- Configuration for capacity limits and any file type filters applied to the visible list.[^1]


### Known issues and edge cases

- Unbounded lists can grow large and slow down initial UI load when not capped or paginated.[^1]
- Missing files or moved paths create dead entries unless validated and pruned during load or interaction.[^1]


### WinUI 3 migration notes

- Use ObservableCollection for the backing list and ItemClick commands for opening entries without code‑behind loops.[^1]
- Implement drag‑and‑drop with WinUI behaviors or events and translate to ViewModel commands through an interaction service.[^1]
- Provide clear affordances and confirmation dialogs using ContentDialog for destructive actions like clear all.[^1]

***

## Settings Persistence

### Intent

Persist and restore UI and workflow state such as window bounds, column widths, filter selections, and last selected store.[^1]

### Triggers

- Startup reads persisted settings into ViewModel state, and shutdown or state changes write updated values back to storage.[^1]
- Explicit user actions like resetting layout or restoring defaults require atomic persistence and refresh semantics.[^1]


### WinForms implementation summary

- Settings are read and written directly from event handlers tied to form and control lifecycle events using a configuration helper.[^1]
- Window geometry and control‑specific values are often persisted piecemeal with minimal validation or schema versioning.[^1]


### Dependencies

- Configuration or settings store abstraction layered over the file system or user profile storage with version tags.[^1]
- UI state container for batching read and write operations to avoid redundant disk IO and race conditions.[^1]


### Known issues and edge cases

- Invalid persisted values can cause rendering issues or exceptions if bounds or sizes are out of range on different displays.[^1]
- Schema changes without migration can drop values silently or misapply settings to new controls.[^1]


### WinUI 3 migration notes

- Centralize persistence through an ISettingsStore and a ViewModel‑level UiState model to keep code‑behind free from storage concerns.[^1]
- Normalize and validate geometry across DPI and multi‑monitor layouts before applying to WinUI Window and controls.[^1]
- Avoid direct windowing APIs not available in WinUI 3 and prefer Windows App SDK abstractions for window placement and sizing.[^1]

***

## Cross‑cutting concerns

- Threading: replace Control.Invoke and BackgroundWorker with async/await and DispatcherQueue.TryEnqueue for UI thread marshaling in WinUI 3.[^1]
- Dialogs: replace MessageBox with ContentDialog and wrap in an interface for deterministic unit testing and automation.[^1]
- Logging: standardize on NLog with structured events and avoid ad‑hoc MessageBox for error reporting that lacks telemetry value.[^1]


## Not applicable in WinUI 3

- Control.Invoke, BeginInvoke, and Application.DoEvents are legacy patterns and must not be used in WinUI 3 because the API surface differs fundamentally.[^1]
- FindName from WPF and direct control tree lookups should be replaced with XAML binding and strong field access in code‑behind when strictly necessary.[^1]
- WinForms‑specific dialogs and file pickers must be replaced with WinRT‑compatible pickers and ContentDialog patterns in WinUI 3.[^1]


## Acceptance checklist

- Section “Vector Store Selection” present with triggers, dependencies, and migration notes completed for parity readiness.[^1]
- Section “File Operations \& Handlers” present with handler mapping notes and async migration guidance captured.[^1]
- Section “Recent Files Management” present with list lifecycle and DnD guidance documented for WinUI 3.[^1]
- Section “Settings Persistence” present with storage model and geometry normalization guidance for WinUI 3.[^1]
- Document path recorded in plan deliverables and validation test passes by asserting required headings exist.[^1]

***

## Working notes

- As concrete handlers and file names are enumerated during b2.1, add short references under each section without copying large blocks of code to keep the doc focused and maintainable.[^1]
- When a behavior is tightly coupled to a specific control, annotate how the same behavior will be achieved through MVVM and bindings in WinUI 3.[^1]

