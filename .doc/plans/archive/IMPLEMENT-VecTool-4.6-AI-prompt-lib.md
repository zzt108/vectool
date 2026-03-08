## Step-by-Step Implementation Roadmap

Based on Plan 4.6, here's the detailed implementation path with acceptance criteria, dependencies, and validation points.[^1]

***

## Phase 4.6.1: Infrastructure Layer

**Branch:** `feature/4.6.1-infrastructure-llm-prompts`
**Estimated Duration:** 12 hours
**Pre-requisites:** None (starts from current codebase)

***

### Step 4.6.1.1: Configuration (JSON-based)

**Duration:** 2 hours
**Branch:** `feature/4.6.1.1-config-prompts-llm`

#### Files to Create

| File | Purpose | Pattern Reference |
| :-- | :-- | :-- |
| `VecTool.Core/Config/PromptsConfig.cs` | Prompts repo settings | Similar to `VectorStoreConfig` [^2] |
| `VecTool.Core/Config/LLMProviderConfig.cs` | LLM provider config loader | JSON deserialization + env var substitution [^1] |
| `VecTool.Core/Config/IPromptsConfig.cs` | Interface for DI | Follows existing `IVectorStoreConfig` pattern [^2] |

#### Implementation Details

**app.config additions:**

```xml
<add key="promptsRepositoryPath" value="C:\repos\AI-prompts" />
<add key="promptsFileExtensions" value=".md,.txt,.yaml,.json" />
<add key="llmProviderConfig" value="C:\repos\AI-prompts\config\llm-config.json" />
<add key="favoritesConfigPath" value="C:\repos\AI-prompts\config\favorites.json" />
```

**External JSON structure (llm-config.json):**

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

**Key classes:**

- `PromptsConfig` with properties: `RepositoryPath`, `FileExtensions`, `LLMConfigPath`, `FavoritesConfigPath`[^1]
- `LLMProviderConfig` with JSON deserialization + environment variable substitution for `${VAR}` patterns[^1]


#### Acceptance Criteria

✅ Config loads from app.config successfully
✅ Environment variable substitution works (e.g., `${PPLX_API_KEY}`)
✅ Invalid paths/missing JSON handled gracefully with LogCtx warnings
✅ Unit tests: `PromptsConfigTests`, `LLMProviderConfigTests` (NUnit + Shouldly)[^1]

#### Testing Checklist

- [ ] Valid config loads without exceptions
- [ ] Missing repo path logs warning
- [ ] Invalid JSON shows clear error message
- [ ] Environment variables substitute correctly
- [ ] Default values applied when optional config missing

***

### Step 4.6.1.2: Domain Models

**Duration:** 2 hours
**Branch:** `feature/4.6.1.2-domain-models-prompt-metadata`
**Depends on:** 4.6.1.1

#### Files to Create

| File | Purpose | Key Features |
| :-- | :-- | :-- |
| `VecTool.Core/Models/PromptMetadata.cs` | Metadata parser | Filename parsing: `PROMPT-1.0-analyzer.md` → version=1.0, name=analyzer [^1] |
| `VecTool.Core/Models/PromptFile.cs` | File wrapper | Path, Content, Metadata, IsFavorite properties [^1] |

#### Implementation Details

**PromptMetadata fields:**

```csharp
public sealed record PromptMetadata
{
    public string FileName { get; init; }
    public string Version { get; init; }  // "1.0", "1.1"
    public string Name { get; init; }     // "analyzer", "git-integration"
    public string Type { get; init; }     // "PROMPT", "GUIDE", "SPACE"
    public string? Description { get; init; }  // From first line/comment
    public string Area { get; init; }     // "work", "private"
    public string Project { get; init; }  // "VecTool", "LINX"
    public string Category { get; init; } // "Spaces", "Guides"
}
```

**Parsing logic:**

- Path: `work/vectool/spaces/PROMPT-1.0-analyzer.md` → Area=work, Project=vectool, Category=spaces[^1]
- Filename: `PROMPT-1.0-analyzer.md` → Type=PROMPT, Version=1.0, Name=analyzer[^1]
- No regex overkill - simple `string.Split('-')` and path parsing[^1]

**PromptFile wrapper:**

```csharp
public sealed record PromptFile
{
    public string FullPath { get; init; }
    public PromptMetadata Metadata { get; init; }
    public string Content { get; init; }
    public bool IsFavorite { get; set; }
    public DateTime LastModified { get; init; }
}
```


#### Acceptance Criteria

