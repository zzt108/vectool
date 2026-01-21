# 📋 File Markers + Auto-Levels Implementation Template - 4.5.X (Updated)

## Quick Reference

| Attribute | Value |
| :-- | :-- |
| Plan Version | 4.5.X (v1.1 — XSD + JSON Updated) |
| Parent Plan | 4.5 |
| Target App Version | 4.x |
| Status | 📋 Planning |
| Precondition | Phase 4.5.2 complete (3/3 handlers verified) |
| Last Updated | 2025-11-02 (XSD mandate + JSON string support) |

---

## 🎯 Mission Statement

Add **granular file-level exclusion markers** with **language-agnostic syntax** to enable precise control over file content visibility during AI context generation, complementing existing pattern-based exclusion (Layer 1: patterns, Layer 2: markers).

**Key addition:** XSD files and JSON string-embedded markers are first-class citizens.

---

## 📊 Implementation Roadmap (Revised)

### Timeline Overview

| Step | Task | Deliverable | Time | Status |
| :-- | :-- | :-- | :-- | :-- |
| 1 | File marker parser design | `FileMarkerExtractor.cs` interface + impl | 1h | 📋 Planning |
| 2 | FileSystemTraverser integration | Modified `ProcessFolder()` method | 1.5h | 📋 Planning |
| 3 | LogCtx structured logging | Logging props + audit trail | 0.5h | 📋 Planning |
| 4 | Unit tests (language-agnostic) | `FileMarkerExtractorTests.cs` | 1h | 📋 Planning |
| 5 | Documentation + examples | Markdown guide + code templates | 0.5h | 📋 Planning |
| **Total** | | | **4.5h** | |

**Justification:** Auto-levels (token-based splitting) deferred to Phase 4.5.Y per plan review.

---

## 🌿 Proposed Git Branches

| Phase | Branch Name | Purpose |
| :-- | :-- | :-- |
| 4.5.X.1 | `feature/4.5.X.1-marker-parser-design` | Core parser logic |
| 4.5.X.2 | `feature/4.5.X.2-traverser-integration` | Layer 2 integration |
| 4.5.X.3 | `feature/4.5.X.3-logctx-audit-trail` | Logging + Props |
| 4.5.X.4 | `feature/4.5.X.4-unit-tests` | Test coverage |
| 4.5.X.5 | `feature/4.5.X.5-documentation` | User guide + templates |

---

## 🔍 Core Design Specification

### Marker Syntax (Language-Agnostic)

```
[VECTOOL:EXCLUDE:reason_text:@space_reference]
```

**Components:**
- **`[VECTOOL:EXCLUDE:...]`**: Marker wrapper (case-sensitive on EXCLUDE keyword, but matched case-insensitively)
- **`reason_text`**: URL-safe identifier (alphanumeric + underscore) explaining exclusion
- **`@space_reference`**: Optional space/documentation reference (e.g., `@XSD-Schema-Docs`)

**Format Rules:**
- Placed in **first 1500 bytes** of file (header section)
- Positioned in **first 50 lines** maximum
- Must appear in **language-appropriate syntax** (comments, strings, etc.)
- Extracted via regex: `\[VECTOOL:EXCLUDE:([^:]+):(@[\w\-]+)?\]` (case-insensitive match)

### Language Support Matrix (UPDATED)

