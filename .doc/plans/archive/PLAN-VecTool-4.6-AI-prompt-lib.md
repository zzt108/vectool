-prefer simple solutions

# **PLAN-VECTOOL-4.6-prompts-library-ai-git-integration.md**

**Plan Version:** 4.6
**Status:** 🎯 Planning
**Duration Estimate:** ~40-50 hours total (4.6.1 + 4.6.2)
**Last Updated:** 2025-11-08

***

## 🎯 Objective

Integrate AI-assisted prompt library management into VecTool with Git workflow automation:

- Browse/search/manage prompts from repo with 3-level hierarchy (Area → Project → Category)
- AI-generated commit messages for Git repos (prompts + vector store)
- Simple, clean architecture (no bloat)

***

## 🏗️ Architecture Overview

```
┌─ VecTool ─────────────────────────────────────┐
│                                                 │
│  PromptsBrowserPanel                           │
│  ├─ TreeView (Area/Project hierarchy)          │
│  ├─ ListView (full-text search results)        │
│  └─ Buttons: Copy, Edit, New Version, Git      │
│                                                 │
│  ↓↓↓ (data layer)                             │
│                                                 │
│  PromptSearchEngine                            │
│  └─ Full-text indexing + search                │
│                                                 │
│  LLMService (Factory pattern)                  │
│  ├─ ILLMProvider (abstraction)                 │
│  ├─ PerplexityProvider (MVP)                   │
│  ├─ OpenAIProvider (optional)                  │
│  └─ ... DeepSeek, Anthropic (optional)        │
│                                                 │
│  Services:                                      │
│  ├─ PromptCategorizer (AI: area/project/cat)  │
│  ├─ GitCommitMessageGenerator (AI: commit msg) │
│  ├─ PromptTemplateGenerator (var substitution) │
│  └─ FavoritesManager (JSON persistence)       │
│                                                 │
└─────────────────────────────────────────────────┘
```


***

## 📋 Phase 4.6.1: Infrastructure Layer (12 hours)

**Objective:** Foundation – no UI yet, just plumbing

### Step 1.1: Configuration (JSON-based)

**Files to create:**

- `VecTool.Core/Config/PromptsConfig.cs`
- `VecTool.Core/Config/LLMProviderConfig.cs`
- `app.config` entries

**Config structure (app.config):**

```xml
<add key="promptsRepositoryPath" value="C:\repos\AI-prompts" />
<add key="promptsFileExtensions" value=".md,.txt,.yaml,.json" />
<add key="llmProviderConfig" 
     value="C:\repos\AI-prompts\config\llm-config.json" />
<add key="favoritesConfigPath" 
     value="C:\repos\AI-prompts\config\favorites.json" />
```

**Simplicity decision:** Single JSON file for all LLM providers (not multiple files).

***

### Step 1.2: Domain Models

**Files to create:**

- `VecTool.Core/Models/PromptMetadata.cs` – File + metadata parsing
- `VecTool.Core/Models/PromptFile.cs` – Simple file wrapper

**Key simplifications:**

- No fancy enum-based prefixes – just string matching (`PROMPT*`, `GUIDE*`, `SPACE*`)
- No separate version history table – versions are in filename + Git history
- Flat hierarchy parsing: `area/project/category/PROMPT-version-name.md`

***

### Step 1.3: LLM Provider Abstraction (Simple)

**Files to create:**

- `VecTool.Core/AI/ILLMProvider.cs` – Simple interface
- `VecTool.Core/AI/LLMProviderFactory.cs` – Factory pattern
- `VecTool.Core/AI/Providers/PerplexityProvider.cs` – MVP only
- `VecTool.Core/AI/Providers/OpenAIProvider.cs` – Stub (optional)

**ILLMProvider interface (simple = 3 methods):**

```
RequestAsync(prompt: string): Task<string>
ValidateConfigAsync(): Task<bool>
GetProviderName(): string
```

**Simplicity decision:**