✅ Parses `PROMPT-1.0-name.md` correctly (type, version, name)[^1]
✅ Parses `GUIDE-1.5-convention.md` correctly[^1]
✅ Handles invalid filenames gracefully (logs warning, returns null)[^1]
✅ Extracts area/project/category from path[^1]
✅ Unit tests: `PromptMetadataTests` with 10+ scenarios[^1]

#### Testing Checklist

- [ ] Valid PROMPT file parsed correctly
- [ ] Valid GUIDE file parsed correctly
- [ ] Invalid filename returns null + logs warning
- [ ] Path parsing extracts area/project/category
- [ ] Missing description doesn't crash parsing
- [ ] Non-standard paths handled (e.g., root-level files)

***

### Step 4.6.1.3: LLM Provider Abstraction

**Duration:** 4 hours
**Branch:** `feature/4.6.1.3-llm-provider-abstraction`
**Depends on:** 4.6.1.1

#### Files to Create

| File | Purpose | Complexity |
| :-- | :-- | :-- |
| `VecTool.Core/AI/ILLMProvider.cs` | Simple 3-method interface | Low [^1] |
| `VecTool.Core/AI/LLMProviderFactory.cs` | Factory pattern | Medium [^1] |
| `VecTool.Core/AI/Providers/PerplexityProvider.cs` | MVP implementation | Medium [^1] |
| `VecTool.Core/AI/Providers/OpenAIProvider.cs` | Stub (optional) | Low [^1] |

#### Implementation Details

**ILLMProvider interface (KISS):**

```csharp
public interface ILLMProvider
{
    Task<string> RequestAsync(string prompt);
    Task<bool> ValidateConfigAsync();
    string GetProviderName();
}
```

**Simplicity decisions:**

- No streaming (complexity overhead)[^1]
- No retry logic yet (add if needed)[^1]
- Single model per provider (configured, not auto-detected)[^1]
- Timeout via `HttpClient.Timeout`[^1]

**PerplexityProvider implementation:**

```csharp
public sealed class PerplexityProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    
    public async Task<string> RequestAsync(string prompt)
    {
        // POST to https://api.perplexity.ai/chat/completions
        // Body: {"model": "...", "messages": [{"role": "user", "content": "..."}]}
        // Return: response.choices[^0].message.content
    }
    
    // ... ValidateConfigAsync, GetProviderName
}
```

**LLMProviderFactory:**

```csharp
public static class LLMProviderFactory
{
    public static ILLMProvider Create(LLMProviderConfig config)
    {
        // Switch on config.defaultProvider
        // Return new PerplexityProvider(config.providers["perplexity"])
    }
}
```


#### Acceptance Criteria

✅ `ILLMProvider` interface defined with 3 methods[^1]
✅ `PerplexityProvider` makes successful API calls[^1]
✅ Invalid API key returns clear error (not crash)[^1]
✅ Timeout configured and enforced[^1]
✅ Factory creates correct provider based on config[^1]
✅ Unit tests + integration tests (mocked HTTP)[^1]

#### Testing Checklist

- [ ] Valid Perplexity API call succeeds
- [ ] Invalid API key returns error (not exception)
- [ ] Timeout enforced (test with 1s timeout)
- [ ] Factory returns correct provider type
- [ ] Disabled provider throws clear exception
- [ ] LogCtx logs all API requests/responses

***

### Step 4.6.1.4: Search Engine (In-memory)

**Duration:** 3 hours
**Branch:** `feature/4.6.1.4-search-engine-in-memory`
**Depends on:** 4.6.1.2

#### Files to Create

| File | Purpose | Pattern |
| :-- | :-- | :-- |
| `VecTool.Core/Services/PromptSearchEngine.cs` | In-memory search | Similar to existing traversal logic [^2] |

#### Implementation Details

**Simplicity decisions:**

- In-memory `Dictionary<string, PromptFile>` indexed by path[^1]
- LINQ queries for search (no Lucene)[^1]
- Search scope: filename + description + first 2000 chars[^1]
- No ranking/scoring (boolean match)[^1]
- `RebuildIndex()` called on app startup + `FileSystemWatcher` events[^1]

**Class structure:**

```csharp
public sealed class PromptSearchEngine
{
    private readonly Dictionary<string, PromptFile> _index = new();
    private readonly IPromptsConfig _config;
    
    public void RebuildIndex()
    {
        // Traverse config.RepositoryPath
        // Parse each file → PromptFile
        // Add to _index
    }
    
    public List<PromptFile> Search(string query)
    {
        // Simple LINQ: _index.Values.Where(p => p matches query)
        // Match on: filename, description, first 2000 chars of content
    }
    
    public List<PromptFile> GetByHierarchy(string? area, string? project, string? category)
    {
        // Filter _index by Metadata properties
    }
}
```