| Language | Extension | Marker Location | XSD Support | Status | Notes |
| :-- | :-- | :-- | :-- | :-- | :-- |
| **XML/XSD** 🆕 | `.xml`, `.xsd` | Comment: `<!-- [VECTOOL:EXCLUDE:...] -->` | ✅ **REQUIRED** | ✅ Supported | First-class citizen, use `generated_by_xsd` reason |
| **C# (XSD-generated)** | `.g.cs` | Comment: `// [VECTOOL:EXCLUDE:...]` | ✅ **REQUIRED** | ✅ Supported | Generated classes from XSD (e.g., Person.g.cs) |
| **C#** | `.cs` | Comment: `// ...` or `/* ... */` | ✅ Supported | ✅ Supported | Hand-written C# code |
| **Python** | `.py` | Comment: `# [VECTOOL:EXCLUDE:...]` | ✅ Supported | ✅ Supported | Python 3.x files |
| **JavaScript** | `.js` | Comment: `// ...` or `/* ... */` | ✅ Supported | ✅ Supported | Node.js and browser code |
| **Java** | `.java` | Comment: `// ...` or `/** ... */` | ✅ Supported | ✅ Supported | Java 8+ files |
| **Go** | `.go` | Comment: `// ...` or `/* ... */` | ✅ Supported | ✅ Supported | Go 1.x files |
| **Rust** | `.rs` | Comment: `// ...` or `/* ... */` | ✅ Supported | ✅ Supported | Rust 2021 edition |
| **YAML** | `.yaml`, `.yml` | Comment: `# [VECTOOL:EXCLUDE:...]` | ✅ Supported | ✅ Supported | Configuration files |
| **JSON** 🆕 | `.json` | String value: `"__vectool_exclude": "[VECTOOL:EXCLUDE:...]"` | ✅ **NEW** | ✅ Supported | Member at file beginning (first 50 lines) |
| ~~TypeScript~~ | ~~`.ts`~~ | ~~Comment~~ | ✅ Supported | ✅ Supported | Same as JavaScript (same syntax rules) |

**Language Support Details:**

#### XSD Files (First-Class Support)

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs] -->
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
    <!-- schema content -->