- No streaming (complexity overhead)
- No retry logic yet (start simple, add if needed)
- Single model per provider (configurable, not auto-detected)

***

### Step 1.4: Search Engine (In-memory, no Lucene)

**Files to create:**

- `VecTool.Core/Services/PromptSearchEngine.cs`

**Simplicity decision:**

- In-memory dictionary + LINQ queries
- Full-text: filename + description + first 2000 chars of content
- No ranking/scoring yet (just boolean match)
- RebuildIndex() called on app startup + FileSystemWatcher events

***

### Step 1.5: LLM Service Layer (Core)

**Files to create:**

- `VecTool.Core/Services/PromptCategorizer.cs` – AI: suggest area/project/category
- `VecTool.Core/Services/GitCommitMessageGenerator.cs` – AI: generate commit messages
- `VecTool.Core/Services/PromptTemplateGenerator.cs` – Template var substitution

**🔑 Key Feature: Git Commit Message Generation**

Simple flow:

```csharp
// Input: git diff output + commit context
var diff = GitHelper.GetUnstagedChanges(repoPath);
var context = new
{
    repo = "AI-prompts",
    changedFiles = fileList,
    deletedFiles = deletedList,
    area = "work",
    project = "VecTool"
};

// LLM prompt
var commitPrompt = $"""
Git repository: {context.repo}
Changes:
{diff}

Generate a concise, professional commit message (1 line, <72 chars).
Start with present tense verb. Example: "Add PROMPT-1.1-analyzer"
""";

// Call LLM
var commitMessage = await _llmService.RequestAsync(commitPrompt);
```


***

### Step 1.6: Utilities \& Helpers

**Files to create:**