**Performance note:** Fine for <10K prompts[^1]

#### Acceptance Criteria

✅ Index rebuilt from disk successfully[^1]
✅ Search returns correct results (filename match)[^1]
✅ Search returns correct results (content match)[^1]
✅ Hierarchy filtering works (area/project/category)[^1]
✅ Empty search returns all files[^1]
✅ Unit tests: `PromptSearchEngineTests` with test prompts[^1]

#### Testing Checklist

- [ ] Rebuild index parses 100+ test files
- [ ] Search "analyzer" finds matching files
- [ ] Search "VecTool" finds content matches
- [ ] Filter by area="work" returns correct subset
- [ ] Invalid query returns empty (not exception)
- [ ] Performance: <100ms for 1000 files

***

### Step 4.6.1.5: LLM Service Layer

**Duration:** 3 hours
**Branch:** `feature/4.6.1.5-llm-service-layer`
**Depends on:** 4.6.1.3, 4.6.1.4

#### Files to Create

| File | Purpose | AI Feature |
| :-- | :-- | :-- |
| `VecTool.Core/Services/PromptCategorizer.cs` | AI suggest category | Auto-categorize imports [^1] |
| `VecTool.Core/Services/GitCommitMessageGenerator.cs` | AI commit messages | Git workflow [^1] |
| `VecTool.Core/Services/PromptTemplateGenerator.cs` | Variable substitution | Template support [^1] |

#### Implementation Details

**GitCommitMessageGenerator (key feature):**

```csharp
public sealed class GitCommitMessageGenerator
{
    private readonly ILLMProvider _llmProvider;
    
    public async Task<string> GenerateAsync(string gitDiff, CommitContext context)
    {
        var prompt = $"""
        Git repository: {context.Repo}
        Changes:
        {gitDiff}
        
        Generate a concise, professional commit message (1 line, <72 chars).
        Start with present tense verb. Example: "Add PROMPT-1.1-analyzer"
        """;
        
        return await _llmProvider.RequestAsync(prompt);
    }
}
```

**PromptCategorizer:**

```csharp
public sealed class PromptCategorizer
{
    public async Task<CategorySuggestion> SuggestCategoryAsync(string content)
    {
        var prompt = $"""
        This is a prompt about:
        {content.Substring(0, 1000)}
        
        Where should it go?
        Format: AREA|PROJECT|CATEGORY
        Areas: private, work, development
        Projects: VecTool, LINX, AgileAI
        """;
        
        var response = await _llmProvider.RequestAsync(prompt);
        // Parse "work|VecTool|Spaces" → CategorySuggestion
    }
}
```

**PromptTemplateGenerator:**

```csharp
public sealed class PromptTemplateGenerator
{
    public string ApplyTemplateVariables(string content, Dictionary<string, string> vars)
    {
        // Simple string.Replace for {{VAR}}
        // Auto-provided: {{AREA}}, {{PROJECT}}, {{CATEGORY}}, {{VERSION}}, {{TIMESTAMP}}
    }
}
```


#### Acceptance Criteria

✅ Commit message generation returns valid message[^1]
✅ Prompt categorizer parses AI response correctly[^1]
✅ Template variables substituted correctly[^1]
✅ LLM errors handled gracefully (not crash)[^1]
✅ All AI requests logged via LogCtx[^1]
✅ Unit tests + integration tests[^1]

#### Testing Checklist

- [ ] Commit message <72 chars
- [ ] Category suggestion parsed correctly
- [ ] Invalid AI response handled gracefully
- [ ] Template vars: {{AREA}}, {{TIMESTAMP}} work
- [ ] Custom vars not supported (by design)
- [ ] LogCtx captures all LLM interactions

***

### Step 4.6.1.6: Utilities \& Helpers

**Duration:** 2 hours
**Branch:** `feature/4.6.1.6-utilities-git-helpers`
**Depends on:** 4.6.1.5

#### Files to Create

| File | Purpose | Technology |
| :-- | :-- | :-- |
| `VecTool.Core/Helpers/GitHelper.cs` | Git command wrappers | Process.Start("git") [^1] |
| `VecTool.Core/Helpers/FavoritesManager.cs` | JSON persistence | System.Text.Json [^1] |

#### Implementation Details

**GitHelper:**

```csharp
public static class GitHelper
{
    public static string GetUnstagedChanges(string repoPath)
    {
        // git diff
    }
    
    public static List<string> GetChangedFiles(string repoPath)
    {
        // git status --porcelain
    }
    
    public static bool IsGitRepository(string path)
    {
        // Check .git folder exists
    }
}
```