</xs:schema>
```

#### C# Generated Files (First-Class Support)

```csharp
// [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs]
// Auto-generated from XSD schema
[System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.0")]
public partial class Person { }
```

#### JSON Files (NEW: String Value Support)

```json
{
  "__vectool_exclude": "[VECTOOL:EXCLUDE:generated_by_proto:@Proto-Docs]",
  "name": "GeneratedConfig",
  "version": "1.0.0",
  "properties": {
    "field1": "value1"
  }
}
```

**JSON Marker Rules:**
- Member key must be: `__vectool_exclude` (case-sensitive)
- Value is the marker string: `"[VECTOOL:EXCLUDE:reason:@reference]"`
- Must appear in first 50 lines of file
- Regex will match marker pattern inside the string value
- No other comment syntax exists in JSON, so string value is the only option

---

## Phase 4.5.X.1 - File Marker Parser Design

### Deliverables

1. **`IFileMarkerExtractor` interface**
   ```csharp
   public interface IFileMarkerExtractor
   {
       FileMarkerPattern? ExtractMarker(string filePath);
   }
   ```

2. **`FileMarkerPattern` model**
   ```csharp
   public class FileMarkerPattern
   {
       public string FilePath { get; set; }
       public string Reason { get; set; }
       public string? SpaceReference { get; set; }
       public int LineNumber { get; set; }
       public DateTime ExtractedAt { get; set; }
   }
   ```

3. **`FileMarkerExtractor` implementation**
   - Read file header (1500 bytes max)
   - Parse first 50 lines only
   - Apply regex pattern matching (handles comments AND JSON string values)
   - Handle encoding edge cases (UTF-8 with BOM, UTF-16)
   - Return `null` if no marker found
   - Log exceptions with context

### Key Implementation Details

**Performance Target:** < 1ms per file (header read + regex match)

**Header Reading Strategy:**
```csharp
private static string? ReadFileHeader(string filePath, int maxBytes = 1500)
{
    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, 
        FileAccess.Read, bufferSize: 4096);
    using var sr = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
    
    var buffer = new char[maxBytes / 2];
    int charsRead = sr.Read(buffer, 0, buffer.Length);
    
    // Only return content actually read
    return charsRead > 0 ? new string(buffer, 0, charsRead) : null;
}
```

**Regex Pattern:**
```csharp
private static readonly Regex MarkerRegex = new(
    @"\[VECTOOL:EXCLUDE:([^:]+):(@[\w\-]+)?\]",
    RegexOptions.Compiled | RegexOptions.IgnoreCase,
    TimeSpan.FromMilliseconds(100)
);
```

**Regex Matching Examples:**

✅ Matches in all contexts:
```
<!-- [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs] -->     (XML comment)
// [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs]             (C# comment)
# [VECTOOL:EXCLUDE:vendor:@Third-Party]                           (Python comment)
"__vectool_exclude": "[VECTOOL:EXCLUDE:generated:@docs]"         (JSON string value)
[vectool:exclude:reason:@ref]                                    (Any context, case-insensitive)
```

**Bounds Checking:**
```csharp
public FileMarkerPattern? ExtractMarker(string filePath)
{
    try
    {
        string? header = ReadFileHeader(filePath, maxBytes: 1500);
        if (string.IsNullOrEmpty(header)) return null;
        
        // Split into lines and take first 50
        var lines = header.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
        var firstLines = string.Join("\n", lines.Take(50));
        
        var match = MarkerRegex.Match(firstLines);
        if (!match.Success) return null;
        
        // Safe group extraction
        if (!match.Groups[1].Success) return null;
        
        return new FileMarkerPattern
        {
            FilePath = filePath,
            Reason = match.Groups[1].Value,
            SpaceReference = match.Groups[2].Success ? match.Groups[2].Value : null,
            LineNumber = DetermineLineNumber(firstLines, match.Index),
            ExtractedAt = DateTime.UtcNow
        };
    }
    catch (Exception ex)
    {
        using var ctx = log.Ctx.Set(new Props
            .Add("file_path", filePath)
            .Add("error_type", ex.GetType().Name));
        log.Warn($"Failed to extract marker from file: {ex.Message}");
        return null;
    }
}
```

### Testing Focus

- ✅ Marker found in various comment syntaxes (C#, Python, JS, XML)
- ✅ Marker found in JSON string value (`__vectool_exclude`)
- ✅ Marker in XSD files (XML comments)
- ✅ Marker in generated C# files (.g.cs)
- ✅ Marker at different line positions (line 1, line 10, line 50)
- ✅ Marker at boundary positions (exactly 1500 bytes, exactly 50 lines)
- ✅ Missing marker returns `null`
- ✅ Invalid regex returns `null`
- ✅ File read exceptions logged and handled gracefully
- ✅ UTF-8 BOM, UTF-16, mixed encodings handled correctly
- ✅ JSON file with `__vectool_exclude` member extracts correctly
- ✅ JSON file without `__vectool_exclude` returns null

---

## Phase 4.5.X.2 - FileSystemTraverser Integration

### Integration Point

**Existing code location:** `FileSystemTraverser.ProcessFolder()`

**Current Layer 1 logic (pattern-based):**
```csharp
if (primaryMatcher!.IsIgnored(current, isDirectory: true))
{
    // Skip directory
    continue;
}
```

### New Layer 2 Logic (Marker-based)

**Add after Layer 1 pattern check:**
```csharp
// Layer 2: File marker check (only for files, not directories)
if (!isDirectory && _markerExtractor != null)
{
    var marker = _markerExtractor.ExtractMarker(current);
    if (marker != null)
    {
        using var ctx = log.Ctx.Set(new Props
            .Add("source", "file_marker")
            .Add("file_path", current)
            .Add("reason", marker.Reason)
            .Add("space_reference", marker.SpaceReference ?? "none")
            .Add("line_number", marker.LineNumber));
        log.Info("File excluded by marker");
        continue;  // Skip file
    }
}
```

### Constructor Injection

**Update `FileSystemTraverser` constructor:**
```csharp
public FileSystemTraverser(
    IUserInterface? ui,
    IRecentFilesManager? recentFilesManager,
    IFileSystemTraverser? traverser,
    IFileMarkerExtractor? markerExtractor = null)  // ← New parameter
{
    // ... existing code
    _markerExtractor = markerExtractor;
}
```

### Layer 1 vs Layer 2 Consistency

**Align Layer 1 pattern logging to use LogCtx:**
```csharp
// BEFORE
if (primaryMatcher!.IsIgnored(current, isDirectory: true))
{
    log.Trace($"Skipping excluded folder pattern: {current}");
    continue;
}

// AFTER (consistent with Layer 2)
if (primaryMatcher!.IsIgnored(current, isDirectory: true))
{
    using var ctx = log.Ctx.Set(new Props
        .Add("source", "pattern")
        .Add("file_path", current)
        .Add("library", "MAB.DotIgnore")
        .Add("pattern_type", isDirectory ? "folder" : "file"));
    log.Info("File/folder excluded by pattern");
    continue;
}
```

### Performance Implications

**Before optimization:** ~1000ms for 1000 files (patterns only)
**After Layer 2 addition:** ~1100-1150ms for 1000 files (1-1.5ms per marker extraction)

**Optimization:** Cache marker results if re-traversing same file tree
```csharp
private static readonly MemoryCache MarkerCache = new(new MemoryCacheOptions
{
    SizeLimit = 1024  // Store last 1024 marker checks
});

