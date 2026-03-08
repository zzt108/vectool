# 🔧 Phase 4.5.X Technical Specification — File Markers Implementation

## Document Control

| Attribute | Value |
| :-- | :-- |
| **Specification Version** | 1.0 |
| **Plan Phase** | 4.5.X |
| **Date** | 2025-11-02 |
| **Status** | Ready for Phase 4.5.X.1 Kickoff |
| **Audience** | Developers (C#/.NET 8.0 + NUnit + Shouldly) |

---

## 📐 Architecture Overview

### Layer Model

```
┌─────────────────────────────────────────────┐
│         FileSystemTraverser                 │
│     (ProcessFolder() entry point)           │
└────────────┬────────────────────────────────┘
             │
             ├──→ Layer 1: Pattern Matching ✅ (Existing)
             │    • .gitignore/.vtignore rules
             │    • Pattern library: MAB.DotIgnore
             │    • Applied to directories AND files
             │    • Fast: ~0.1ms per item
             │
             ├──→ Layer 2: File Markers 🆕 (This Phase)
             │    • In-file [VECTOOL:EXCLUDE:...] syntax
             │    • Applied to files only (not directories)
             │    • Header parsing: <1ms per file
             │    • LogCtx audit trail
             │
             └──→ Layer 3: Process File ✅ (Existing)
                  • Parse content into AIContextObject
                  • Generate embeddings
                  • Return to caller
```

### Data Flow

```
File: GeneratedClass.cs
│
├─→ Layer 1 Check
│   ├─→ Is path matching *.g.cs pattern?
│   ├─→ Result: Not in pattern
│   └─→ CONTINUE TO LAYER 2
│
├─→ Layer 2 Check
│   ├─→ Read first 1500 bytes
│   ├─→ Search first 50 lines for [VECTOOL:EXCLUDE:...]
│   ├─→ Found: [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs]
│   ├─→ Result: Marker found
│   └─→ SKIP FILE (log exclusion to SEQ)
│
└─→ Result: File excluded by Layer 2
```

---

## 🏗️ Component Specifications

### Component 1: FileMarkerExtractor

#### Interface Definition

```csharp
/// <summary>
/// Extracts file-level exclusion markers from file headers.
/// Language-agnostic implementation supporting comment syntax of any language.
/// </summary>
public interface IFileMarkerExtractor
{
    /// <summary>
    /// Extracts [VECTOOL:EXCLUDE:...] marker from file header (first 1500 bytes, first 50 lines).
    /// </summary>
    /// <param name="filePath">Full path to file to analyze</param>
    /// <returns>FileMarkerPattern if marker found; null otherwise</returns>
    /// <remarks>
    /// - Returns null if file doesn't exist (no exception)
    /// - Returns null if file read fails (logs warning)
    /// - Returns null if no marker in first 50 lines
    /// - Thread-safe when called concurrently
    /// - Regex timeout: 100ms (prevents catastrophic backtracking)
    /// </remarks>
    FileMarkerPattern? ExtractMarker(string filePath);
}
```

#### Model Definition

```csharp
/// <summary>
/// Represents parsed file-level exclusion marker.
/// </summary>
public class FileMarkerPattern
{
    /// <summary>Full path to file containing marker.</summary>
    public string FilePath { get; set; } = null!;

    /// <summary>Exclusion reason (e.g., "generated_by_xsd", "vendor_library").</summary>
    public string Reason { get; set; } = null!;

    /// <summary>Optional reference to documentation space (e.g., "@XSD-Schema-Docs"). Null if not specified.</summary>
    public string? SpaceReference { get; set; }

    /// <summary>Line number where marker was found (1-indexed).</summary>
    public int LineNumber { get; set; }

    /// <summary>UTC timestamp when marker was extracted.</summary>
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates summary for logging/display.
    /// Format: "{FilePath}:{LineNumber} → {Reason} [{SpaceReference}]"
    /// </summary>
    public override string ToString() =>
        $"{FilePath}:{LineNumber} → {Reason} [{SpaceReference ?? "no-docs"}]";
}
```

#### Implementation Details

**Regex Pattern (compiled, case-insensitive):**
```csharp
private static readonly Regex MarkerRegex = new(
    pattern: @"\[VECTOOL:EXCLUDE:([^:]+):(@[\w\-]+)?\]",
    options: RegexOptions.Compiled | RegexOptions.IgnoreCase,
    matchTimeout: TimeSpan.FromMilliseconds(100)
);
```

**Regex Breakdown:**
- `\[` — Literal opening bracket
- `VECTOOL:EXCLUDE:` — Marker type (case-insensitive)
- `([^:]+)` — Group 1: Reason (one or more non-colon characters)
- `:` — Separator
- `(@[\w\-]+)?` — Group 2: Optional space reference (@alphanumeric-dashes)
- `\]` — Literal closing bracket

**Supported Marker Formats:**
```
[VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs]     ✅ Full format
[VECTOOL:EXCLUDE:vendor_library:@Third-Party]          ✅ Full format
[VECTOOL:EXCLUDE:auto_generated:]                       ✅ No space ref
[vectool:exclude:generated:@docs]                       ✅ Case-insensitive
```

**Not Supported (no match):**
```
[VECTOOL:EXCLUDE:reason]                                ❌ Missing colon after reason
VECTOOL:EXCLUDE:reason:@docs                            ❌ Missing brackets
[VECTOOL:EXCLUDE:reason@docs]                           ❌ Missing colon before @
```

#### Public Method: ExtractMarker

```csharp
public FileMarkerPattern? ExtractMarker(string filePath)
{
    // 1. Read file header (1500 bytes max)
    string? header = ReadFileHeader(filePath, maxBytes: 1500);
    
    // 2. Handle read failures
    if (string.IsNullOrEmpty(header))
    {
        return null;  // File doesn't exist or can't be read
    }
    
    // 3. Take only first 50 lines (header section)
    var lines = header.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
    var headerLines = string.Join("\n", lines.Take(50));
    
    // 4. Apply regex pattern
    Match match;
    try
    {
        match = MarkerRegex.Match(headerLines);
    }
    catch (RegexMatchTimeoutException ex)
    {
        using var ctx = log.Ctx.Set(new Props.Add("file_path", filePath));
        log.Warn($"Regex timeout analyzing file: {ex.Message}");
        return null;
    }
    
    // 5. Validate match groups
    if (!match.Success || !match.Groups[1].Success)
    {
        return null;  // No marker found or invalid format
    }
    
    // 6. Extract components
    var reason = match.Groups[1].Value;
    var spaceReference = match.Groups[2].Success ? match.Groups[2].Value : null;
    var lineNumber = DetermineLineNumber(headerLines, match.Index);
    
    // 7. Return marker pattern
    return new FileMarkerPattern
    {
        FilePath = filePath,
        Reason = reason,
        SpaceReference = spaceReference,
        LineNumber = lineNumber,
        ExtractedAt = DateTime.UtcNow
    };
}
```

#### Private Method: ReadFileHeader

```csharp
private static string? ReadFileHeader(string filePath, int maxBytes = 1500)
{
    try
    {
        // Use StreamReader to handle encoding detection (UTF-8 BOM, UTF-16, etc.)
        using var fileStream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096
        );
        
        using var reader = new StreamReader(
            fileStream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true
        );
        
        // Read up to maxBytes / 2 (account for multi-byte characters)
        var buffer = new char[maxBytes / 2];
        int charsRead = reader.Read(buffer, 0, buffer.Length);
        
        // Return only the actual characters read
        return charsRead > 0 
            ? new string(buffer, 0, charsRead) 
            : null;
    }
    catch (FileNotFoundException)
    {
        return null;  // File doesn't exist
    }
    catch (UnauthorizedAccessException)
    {
        // Log warning but don't throw
        using var ctx = log.Ctx.Set(new Props
            .Add("file_path", filePath)
            .Add("error_type", "UnauthorizedAccessException"));
        log.Warn("Permission denied reading file header");
        return null;
    }
    catch (Exception ex)
    {
        // Log any other exceptions
        using var ctx = log.Ctx.Set(new Props
            .Add("file_path", filePath)
            .Add("error_type", ex.GetType().Name)
            .Add("message", ex.Message));
        log.Warn($"Failed to read file header: {ex.Message}");
        return null;
    }
}
```

#### Private Method: DetermineLineNumber

```csharp
private static int DetermineLineNumber(string headerContent, int matchIndex)
{
    // Count newlines before match position
    int lineNumber = 1;
    for (int i = 0; i < matchIndex && i < headerContent.Length; i++)
    {
        if (headerContent[i] == '\n')
            lineNumber++;
    }
    return lineNumber;
}
```

---

### Component 2: FileSystemTraverser Integration

#### Existing Architecture

```csharp
public class FileSystemTraverser : IFileSystemTraverser
{
    private readonly IUserInterface? _ui;
    private readonly IRecentFilesManager? _recentFilesManager;
    private readonly AiContextGenerator _aiContextGenerator;
    private readonly IIgnoreFileParser? _primaryMatcher;  // Layer 1
    private IFileMarkerExtractor? _markerExtractor;       // Layer 2 (NEW)
    
    public FileSystemTraverser(
        IUserInterface? ui,
        IRecentFilesManager? recentFilesManager,
        IFileSystemTraverser? traverser,
        IFileMarkerExtractor? markerExtractor = null)  // NEW parameter
    {
        _ui = ui;
        _recentFilesManager = recentFilesManager;
        _aiContextGenerator = new AiContextGenerator(traverser);
        _primaryMatcher = traverser as IIgnoreFileParser;
        _markerExtractor = markerExtractor;  // NEW assignment
    }
}
```

#### Method: ProcessFolder (Layer 2 Integration Point)

**Current code (Layer 1 only):**
```csharp
private void ProcessFolder(string folderPath, Action<string> onFileFound)
{
    var stack = new Stack<string> { folderPath };
    
    while (stack.Count > 0)
    {
        string current = stack.Pop();
        
        try
        {
            if (Directory.Exists(current))
            {
                // Layer 1: Check pattern exclusions
                if (_primaryMatcher?.IsIgnored(current, isDirectory: true) ?? false)
                {
                    log.Trace($"Skipping excluded folder: {current}");
                    continue;
                }
                
                // Recurse into subdirectories
                foreach (var subFolder in Directory.GetDirectories(current))
                {
                    stack.Push(subFolder);
                }
                
                // Process files in this folder
                foreach (var file in Directory.GetFiles(current))
                {
                    // Layer 1: Check pattern exclusions for files
                    if (_primaryMatcher?.IsIgnored(file, isDirectory: false) ?? false)
                    {
                        log.Trace($"Skipping excluded file (pattern): {file}");
                        continue;
                    }
                    
                    // Process the file
                    onFileFound(file);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error($"Error processing folder {current}: {ex.Message}");
        }
    }
}
```

**Updated code (Layer 1 + Layer 2):**
```csharp
private void ProcessFolder(string folderPath, Action<string> onFileFound)
{
    var stack = new Stack<string> { folderPath };
    
    while (stack.Count > 0)
    {
        string current = stack.Pop();
        
        try
        {
            if (Directory.Exists(current))
            {
                // === LAYER 1: Pattern Exclusions (Directories) ===
                if (_primaryMatcher?.IsIgnored(current, isDirectory: true) ?? false)
                {
                    using var ctx = log.Ctx.Set(ExclusionProps.CreatePatternProps(
                        current, "MAB.DotIgnore", isDirectory: true));
                    log.Info("Folder excluded by pattern");
                    continue;  // Skip this directory tree
                }
                
                // Recurse into subdirectories
                foreach (var subFolder in Directory.GetDirectories(current))
                {
                    stack.Push(subFolder);
                }
                
                // Process files in this folder
                foreach (var file in Directory.GetFiles(current))
                {
                    // === LAYER 1: Pattern Exclusions (Files) ===
                    if (_primaryMatcher?.IsIgnored(file, isDirectory: false) ?? false)
                    {
                        using var ctx = log.Ctx.Set(ExclusionProps.CreatePatternProps(
                            file, "MAB.DotIgnore", isDirectory: false));
                        log.Info("File excluded by pattern");
                        continue;  // Skip this file
                    }
                    
                    // === LAYER 2: File Marker Exclusions (NEW) ===
                    if (_markerExtractor != null)
                    {
                        var marker = _markerExtractor.ExtractMarker(file);
                        if (marker != null)
                        {
                            using var ctx = log.Ctx.Set(ExclusionProps.CreateMarkerProps(marker));
                            log.Info("File excluded by marker");
                            continue;  // Skip this file
                        }
                    }
                    
                    // === LAYER 3: Process File ===
                    // File passed all exclusion checks
                    onFileFound(file);
                }
            }
        }
        catch (Exception ex)
        {
            using var ctx = log.Ctx.Set(new Props.Add("folder_path", current));
            log.Error($"Error processing folder: {ex.Message}");
        }
    }
}
```

#### LogCtx Props Helper Class

```csharp
/// <summary>
/// LogCtx Properties helper for file exclusion audit trail.
/// Provides standardized Props objects for SEQ queries.
/// </summary>
public static class ExclusionProps
{
    /// <summary>
    /// Creates Props for Layer 1 pattern-based exclusion.
    /// </summary>
    public static Props CreatePatternProps(string filePath, string library, bool isDirectory)
        => new Props
            .Add("source", "pattern")
            .Add("file_path", filePath)
            .Add("library", library)
            .Add("pattern_type", isDirectory ? "folder" : "file");

    /// <summary>
    /// Creates Props for Layer 2 file marker exclusion.
    /// </summary>
    public static Props CreateMarkerProps(FileMarkerPattern marker)
        => new Props
            .Add("source", "file_marker")
            .Add("file_path", marker.FilePath)
            .Add("reason", marker.Reason)
            .Add("space_reference", marker.SpaceReference ?? "none")
            .Add("line_number", marker.LineNumber);

    /// <summary>
    /// Creates Props for files that passed all exclusion checks and were processed.
    /// </summary>
    public static Props CreateProcessedProps(string filePath, long bytesProcessed)
        => new Props
            .Add("source", "processed")
            .Add("file_path", filePath)
            .Add("bytes_processed", bytesProcessed);
}
```

---

## 📊 Performance Specifications

### Header Reading Performance

| Metric | Target | Notes |
| :-- | :-- | :-- |
| File read (1500 bytes) | < 0.5ms | SSD typical; network shares may be slower |
| Encoding detection | < 0.1ms | .NET StreamReader handles transparently |
| Regex matching | < 0.4ms | Timeout set to 100ms (catastrophic backtracking protection) |
| **Total per file** | **< 1ms** | Average case; ~2ms for problematic encodings |

### Traversal Performance

| Scenario | Files | Pattern Only | +Markers | Overhead |
| :-- | :-- | :-- | :-- | :-- |
| Small folder | 10 | ~10ms | ~15ms | 5ms (50%) |
| Medium folder | 100 | ~100ms | ~120ms | 20ms (20%) |
| Large folder | 1000 | ~1000ms | ~1100-1150ms | 100-150ms (10-15%) |
| **Very large** | **10000** | **~10s** | **~10.5-11s** | **500-1000ms (5-10%)** |

**Note:** Marker cache dramatically reduces overhead on repeated traversals (2nd pass: < 5% overhead)

### Memory Impact

| Component | Allocation | Notes |
| :-- | :-- | :-- |
| Regex (compiled) | ~2KB | One-time, reused across all files |
| File header buffer | ~750B per file | Temporary, released after parsing |
| FileMarkerPattern object | ~200B | Only created if marker found |
| MemoryCache (optional) | ~1MB | Stores last 1024 marker checks |

---

## 🧪 Test Specifications

### Test Framework

- **Unit Testing:** NUnit
- **Assertions:** Shouldly (fluent assertions)
- **Mocking:** (NSubstitute recommended for IFileMarkerExtractor mocks)

### Test Coverage Requirements

| Category | Minimum | Target |
| :-- | :-- | :-- |
| Critical paths | 100% | 100% |
| Error handling | 95% | 100% |
| Integration | 80% | 90% |
| **Overall** | **85%** | **90%** |

### Critical Test Cases (Minimum Set)

**Marker Extraction - Valid:**
- ✅ C# comment marker
- ✅ Python comment marker
- ✅ XML comment marker
- ✅ Marker with space reference
- ✅ Marker without space reference

**Marker Extraction - Boundary Conditions:**
- ✅ Marker at line 1
- ✅ Marker at line 50 (boundary)
- ✅ Marker at line 51 (beyond boundary)
- ✅ Marker at byte 1500 (boundary)
- ✅ Marker at byte 1501 (beyond boundary)

**Error Handling:**
- ✅ Non-existent file returns null
- ✅ Permission denied logged as warning, returns null
- ✅ Encoding errors handled gracefully
- ✅ Regex timeout caught and logged

**Integration:**
- ✅ FileSystemTraverser correctly skips marked files
- ✅ LogCtx props logged correctly
- ✅ Layer 1 patterns checked before Layer 2 markers

---

## 🔐 Security Considerations

### Input Validation

- **File path:** Validated by `File.Exists()` check
- **Regex pattern:** Fixed pattern, no user input
- **File content:** Only header read (first 1500 bytes max), no code execution

### Performance DoS Prevention

- **Regex timeout:** 100ms maximum (prevents catastrophic backtracking)
- **Header size limit:** 1500 bytes maximum (prevents excessive memory)
- **Line limit:** First 50 lines only (prevents large header scanning)

### Exception Safety

- **All exceptions caught** and logged, never bubbles up
- **Thread-safe** (regex is compiled and thread-safe)
- **No file locks** (read-only, file share read-enabled)

---

## 📝 Logging Specifications

### Log Levels

| Level | Usage | Example |
| :-- | :-- | :-- |
| **Trace** | Layer 1 pattern matches (verbose) | `"Folder excluded by pattern"` |
| **Info** | File exclusion events (auditable) | `"File excluded by marker"` |
| **Warn** | Extraction failures (investigate) | `"Failed to read file header"` |
| **Error** | Unexpected exceptions (critical) | `"Error processing folder"` |

### SEQ Query Examples

**Find all file marker exclusions:**
```
source = "file_marker"
```

**Find exclusions with specific reason:**
```
source = "file_marker" AND reason = "generated_by_xsd"
```

**Find all documented exclusions:**
```
source = "file_marker" AND space_reference != "none"
```

**Audit trail for specific file:**
```
file_path = "/path/to/file.cs"
```

**Count exclusion types:**
```
source = "file_marker" | stats count() by reason
```

---

## 🚀 Deployment Checklist

### Pre-Deployment

- [ ] All unit tests passing (90%+ coverage)
- [ ] Performance tests validate < 1150ms for 1000 files
- [ ] Code review approved
- [ ] Documentation complete and reviewed

### Deployment

- [ ] Create feature branch: `feature/4.5.X.1-marker-parser-design`
- [ ] Commit changes with message: `feat(markers): add FileMarkerExtractor`
- [ ] Create PR with test results attached
- [ ] Deploy to staging environment
- [ ] Run integration tests

### Post-Deployment

- [ ] Monitor SEQ logs for exclusion events
- [ ] Verify LogCtx props structure in SEQ queries
- [ ] Performance monitoring confirms < 1150ms baseline
- [ ] Document any edge cases discovered

---

## 📋 Architecture Decision Records (ADRs)

### ADR-1: Language-Agnostic Marker Syntax

**Decision:** Use `[VECTOOL:EXCLUDE:reason:@reference]` format instead of language-specific annotations

**Rationale:**
- Works in any language (C#, Python, JS, XML, etc.)
- No language-specific parser needed
- Marker is human-readable in source files
- Easy to document with regex pattern

**Alternative rejected:** Language-specific attributes
- Would require separate parser per language
- C# attributes, Python decorators, JS comments - too complex
- Maintenance burden too high

### ADR-2: Header Size Limit (1500 bytes)

**Decision:** Read only first 1500 bytes of file for marker extraction

**Rationale:**
- Covers typical file headers: XML declarations, Python shebangs, copyright notices
- Prevents reading large files unnecessarily
- Balances coverage vs. performance
- 1500 bytes ≈ 50 lines of typical code

**Calculation:**
- XML declaration: ~200 bytes
- Python shebang + encoding: ~50 bytes
- Copyright notice: ~500 bytes
- Module docstring: ~500 bytes
- Total: ~1250 bytes (safely under 1500 limit)

### ADR-3: Line-Based Bounds Check

**Decision:** Limit marker search to first 50 lines of file

**Rationale:**
- Markers should appear in file header, not mid-code
- Prevents false positives in string literals or comments
- 50 lines covers all realistic header scenarios
- Performance: Still < 1ms per file

### ADR-4: Layer 2 Applies to Files Only

**Decision:** File markers only exclude individual files, not directories

**Rationale:**
- Pattern-based exclusion (Layer 1) already handles directories
- File markers are placed in file headers - only files have headers
- Mixing directory + file exclusion would require directory scanning
- Cleaner separation of concerns: Layer 1 (directories), Layer 2 (files)

---

**Document Version:** 1.0  
**Last Updated:** 2025-11-02  
**Next Review:** After Phase 4.5.X.1 implementation complete