**FavoritesManager:**

```csharp
public sealed class FavoritesManager
{
    public List<string> LoadFavorites(string configPath)
    {
        // Deserialize JSON → List<string> (file paths)
    }
    
    public void SaveFavorites(string configPath, List<string> favorites)
    {
        // Serialize to JSON
    }
}
```


#### Acceptance Criteria

✅ GitHelper executes `git diff` successfully[^1]
✅ GitHelper detects non-git repo gracefully[^1]
✅ FavoritesManager loads/saves JSON correctly[^1]
✅ Invalid JSON handled without crash[^1]
✅ Unit tests cover all methods[^1]

#### Testing Checklist

- [ ] Git diff returns diff output
- [ ] Non-git folder returns error (not exception)
- [ ] Favorites JSON loads correctly
- [ ] Empty favorites file creates default
- [ ] Corrupted JSON logs warning

***

## Phase 4.6.2: UI + Integration

**Branch:** `feature/4.6.2-ui-prompts-browser`
**Estimated Duration:** 25-30 hours
**Pre-requisites:** Phase 4.6.1 complete

***

### Step 4.6.2.1: Prompts Browser Panel

**Duration:** 8 hours
**Branch:** `feature/4.6.2.1-prompts-browser-panel`
**Depends on:** All 4.6.1 steps

#### Files to Create

| File | Purpose | WinForms Pattern |
| :-- | :-- | :-- |
| `VecTool.UI/Panels/PromptsBrowserPanel.cs` | Main UI container | Similar to `RecentFilesPanel` [^2] |
| `VecTool.UI/Controls/PromptTreeView.cs` | Hierarchy TreeView | Custom TreeView with LogCtx [^1] |
| `VecTool.UI/Controls/PromptResultsListView.cs` | Search results ListView | Custom ListView [^1] |

#### UI Layout

```
┌─ Prompts Browser ────────────────────────┐
│ Filter: [PROMPT*▼] Search: [______] 🔍   │
├──────────────────────────────────────────┤
│ 📂 Tree          │  File List            │
│ ├─ work          │  ┌─────────────────┐ │
│ │ ├─ VecTool     │  │ ☑ PROMPT-1.0... │ │
│ │ │ ├─ Spaces    │  │ ☐ PROMPT-1.1... │ │
│ │ │ └─ Guides    │  └─────────────────┘ │
│ │ └─ LINX        │                       │
│ └─ development   │                       │
├──────────────────────────────────────────┤
│ [📋 Copy] [✏️ Edit] [➕ New] [🔗 Git]     │
└──────────────────────────────────────────┘
```


#### Core Buttons (4 total)

1. **Copy** → Clipboard (user pastes into AI chat)[^1]
2. **Edit** → `Process.Start(defaultEditor, filePath)`[^1]
3. **New** → Dialog: "Enter version (e.g., 1.1)" → Create copy[^1]
4. **Git** → `Process.Start("GitExtensions.exe", repoPath)`[^1]

#### Acceptance Criteria

✅ TreeView displays area → project → category hierarchy[^1]
✅ ListView shows filtered/searched results[^1]
✅ Copy button copies content to clipboard[^1]
✅ Edit button opens default editor[^1]
✅ New button creates versioned copy[^1]
✅ Git button launches GitExtensions[^1]
✅ Favorites checkbox toggles state[^1]

#### Testing Checklist

- [ ] TreeView loads from search engine
- [ ] Click tree node filters ListView
- [ ] Copy button copies full content
- [ ] Edit opens Notepad/VS Code
- [ ] New version dialog validates input
- [ ] Git button opens correct repo

***

### Step 4.6.2.2: Full-Text Search Integration

**Duration:** 2 hours
**Branch:** `feature/4.6.2.2-fulltext-search-integration`
**Depends on:** 4.6.2.1

#### Implementation Details

- Search box → real-time filtering (debounced 300ms)[^1]
- Empty search → show hierarchy view[^1]
- Non-empty search → flat list view[^1]
- No ranking (boolean match only)[^1]


#### Acceptance Criteria

✅ Search updates results in <300ms[^1]
✅ Empty search shows full hierarchy[^1]
✅ Search matches filename + content[^1]
✅ Case-insensitive search[^1]

***

### Step 4.6.2.3: AI-Assisted Categorization Dialog

**Duration:** 3 hours
**Branch:** `feature/4.6.2.3-ai-categorization-dialog`
**Depends on:** 4.6.2.1

#### Workflow