// In ExtractMarker():
if (MarkerCache.TryGetValue(filePath, out var cached))
    return cached as FileMarkerPattern;

// Store result (even if null)
var options = new MemoryCacheEntryOptions()
    .SetSize(1)
    .SetSlidingExpiration(TimeSpan.FromHours(1));
MarkerCache.Set(filePath, result, options);
```

### Testing Focus

- ✅ Layer 1 patterns processed first (backward compatibility)
- ✅ Layer 2 markers only checked for files (not directories)
- ✅ Correct skip order (pattern → marker → process)
- ✅ LogCtx properties logged correctly for SEQ queries
- ✅ Performance remains under 1200ms for 1000 files
- ✅ Cache hits improve repeated traversals
- ✅ XSD files correctly excluded when marked
- ✅ JSON files correctly excluded when marked

---

## Phase 4.5.X.3 - LogCtx Audit Trail

### LogCtx Props Structure

**Define reusable Props object:**
```csharp
public static class ExclusionProps
{
    public static Props CreatePatternProps(string filePath, string library, bool isDirectory)
        => new Props
            .Add("source", "pattern")
            .Add("file_path", filePath)
            .Add("library", library)
            .Add("pattern_type", isDirectory ? "folder" : "file");

    public static Props CreateMarkerProps(FileMarkerPattern marker)
        => new Props
            .Add("source", "file_marker")
            .Add("file_path", marker.FilePath)
            .Add("reason", marker.Reason)
            .Add("space_reference", marker.SpaceReference ?? "none")
            .Add("line_number", marker.LineNumber);

    public static Props CreateProcessedProps(string filePath, long bytesProcessed)
        => new Props
            .Add("source", "processed")
            .Add("file_path", filePath)
            .Add("bytes_processed", bytesProcessed);
}
```

### SEQ Query Examples

**Find all excluded files:**
```
source = "pattern" OR source = "file_marker"
```

**Find files excluded by specific marker reason:**
```
source = "file_marker" AND reason = "generated_by_xsd"
```

**Find all XSD-related exclusions (schema + generated classes):**
```
source = "file_marker" AND space_reference = "@XSD-Schema-Docs"
```

**Find JSON config exclusions:**
```
source = "file_marker" AND reason = "generated_by_proto"
```

**Find exclusions referencing specific documentation:**
```
space_reference = "@XSD-Schema-Docs"
```

**Audit trail for specific file:**
```
file_path = "/path/to/file.cs"
```

### Logging Best Practices

**Use consistent log levels:**
- **`.Trace()`** - Detailed diagnostic (use for Layer 1 patterns - optional in production)
- **`.Info()`** - Exclusion event (Layer 2 markers - always logged)
- **`.Warn()`** - Marker extraction failures (file read errors, encoding issues)
- **`.Error()`** - Unexpected exceptions (should not occur in normal operation)

**Example:**
```csharp
using var ctx = log.Ctx.Set(ExclusionProps.CreateMarkerProps(marker));
log.Info("File excluded by marker");  // Always logged, queryable in SEQ
```

### Testing Focus

- ✅ Props contain all required fields (source, file_path, reason, etc.)
- ✅ Props serialization works correctly (NLog → JSON → SEQ)
- ✅ SEQ queries return expected results
- ✅ No sensitive data in props (paths OK, content not logged)
- ✅ Log levels used correctly (Info for exclusions, Warn for failures)
- ✅ XSD-specific markers logged with `@XSD-Schema-Docs` reference
- ✅ JSON-specific markers logged with correct reason

---

## Phase 4.5.X.4 - Unit Tests

### Test Structure

**File:** `FileMarkerExtractorTests.cs`  
**Framework:** NUnit + Shouldly  
**Pattern:** Arrange-Act-Assert

### Test Categories

#### 1. Marker Extraction - Basic Cases

```csharp
[Test]
public void ExtractMarker_WithValidCSharpMarker_ReturnsMarkerPattern()
{
    // Arrange
    var content = @"// [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs]
public class Generated { }";
    var filePath = CreateTempFile(content, ".cs");
    
    // Act
    var marker = _extractor.ExtractMarker(filePath);
    
    // Assert
    marker.Should().NotBeNull();
    marker!.Reason.Should().Be("generated_by_xsd");
    marker.SpaceReference.Should().Be("@XSD-Schema-Docs");
}