- `VecTool.Core/Helpers/GitHelper.cs` – `git diff`, `git status` wrappers
- `VecTool.Core/Helpers/FavoritesManager.cs` – JSON persistence
- `VecTool.Core/Logging/PromptLogger.cs` – Structured logging (reuse VecTool's LogCtx)

***

## 📊 Phase 4.6.2: UI + Integration (25-30 hours)

**Objective:** Full-featured browser + LLM integration in UI

### Step 2.1: Prompts Browser Panel

**Files to create:**

- `VecTool.UI/Panels/PromptsBrowserPanel.cs` – Main UI
- `VecTool.UI/Controls/PromptTreeView.cs` – Hierarchy display
- `VecTool.UI/Controls/PromptResultsListView.cs` – Search results

**Simple UI layout:**

```
┌─ Prompts Browser ────────────────────────┐
│ Filter: [PROMPT*▼] Search: [______] 🔍   │
├──────────────────────────────────────────┤
│ 📂 Tree          │  File List            │
│ ├─ work          │  ┌─────────────────┐ │
│ │ ├─ VecTool     │  │ PROMPT-1.0...   │ │
│ │ │ ├─ Spaces    │  │ PROMPT-1.1...   │ │
│ │ │ └─ Guides    │  └─────────────────┘ │
│ │ └─ LINX        │                       │
│ └─ development   │                       │
├──────────────────────────────────────────┤
│ [📋 Copy] [✏️ Edit] [➕ New] [🔗 Git]     │
└──────────────────────────────────────────┘
```

**Core buttons (simple = 4):**

1. **Copy** – Clipboard (user pastes into AI chat)
2. **Edit** – Open in default editor
3. **New** – Create new version (user prompted for version \#)
4. **Git** – Open in GitExtensions (prompts repo)

**No** fancy features (keep it simple):

- No in-app preview/edit (use external editor)
- No drag-drop files yet (add in 4.6.3 if needed)
- No right-click context menu (buttons only)

***

### Step 2.2: Full-Text Search

**Integration:**

- Search box → filters results in real-time
- Searches: filename + first N chars of content
- No ranking (simple boolean match)

**Performance note:** In-memory search fine for <10K prompts. If larger, revisit.

***

### Step 2.3: AI-Assisted Prompt Categorization (On Import)

**Workflow (optional, configurable):**

```
User drops PROMPT-*.md into VecTool
  ↓
VecTool detects: IsPromptFile?
  ↓
Config: autoCategorizationOnImport? 
  ↓ YES
Send to LLM:
  "This is a prompt about [content summary].
   Where should it go? 
   Format: AREA|PROJECT|CATEGORY
   Areas: private, work, development
   Projects: VecTool, LINX, AgileAI, etc"
  ↓
LLM response: "work|VecTool|Spaces"
  ↓
Show dialog: "Move to work/VecTool/Spaces?" [Yes] [No] [Edit]
  ↓
User confirms → File moved (or user edits path)
```

**Implementation simplicity:**

- Single LLM call (no complex orchestration)
- Dialog handles user override
- On reject: ask manual path or "Imported" folder

***

### Step 2.4: Favorites (JSON Persistence)

**Simple JSON structure:**

```json
{
  "favorites": [
    {
      "path": "work/vectool/spaces/PROMPT-1.0-analyze.md",
      "label": "Test Analyzer",
      "rank": 1
    }
  ]
}
```

**UI: Simple checkbox in ListView**

```
☑ PROMPT-1.0-analyze.md  [star icon means favorite]
```


***

### Step 2.5: Git Commit Message Generation

**🔑 Integration Point: Git Panel (Future)**

When user is about to commit in GitExtensions-launched window:

- Optionally: "Generate commit message" button → calls LLM → shows suggestion
- Or manual workflow: User right-clicks changed files → "Generate commit message" in VecTool

**For MVP:** Manual trigger only (button in Git section of VecTool, not integrated into GitExtensions).

**Simple LLM prompt template:**

```
Repo: {area}/{project}
Changes:
{git_diff_summary}

Generate a concise commit message (max 72 chars).
Present tense, no period. Example: "Add PROMPT-1.1-analyzer"
```


***

### Step 2.6: Template Variables (Simple)

**Auto-provided:**

- `{{AREA}}`, `{{PROJECT}}`, `{{CATEGORY}}`
- `{{VERSION}}`, `{{TIMESTAMP}}`, `{{AUTHOR}}`
- `{{REPO_ROOT}}`

**Substitution:** Simple string.Replace() in PromptTemplateGenerator

**No custom variables yet** (KISS principle)

***

## 🎯 Phase 4.6.2 Success Criteria

✅ **Must Have:**

- ✅ TreeView hierarchy browser (Area → Project → Category)
- ✅ Full-text search (no Lucene, in-memory)
- ✅ Copy to clipboard (user controls injection)
- ✅ Edit in default editor
- ✅ Create new version (manual version \#)
- ✅ Perplexity API integration (MVP)
- ✅ Git commit message generation (AI-powered)
- ✅ Auto-categorize on import (optional, configurable)
- ✅ Favorites (JSON, UI checkbox)
- ✅ Template variable substitution

⏭️ **Future (Phase 4.6.3+):**

- ⏭️ Drag-drop file import
- ⏭️ Optional LLM providers (OpenAI, Deepseek, Anthropic)
- ⏭️ Version diff + AI changelog
- ⏭️ Semantic search (embeddings)
- ⏭️ Direct GitExtensions integration (embedded in VecTool)

***

## 📋 Configuration Files

### LLM Config (app.config + external JSON)

**`llm-config.json` (stored in repo `config/` folder):**

```json
{
  "llm": {
    "defaultProvider": "perplexity",
    "providers": {
      "perplexity": {
        "enabled": true,
        "apiKey": "${PPLX_API_KEY}",
        "model": "pplx-7b-online",
        "timeout": 30
      },
      "openai": {
        "enabled": false,
        "apiKey": "${OPENAI_API_KEY}",
        "model": "gpt-4-turbo"
      },
      "deepseek": {
        "enabled": false,
        "apiKey": "${DEEPSEEK_API_KEY}",
        "model": "deepseek-chat"
      },
      "anthropic": {
        "enabled": false,
        "apiKey": "${ANTHROPIC_API_KEY}",
        "model": "claude-3-haiku"
      }
    },
    "features": {
      "autoCategorizationOnImport": true,
      "generateCommitMessages": true,
      "maxTokensPerRequest": 500
    }
  }
}
```

**Environment variables:**

- `PPLX_API_KEY` – Perplexity API key
- `OPENAI_API_KEY` – OpenAI (optional)
- etc.

***

## 🔄 Git Workflow Integration

### Scenario 1: User Makes Prompt Changes

```
1. User modifies PROMPT-1.0-analyze.md
2. Commits in Git (external)
3. VecTool monitors: File changed → Refresh browser
4. No AI involved
```


### Scenario 2: User Wants AI Commit Message

```
1. User opens Git panel in VecTool
2. Shows uncommitted changes (AI prompts repo)
3. Button: "🤖 Generate commit message"
4. VecTool:
   - Gets git diff
   - Calls LLM with diff
   - Shows suggestion: "Add PROMPT-1.1-unit-test-generator"
5. User can copy suggestion to GitExtensions
```


### Scenario 3: Auto-Categorize on Import

```
1. User drops "my-prompt.md" → VecTool
2. Config: autoCategorizationOnImport = true
3. VecTool:
   - Reads file content
   - Calls LLM: "Where should this go?"
   - LLM: "work|VecTool|Spaces"
4. Dialog: "Move to work/VecTool/Spaces?" [Yes] [No] [Manual]
5. File moved → Browser refreshed
```


***

## 🎯 Key Simplification Decisions

| Decision | Rationale |
| :-- | :-- |
| **No streaming LLM** | Complexity overhead. Single request/response fine. |
| **In-memory search** | No Lucene. <10K files handled easily by LINQ. |
| **No retry logic yet** | Add if LLM calls fail. Start simple. |
| **No ranking/scoring** | Boolean match OK for MVP. Add scoring if needed. |
| **No right-click menus** | Just buttons. Cleaner, faster to code. |
| **No in-app editor** | Use OS default editor. Simpler, respects user choice. |
| **Manual versioning** | User types "1.1". No auto-increment complexity. |
| **JSON-only config** | No SQLite/DB. Just files + env vars. |
| **Single LLM call per action** | No complex orchestration. 1 prompt → 1 response. |
| **No favorites ranking** | Simple JSON list. No algorithm complexity. |


***

## 📊 Effort Breakdown

| Phase | Step | Hours | Status |
| :-- | :-- | :-- | :-- |
| **4.6.1** | Config + Models | 2 | 🎯 |
| **4.6.1** | LLM Provider abstraction | 4 | 🎯 |
| **4.6.1** | Search engine | 3 | 🎯 |
| **4.6.1** | LLM services (categorizer, commit gen, template) | 3 | 🎯 |
| **4.6.2** | UI panels + controls | 8 | 🎯 |
| **4.6.2** | Search integration | 2 | 🎯 |
| **4.6.2** | AI categorization dialog | 3 | 🎯 |
| **4.6.2** | Git integration (commit msg gen) | 4 | 🎯 |
| **4.6.2** | Testing + polish | 5 | 🎯 |
| **TOTAL** |  | **34 hours** | 🎯 |


***

## ✅ Summary

**What we're building:**

1. Simple prompt browser in VecTool (3-level hierarchy)
2. Full-text search (no bloat)
3. Perplexity LLM integration for 2 AI features:
    - **Auto-categorize prompts** on import
    - **Generate commit messages** for Git repos
4. Favorites + template variables
5. Plugin architecture (optional: OpenAI, Deepseek, Anthropic)

**What we're NOT building (yet):**

- Drag-drop complexity
- Advanced search ranking
- In-app prompt editing
- Embedded GitExtensions
- Semantic search

**Mantra: Simple, clean, working. Expand if needed.** 🔥