```
User imports PROMPT-*.md
  ↓
Config: autoCategorizationOnImport? YES
  ↓
Call LLM → "work|VecTool|Spaces"
  ↓
Dialog: "Move to work/VecTool/Spaces?" [Yes] [No] [Edit]
  ↓
User confirms → File moved
```


#### Acceptance Criteria

✅ AI categorization called on import[^1]
✅ Dialog shows suggestion[^1]
✅ User can override path[^1]
✅ File moved to correct location[^1]
✅ Browser refreshed after move[^1]

***

### Step 4.6.2.4: Favorites (JSON Persistence)

**Duration:** 2 hours
**Branch:** `feature/4.6.2.4-favorites-json-persistence`
**Depends on:** 4.6.2.1

#### Implementation Details

**JSON structure:**

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

**UI:** Checkbox in ListView (☑ = favorite)[^1]

#### Acceptance Criteria

✅ Toggle favorite saves to JSON[^1]
✅ Favorites loaded on startup[^1]
✅ Favorites displayed at top of list[^1]

***

### Step 4.6.2.5: Git Commit Message Generation

**Duration:** 4 hours
**Branch:** `feature/4.6.2.5-git-commit-ai-generation`
**Depends on:** 4.6.2.1

#### UI Integration

Button: "🤖 Generate Commit Message"
→ Gets `git diff`
→ Calls LLM
→ Shows dialog with suggestion
→ User copies to GitExtensions[^1]

#### Acceptance Criteria

✅ Button detects Git repo[^1]
✅ AI generates commit message <72 chars[^1]
✅ User can copy suggestion[^1]
✅ Error handled if no changes[^1]

***

### Step 4.6.2.6: Template Variables

**Duration:** 2 hours
**Branch:** `feature/4.6.2.6-template-variables`
**Depends on:** 4.6.2.1

#### Auto-Provided Variables

- `{{AREA}}`, `{{PROJECT}}`, `{{CATEGORY}}`[^1]
- `{{VERSION}}`, `{{TIMESTAMP}}`, `{{AUTHOR}}`[^1]
- `{{REPO_ROOT}}`[^1]


#### Implementation

Simple `string.Replace()` in `PromptTemplateGenerator`[^1]

#### Acceptance Criteria

✅ Variables substituted on copy[^1]
✅ Missing variables handled gracefully[^1]

***

### Testing + Polish

**Duration:** 5 hours

- Integration tests for full workflow[^1]
- Performance testing (1000+ prompts)[^1]
- UI polish (keyboard shortcuts, tooltips)[^1]
- Documentation updates[^1]

***

## Git Workflow Summary

### Branch Hierarchy

```
main
 ├── integration/4.6-ai-prompts-git
 │    ├── feature/4.6.1-infrastructure-llm-prompts
 │    │    ├── feature/4.6.1.1-config-prompts-llm
 │    │    ├── feature/4.6.1.2-domain-models-prompt-metadata
 │    │    ├── feature/4.6.1.3-llm-provider-abstraction
 │    │    ├── feature/4.6.1.4-search-engine-in-memory
 │    │    ├── feature/4.6.1.5-llm-service-layer
 │    │    └── feature/4.6.1.6-utilities-git-helpers
 │    └── feature/4.6.2-ui-prompts-browser
 │         ├── feature/4.6.2.1-prompts-browser-panel
 │         ├── feature/4.6.2.2-fulltext-search-integration
 │         ├── feature/4.6.2.3-ai-categorization-dialog
 │         ├── feature/4.6.2.4-favorites-json-persistence
 │         ├── feature/4.6.2.5-git-commit-ai-generation
 │         └── feature/4.6.2.6-template-variables
```


### Merge Strategy

1. Complete step → commit to step branch
2. Step complete → merge to phase branch (`4.6.1` or `4.6.2`)
3. Phase complete → merge to `integration/4.6`
4. Integration tested → merge to `main`

***

## Effort Summary

| Phase | Total Hours | Status |
| :-- | :-- | :-- |
| **4.6.1 Infrastructure** | 12 | 🎯 Not started |
| **4.6.2 UI + Integration** | 25 | 🎯 Not started |
| **Testing + Polish** | 5 | 🎯 Not started |
| **TOTAL** | **42 hours** | 🎯 |


***

## Next Actions

**To start Phase 4.6.1 Step 1:**

```bash
git checkout -b feature/4.6.1.1-config-prompts-llm
```

Then implement: `PromptsConfig.cs`, `LLMProviderConfig.cs`, tests.

**Reply with:**

- **"start 4.6.1.1"** to begin Step 1.1 implementation
- **"dependencies"** for detailed dependency graph
- **"estimate"** for refined time estimates per component