[Test]
public void ExtractMarker_WithPythonMarker_ReturnsMarkerPattern()
{
    // Arrange
    var content = @"# [VECTOOL:EXCLUDE:vendor_library:@Third-Party-Guide]
def external_api(): pass";
    var filePath = CreateTempFile(content, ".py");
    
    // Act
    var marker = _extractor.ExtractMarker(filePath);
    
    // Assert
    marker.Should().NotBeNull();
    marker!.Reason.Should().Be("vendor_library");
}

[Test]
public void ExtractMarker_WithXmlMarker_ReturnsMarkerPattern()
{
    // Arrange
    var content = @"<?xml version=""1.0""?>
<!-- [VECTOOL:EXCLUDE:schema_reference:@XML-Reference] -->
<xs:schema></xs:schema>";
    var filePath = CreateTempFile(content, ".xml");
    
    // Act
    var marker = _extractor.ExtractMarker(filePath);
    
    // Assert
    marker.Should().NotBeNull();
    marker!.Reason.Should().Be("schema_reference");
}

[Test]
public void ExtractMarker_WithOptionalSpaceReference_ReturnsNullForReference()
{
    // Arrange
    var content = "// [VECTOOL:EXCLUDE:auto_generated:]";
    var filePath = CreateTempFile(content, ".cs");
    
    // Act
    var marker = _extractor.ExtractMarker(filePath);
    
    // Assert
    marker.Should().NotBeNull();
    marker!.SpaceReference.Should().BeNull();
}
```

#### 2. XSD-Specific Tests (MANDATORY)

```csharp
[Test]
public void ExtractMarker_FromXsdSchemaFile_ReturnsMarkerPattern()
{
    // Arrange - Real XSD schema header
    var content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<!-- [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs] -->
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
    <xs:element name=""root"" type=""xs:string"" />
</xs:schema>";
    var filePath = CreateTempFile(content, ".xsd");

    // Act
    var marker = _extractor.ExtractMarker(filePath);

    // Assert
    marker.Should().NotBeNull();
    marker!.Reason.Should().Be("generated_by_xsd");
    marker.SpaceReference.Should().Be("@XSD-Schema-Docs");
}

[Test]
public void ExtractMarker_FromXsdGeneratedCsFile_ReturnsMarkerPattern()
{
    // Arrange - C# file generated from XSD
    var content = @"// [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs]
// Auto-generated by xsd.exe

namespace Generated.Schema;

[System.CodeDom.Compiler.GeneratedCode(""xsd"", ""4.8.0"")]
public partial class Person { }";
    var filePath = CreateTempFile(content, ".g.cs");

    // Act
    var marker = _extractor.ExtractMarker(filePath);

    // Assert
    marker.Should().NotBeNull();
    marker.FilePath.Should().Contain(".g.cs");
}
```

#### 3. JSON-Specific Tests (NEW - MANDATORY)

```csharp
[Test]
public void ExtractMarker_FromJsonWithVectoolExcludeString_ReturnsMarkerPattern()
{
    // Arrange - JSON file with __vectool_exclude member
    var content = @"{
  ""__vectool_exclude"": ""[VECTOOL:EXCLUDE:generated_by_proto:@Proto-Docs]"",
  ""name"": ""GeneratedConfig"",
  ""version"": ""1.0.0"",
  ""properties"": {
    ""field1"": ""value1""
  }
}";
    var filePath = CreateTempFile(content, ".json");

    // Act
    var marker = _extractor.ExtractMarker(filePath);

    // Assert
    marker.Should().NotBeNull();
    marker!.Reason.Should().Be("generated_by_proto");
    marker.SpaceReference.Should().Be("@Proto-Docs");
}

[Test]
public void ExtractMarker_FromJsonWithoutVectoolExclude_ReturnsNull()
{
    // Arrange - Regular JSON without marker
    var content = @"{
  ""name"": ""Config"",
  ""version"": ""1.0.0""
}";
    var filePath = CreateTempFile(content, ".json");

    // Act
    var marker = _extractor.ExtractMarker(filePath);

    // Assert
    marker.Should().BeNull();
}

[Test]
public void ExtractMarker_FromJsonVectoolExcludeNotFirstFiftyLines_ReturnsNull()
{
    // Arrange - __vectool_exclude beyond first 50 lines
    var lines = new List<string> { "{ " };
    for (int i = 0; i < 50; i++)
        lines.Add($@"  ""field{i}"": ""value{i}"",");
    lines.Add(@"  ""__vectool_exclude"": ""[VECTOOL:EXCLUDE:test:@docs]""");
    lines.Add("}");
    
    var content = string.Join("\n", lines);
    var filePath = CreateTempFile(content, ".json");

    // Act
    var marker = _extractor.ExtractMarker(filePath);

    // Assert
    marker.Should().BeNull();
}
```

#### 4. Position Boundary Tests

```csharp
[Test]
public void ExtractMarker_At50LineBoundary_ReturnsMarkerPattern()
{
    // Marker at line 50
}

[Test]
public void ExtractMarker_Beyond50Lines_ReturnsNull()
{
    // Marker at line 51
}

[Test]
public void ExtractMarker_At1500ByteBoundary_ReturnsMarkerPattern()
{
    // Marker within 1500 byte header
}

[Test]
public void ExtractMarker_Beyond1500Bytes_ReturnsNull()
{
    // Marker after 1500 bytes
}
```

#### 5. Encoding and Edge Cases

```csharp
[Test]
public void ExtractMarker_WithUtf8Bom_ReturnsMarkerPattern() { }

[Test]
public void ExtractMarker_WithUtf16Encoding_ReturnsMarkerPattern() { }

[Test]
public void ExtractMarker_WithMixedLineEndings_ReturnsMarkerPattern() { }

[Test]
public void ExtractMarker_WithLargeHeader_PerformsUnder1ms() { }
```

#### 6. Failure and Error Handling

```csharp
[Test]
public void ExtractMarker_WithNonExistentFile_ReturnsNull() { }

[Test]
public void ExtractMarker_WithFileReadPermissionDenied_ReturnsNullAndLogsWarning() { }

[Test]
public void ExtractMarker_WithInvalidRegex_ReturnsNull() { }

[Test]
public void ExtractMarker_WithEmptyFile_ReturnsNull() { }
```

### Test Coverage Goals

- ✅ **Critical paths:** 100% coverage (marker extraction logic)
- ✅ **Error handling:** 95%+ coverage (exception cases)
- ✅ **Integration:** 80%+ coverage (FileSystemTraverser integration)
- **Target:** Overall 90%+ line coverage

---

## Phase 4.5.X.5 - Documentation + User Guide

### Deliverables

1. **`GUIDE-FileMarkers-1.0.md`** - End-user documentation
2. **Code templates** - Copy-paste examples for each language
3. **SEQ query examples** - Common audit trail queries
4. **Troubleshooting guide** - Common issues and solutions

### Documentation Outline

#### Section 1: Quick Start

**For XSD Files:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs] -->
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
```

**For XSD-Generated C# Classes:**
```csharp
// [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs]
public partial class Person { }
```

**For JSON Configuration Files:**
```json
{
  "__vectool_exclude": "[VECTOOL:EXCLUDE:generated_by_proto:@Proto-Docs]",
  "config": { }
}
```

#### Section 2: Common Exclusion Reasons

| Reason | Use Case | Documentation | File Types |
| :-- | :-- | :-- | :-- |
| `generated_by_xsd` | XSD-generated files | `@XSD-Schema-Docs` | `.xsd`, `.g.cs` |
| `generated_by_proto` | Protocol Buffer generated | `@Protobuf-Guide` | `.proto`, `.g.cs`, `.json` |
| `generated_by_swagger` | OpenAPI/Swagger generated | `@Swagger-Guide` | `.json`, `.yaml` |
| `vendor_library` | Third-party vendored code | `@Third-Party-Guide` | `.cs`, `.py`, `.js` |
| `auto_generated` | Build tool output | `@Code-Generator-Docs` | All types |
| `temporary_cache` | Build cache files | `@Build-Process` | All types |

#### Section 3: Performance Notes

```
Performance Impact:
- Marker extraction: <1ms per file
- For 1000 files: ~1.1 seconds (vs 1.0 second for patterns only)
- JSON string parsing: < 0.5ms (simple string search)
- Negligible impact on context generation performance
- Cached results improve repeated traversals
```

#### Section 4: Troubleshooting

```
### JSON Marker Not Recognized

Problem: I added __vectool_exclude to JSON but it's not working.

Solutions:
1. Key must be "__vectool_exclude" (exact, case-sensitive)
2. Must be a string value containing [VECTOOL:EXCLUDE:...]
3. Member must be in first 50 lines
4. Member must be in first 1500 bytes
5. Verify JSON is valid (proper quotes, commas)
```

---

## 📋 Success Criteria

### Phase Completion Checklist

- [ ] `FileMarkerExtractor` interface + implementation complete
  - [ ] Handles all supported languages (C#, Python, JS, XML, Go, Rust, YAML)
  - [ ] Handles XSD files (XML comments)
  - [ ] Handles JSON files (string value `__vectool_exclude`)
  - [ ] Respects 1500 byte / 50 line boundaries
  - [ ] Performance < 1ms per file
  - [ ] Error handling tested and logged

- [ ] `FileSystemTraverser` integration complete
  - [ ] Layer 2 markers checked after Layer 1 patterns
  - [ ] Files correctly skipped when marker found
  - [ ] LogCtx properties logged correctly
  - [ ] XSD files excluded when marked
  - [ ] JSON files excluded when marked
  - [ ] Performance impact < 150ms for 1000 files

- [ ] LogCtx audit trail working
  - [ ] Props structure standardized and reusable
  - [ ] SEQ queries working and documented
  - [ ] XSD-specific queries available
  - [ ] JSON-specific queries available
  - [ ] Logging levels used consistently

- [ ] Unit tests comprehensive
  - [ ] 90%+ line coverage achieved
  - [ ] All language syntaxes tested (including XSD, JSON)
  - [ ] XSD marker extraction tested (5+ test cases)
  - [ ] JSON marker extraction tested (5+ test cases)
  - [ ] Edge cases and error paths covered
  - [ ] All tests passing

- [ ] Documentation complete and accurate
  - [ ] User-friendly quick start guide (include XSD + JSON examples)
  - [ ] XSD support prominently featured
  - [ ] JSON string value support documented
  - [ ] Common use cases documented
  - [ ] SEQ query examples provided
  - [ ] Troubleshooting guide helpful

---

## 🚀 Next Phase Preparation

**Phase 4.5.Y - Auto-Levels (Deferred):**
- Token-based detail splitting for large files
- Automatic context trimming within token budgets
- User decision elimination
- Estimated: 2-3 phases in 4.5.Y

**Checkpoints:**
- After Phase 4.5.X.5: Request user acceptance and production readiness review
- After Phase 4.5.Y: Full feature validation and performance testing

---

## 📌 Review Notes (Updated)

**Key adjustments from plan reviews:**

1. ✅ Auto-levels deferred (removed from roadmap, total time: 4.5h)
2. ✅ Header read increased from 500 to 1500 bytes
3. ✅ Line bounds check added (first 50 lines only)
4. ✅ **XSD support elevated to FIRST-CLASS CITIZEN** (not optional)
5. ✅ **JSON support added** (string value: `__vectool_exclude`)
6. ✅ Performance claims clarified (1-1.5ms per marker + pattern matching)
7. ✅ Layer 1/Layer 2 logging unified to use consistent LogCtx props
8. ✅ File marker regex bounds checking added (safe group access)
9. ✅ **XSD test cases added to mandatory test set (5+ tests)**
10. ✅ **JSON test cases added to mandatory test set (5+ tests)**

---

**Document Version:** 1.1 (XSD Mandate + JSON Support)  
**Created:** 2025-11-02  
**Updated:** 2025-11-02 (XSD mandate + JSON support added)  
**Status:** Ready for Phase 4.5.X.1 kickoff  

**Next action:** Await confirmation to begin Phase 4.5.X.1 implementation with XSD + JSON support confirmed.