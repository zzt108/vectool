# 📖 File Markers User Guide - VecTool 4.5.X

**Version:** 1.0  
**Created:** 2025-11-08  
**Status:** Production Ready  
**Target:** VecTool 4.x and later

---

## 🎯 Quick Overview

File markers provide **granular, file-level control** over content visibility during AI context generation. Using a simple comment-based syntax, you can exclude specific files from analysis without modifying ignore patterns.

**Key benefits:**
- ✅ Language-agnostic syntax works in any file type
- ✅ First-class support for XSD schemas and generated code
- ✅ Special JSON string syntax for configuration files
- ✅ Audit trail logging for compliance and debugging
- ✅ Complementary to existing pattern-based exclusions

**Quick syntax:**
```
[VECTOOL:EXCLUDE:reason:@documentation_reference]
```

---

## 📋 Table of Contents

- [Quick Start by File Type](#quick-start-by-file-type)
- [Common Exclusion Reasons](#common-exclusion-reasons)
- [Language-Specific Examples](#language-specific-examples)
- [XSD Schema Support](#xsd-schema-support)
- [JSON Configuration Files](#json-configuration-files)
- [Performance Characteristics](#performance-characteristics)
- [Audit Trail & SEQ Queries](#audit-trail--seq-queries)
- [Troubleshooting](#troubleshooting)
- [Copy-Paste Templates](#copy-paste-templates)

---

## 🚀 Quick Start by File Type

### For XSD Schema Files

**File:** `schemas/Order.xsd`

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs] -->
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
    <!-- Your schema definition -->
    <xs:element name="Order" type="OrderType" />
</xs:schema>
```

**Rationale:** XSD schemas are often auto-generated and large. Excluding them keeps the AI context focused on application logic rather than schema definitions.

---

### For XSD-Generated C# Classes

**File:** `Generated/Order.g.cs`

```csharp
// [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs]
// Auto-generated from Order.xsd by xsd.exe
// DO NOT EDIT MANUALLY

[System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.0")]
public partial class Order
{
    public string OrderId { get; set; }
    public OrderLine[] Lines { get; set; }
}
```

**Rationale:** Generated C# files from XSD contain boilerplate that clutters the AI context. The marker ensures the AI sees your hand-written classes instead.

---

### For Protobuf-Generated Files

**File:** `Generated/models.pb.cs`

```csharp
// [VECTOOL:EXCLUDE:generated_by_proto:@Protobuf-Guide]
// Generated from models.proto by protoc compiler
// DO NOT EDIT MANUALLY

namespace Generated
{
    public partial class User { }
}
```

**Rationale:** Protocol Buffer code is identical across regenerations. Exclusion prevents redundant context.

---

### For JSON Configuration Files

**File:** `config/appsettings.generated.json`

```json
{
  "__vectool_exclude": "[VECTOOL:EXCLUDE:generated_by_swagger:@Swagger-Integration-Docs]",
  "name": "API-Config",
  "version": "1.0.0",
  "endpoints": {
    "userService": "https://api.example.com/users",
    "orderService": "https://api.example.com/orders"
  }
}
```

**Rationale:** Auto-generated JSON configs (from Swagger, code generators, etc.) can be excluded to focus on hand-written configuration.

---

### For Third-Party Vendor Code

**File:** `packages/vendor/ThirdPartyLib.cs`

```csharp
// [VECTOOL:EXCLUDE:vendor_library:@Third-Party-Integration]
// This is third-party code included in source tree
// For reference only - do not modify

namespace VendorLib
{
    public class ExternalAPI { }
}
```

**Rationale:** Vendored code is maintained externally. Excluding it prevents the AI from trying to understand or refactor someone else's implementation.

---

### For Build Cache & Temporary Files

**File:** `.build/temp/compiled.cs`

```csharp
// [VECTOOL:EXCLUDE:temporary_cache:@Build-Process-Documentation]
// This file is auto-generated during build
// It will be regenerated on next build

public class CompiledOutput { }
```

**Rationale:** Build artifacts are transient and don't need AI analysis.

---

## 📊 Common Exclusion Reasons

Use these standardized `reason` values for consistency. This allows audit queries to find similar exclusions across your codebase.

| Reason | Use Case | Documentation | File Types | Example |
|:--|:--|:--|:--|:--|
| `generated_by_xsd` | XSD schema files and generated C# classes | `@XSD-Schema-Docs` | `.xsd`, `.g.cs` | Schema definitions, generated entity classes |
| `generated_by_proto` | Protocol Buffer generated code | `@Protobuf-Guide` | `.proto`, `.pb.cs`, `.json` | Generated service stubs, message classes |
| `generated_by_swagger` | OpenAPI/Swagger generated clients | `@Swagger-Integration-Docs` | `.cs`, `.json`, `.yaml` | Auto-generated API client code |
| `generated_by_graphql` | GraphQL code generator output | `@GraphQL-Docs` | `.cs`, `.ts`, `.go` | Generated types, resolvers |
| `vendor_library` | Third-party vendored source | `@Third-Party-Integration` | `.cs`, `.py`, `.js`, `.go` | Vendored libraries in source tree |
| `auto_generated` | Generic auto-generated code | `@Code-Generator-Docs` | All types | Build tool output, code scaffolding |
| `temporary_cache` | Build caches and temporary files | `@Build-Process-Documentation` | All types | Compiled outputs, temp directories |
| `documentation_generated` | Auto-generated documentation | `@Doc-Generation-Guide` | `.md`, `.html`, `.xml` | Generated README, API docs |
| `test_fixture_generated` | Test fixtures from generators | `@Testing-Guide` | `.cs`, `.py` | Generated test data, mocks |

**Choosing the right reason:**
1. If it matches one of the standard reasons above, use that
2. If you need a custom reason, follow the pattern: `snake_case` with underscores
3. Always use the `@documentation_reference` to link to your project's documentation

---

## 🌐 Language-Specific Examples

### C# and C#-Generated Files

```csharp
// Single-line comment
// [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs]
public class Order { }

// Multi-line comment
/* [VECTOOL:EXCLUDE:vendor_library:@Third-Party-Integration]
   This is vendored code from external library */
public class ExternalAPI { }
```

**Valid locations:**
- In the first line before class definition
- In the first 50 lines of the file
- Must be in first 1500 bytes
- Can be in either single-line (`//`) or multi-line (`/* */`) comments

---

### Python Files

```python
# [VECTOOL:EXCLUDE:generated_by_proto:@Protobuf-Guide]
# Auto-generated from models.proto
# DO NOT EDIT MANUALLY

class UserModel:
    pass
```

**Valid locations:**
- In single-line comments using `#`
- First 50 lines
- First 1500 bytes

---

### JavaScript/TypeScript Files

```javascript
// [VECTOOL:EXCLUDE:generated_by_swagger:@Swagger-Integration-Docs]
// Generated by OpenAPI generator
// DO NOT EDIT MANUALLY

export class UserClient {
    async getUser(id) {
        // API call
    }
}
```

**Valid locations:**
- Single-line comments (`//`)
- Multi-line comments (`/* */`)
- First 50 lines, first 1500 bytes

---

### XML/XSD Files

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs] -->
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
    <!-- Schema content -->
</xs:schema>
```

**Valid locations:**
- XML comment syntax: `<!-- marker -->`
- Must appear before schema content
- First 50 lines, first 1500 bytes

---

### JSON Files

```json
{
  "__vectool_exclude": "[VECTOOL:EXCLUDE:generated_by_swagger:@Swagger-Integration-Docs]",
  "name": "AutoGenerated-Config",
  "version": "1.0.0",
  "settings": {
    "key1": "value1"
  }
}
```

**JSON-Specific Rules:**
- Member key **must** be `__vectool_exclude` (case-sensitive)
- Value is a string containing the marker pattern
- Member should be the first or near the beginning of the file
- Must be in first 50 lines and first 1500 bytes

---

### Java Files

```java
// [VECTOOL:EXCLUDE:generated_by_proto:@Protobuf-Guide]
// Generated from models.proto by protoc compiler

@Generated("protoc")
public class User {
    // Fields
}
```

**Valid locations:**
- Single-line (`//`) or multi-line (`/** */`) comments
- First 50 lines, first 1500 bytes

---

### Go Files

```go
// [VECTOOL:EXCLUDE:generated_by_proto:@Protobuf-Guide]
// Code generated by protoc-gen-go. DO NOT EDIT.

package models

type User struct {
    // Fields
}
```

**Valid locations:**
- Single-line comments (`//`)
- Multi-line comments (`/* */`)
- First 50 lines, first 1500 bytes

---

### Rust Files

```rust
// [VECTOOL:EXCLUDE:auto_generated:@Code-Generator-Docs]
// This file is auto-generated by build.rs
// DO NOT EDIT MANUALLY

pub struct Generated {
    // Fields
}
```

**Valid locations:**
- Line comments (`//`)
- Block comments (`/* */`)
- First 50 lines, first 1500 bytes

---

### YAML Files

```yaml
# [VECTOOL:EXCLUDE:auto_generated:@Config-Generation-Docs]
# This file is auto-generated by the configuration generator
# DO NOT EDIT MANUALLY

name: generated-config
version: 1.0.0
settings:
  key1: value1
```

**Valid locations:**
- YAML comments using `#`
- First 50 lines, first 1500 bytes

---

## 🔧 XSD Schema Support

XSD (XML Schema Definition) is a first-class supported file type. Markers in XSD files follow XML comment syntax.

### XSD Files in Your Project

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs] -->
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
           xmlns:tns="http://example.com/schemas/orders"
           targetNamespace="http://example.com/schemas/orders">
    
    <xs:element name="Order">
        <xs:complexType>
            <xs:sequence>
                <xs:element name="OrderId" type="xs:string" />
                <xs:element name="Lines" type="xs:anyType" />
            </xs:sequence>
        </xs:complexType>
    </xs:element>
    
</xs:schema>
```

### XSD-Generated C# Classes

When you run `xsd.exe Order.xsd /c` or similar tool, it generates C# classes:

```csharp
// [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs]
// Auto-generated from Order.xsd by xsd.exe
// DO NOT EDIT MANUALLY

[System.CodeDom.Compiler.GeneratedCode("xsd", "4.8.0")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class Order
{
    [System.Xml.Serialization.XmlElementAttribute(Order = 0)]
    public string OrderId { get; set; }
    
    [System.Xml.Serialization.XmlElementAttribute(Order = 1)]
    public OrderLine[] Lines { get; set; }
}
```

### Benefits of Excluding XSD Files

1. **Reduces Context Bloat:** XSD files can be very large (hundreds to thousands of lines) but are rarely the focus of AI analysis
2. **Focuses on Application Logic:** By excluding XSD files, the AI context remains focused on your hand-written business logic
3. **Version Control Clarity:** Prevents schema files from cluttering git diffs during refactoring
4. **Performance:** Smaller context means faster AI analysis

### Best Practice: When to Exclude XSD

- ✅ **DO exclude:** Auto-generated `.xsd` files
- ✅ **DO exclude:** XSD-generated C# classes (`.g.cs`)
- ❌ **DON'T exclude:** Hand-written domain models (even if they correspond to XSD)
- ❌ **DON'T exclude:** XSD files that are part of your API specification (these should be versioned and documented)

---

## 📁 JSON Configuration Files

JSON files require special handling since JSON has no comment syntax. Use a string-based marker instead.

### JSON Marker Syntax

```json
{
  "__vectool_exclude": "[VECTOOL:EXCLUDE:reason:@reference]",
  "actual": "configuration"
}
```

### Valid Examples

**Auto-generated Swagger config:**
```json
{
  "__vectool_exclude": "[VECTOOL:EXCLUDE:generated_by_swagger:@Swagger-Integration-Docs]",
  "swagger": "2.0",
  "info": {
    "title": "API Generated by Swagger CodeGen",
    "version": "1.0.0"
  },
  "paths": {
    "/users": {
      "get": {
        "responses": {
          "200": {
            "description": "Success"
          }
        }
      }
    }
  }
}
```

**Auto-generated Protocol Buffer config:**
```json
{
  "__vectool_exclude": "[VECTOOL:EXCLUDE:generated_by_proto:@Protobuf-Guide]",
  "services": [
    {
      "name": "UserService",
      "methods": [
        {
          "name": "GetUser",
          "inputType": "UserId",
          "outputType": "User"
        }
      ]
    }
  ]
}
```

### JSON Rules

| Rule | Description |
|:--|:--|
| **Key name** | Must be exactly `__vectool_exclude` (case-sensitive) |
| **Value format** | String containing the full marker: `"[VECTOOL:EXCLUDE:reason:@ref]"` |
| **Position** | Should be the first or second key in the object (in first 50 lines) |
| **Boundaries** | Must be in first 1500 bytes of file |
| **Validity** | JSON must still be valid after adding the member |

### JSON Marker Best Practices

1. **Place first:** Put `__vectool_exclude` as the first key for clarity
   ```json
   {
     "__vectool_exclude": "[VECTOOL:EXCLUDE:...]",
     "actualConfig": { }
   }
   ```

2. **Use consistent reason:** Reference your standards (see [Common Exclusion Reasons](#common-exclusion-reasons))
   ```json
   {
     "__vectool_exclude": "[VECTOOL:EXCLUDE:generated_by_swagger:@Swagger-Integration-Docs]"
   }
   ```

3. **Keep valid JSON:** Use proper escaping and formatting
   ```json
   {
     "__vectool_exclude": "[VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Docs]",
     "data": "value"
   }
   ```

---

## ⚡ Performance Characteristics

### Extraction Performance

File marker extraction is highly optimized:

| Metric | Value | Notes |
|:--|:--|:--|
| Per-file overhead | < 1ms | Header reading + regex match |
| Batch processing | ~1.1s for 1000 files | Negligible vs pattern matching |
| Additional CPU | < 2% | Compared to pattern-based exclusion |
| Memory per file | ~50 bytes | Cached result (if applicable) |

### When to Use File Markers

**Use file markers when:**
- ✅ You need file-level granularity (patterns are too broad)
- ✅ You want to exclude auto-generated code
- ✅ You need audit logging of exclusion decisions
- ✅ You're working with XSD schemas or generated code

**Use patterns instead when:**
- ✅ Excluding entire directories (`node_modules/`, `.git/`)
- ✅ Excluding file types (`*.o`, `*.exe`)
- ✅ Performance is critical (patterns are slightly faster)

### Caching Strategy

For large projects with repeated traversals, marker results are cached automatically:

- **Cache size:** Up to 1024 files
- **TTL:** 1 hour
- **Benefits:** Repeated exclusion checks on same files are instant (< 0.1ms)

---

## 🔍 Audit Trail & SEQ Queries

All file marker exclusions are logged to your SEQ instance for audit trail and debugging.

### Log Structure

When a file is excluded by marker, the following structured log is generated:

```json
{
  "Timestamp": "2025-11-08T10:30:45.123Z",
  "Level": "Info",
  "MessageTemplate": "File excluded by marker",
  "Properties": {
    "source": "file_marker",
    "file_path": "/src/Generated/Order.g.cs",
    "reason": "generated_by_xsd",
    "space_reference": "@XSD-Schema-Docs",
    "line_number": 1
  }
}
```

### Common SEQ Queries

**Find all file marker exclusions:**
```
source = "file_marker"
```

**Find all XSD-related exclusions:**
```
source = "file_marker" AND reason = "generated_by_xsd"
```

**Find all exclusions with a specific documentation reference:**
```
source = "file_marker" AND space_reference = "@XSD-Schema-Docs"
```

**Find all vendor library exclusions:**
```
source = "file_marker" AND reason = "vendor_library"
```

**Audit exclusions in a specific directory:**
```
source = "file_marker" AND file_path LIKE "%/Generated/%"
```

**Compare pattern vs marker exclusions:**
```
source = "pattern" OR source = "file_marker"
| group by source | count
```

**Find marker extraction errors:**
```
source = "file_marker" AND Level = "Warning"
```

### Sample SEQ Dashboard Query

To create a dashboard showing exclusion statistics:

```
(source = "pattern" OR source = "file_marker")
| group by source | count
| select "@Timestamp, source, Count"
```

This would show:
```
@Timestamp              | source      | Count
2025-11-08T10:30:00Z    | pattern     | 85
2025-11-08T10:30:00Z    | file_marker | 12
```

---

## 🐛 Troubleshooting

### Problem: File Marker Not Recognized

**Symptom:** You added a marker but the file is still being included in context.

**Solution Checklist:**

1. **Verify exact key spelling (JSON only):**
   ```json
   // ❌ WRONG - misspelled key
   {
     "__vectool_excluded": "[VECTOOL:EXCLUDE:test:@docs]"
   }
   
   // ✅ CORRECT - exact spelling
   {
     "__vectool_exclude": "[VECTOOL:EXCLUDE:test:@docs]"
   }
   ```

2. **Check marker position (first 50 lines):**
   ```
   ❌ Marker on line 51 - NOT extracted
   ✅ Marker on line 1-50 - extracted
   ```

3. **Verify within 1500 bytes:**
   - In most cases, markers in the first few lines are within 1500 bytes
   - If file has large header, check byte count:
   ```bash
   # Check first 1500 bytes
   head -c 1500 myfile.cs | wc -c
   ```

4. **Validate marker format:**
   ```
   ✅ [VECTOOL:EXCLUDE:reason:@ref]
   ✅ [vectool:exclude:reason:@ref]        (case-insensitive)
   ❌ [VECTOOL:EXCLUDE reason @ref]        (missing colons)
   ❌ [VECTOOL_EXCLUDE:reason:@ref]        (underscore instead of colon)
   ```

5. **Use SEQ to debug:**
   ```
   file_path LIKE "%yourfile%"
   ```
   If no logs appear, the marker extraction is not running. Check:
   - VecTool configuration includes file markers enabled
   - FileMarkerExtractor is injected in dependency injection
   - Check logs for extraction errors (Level = "Warning")

### Problem: Performance Degradation

**Symptom:** Analysis is slower than before.

**Likely causes:**
1. **Marker extraction not cached:** Markers are extracted on every analysis
   - Solution: Re-run analysis on same files (cache warms up)

2. **Large file headers:** Files with 1500+ byte headers take longer
   - Solution: Move marker to first 100 bytes if possible

3. **Regex timeout:** Unusual characters in marker causing regex slowdown
   - Solution: Use only alphanumeric + underscore in reason: `generated_by_xsd` ✅, not `generated-by-xsd` ❌

### Problem: JSON Marker Breaks Application

**Symptom:** Adding `__vectool_exclude` member breaks the app.

**Possible causes:**

1. **Strict JSON validation:**
   ```json
   // ❌ If your app expects exactly these fields...
   {
     "name": "config",
     "version": "1.0"
   }
   
   // ✅ Add marker carefully
   {
     "__vectool_exclude": "[VECTOOL:EXCLUDE:...]",
     "name": "config",
     "version": "1.0"
   }
   ```

2. **JSON schema validation:**
   - If your JSON is validated against a schema, add `__vectool_exclude` to schema:
   ```json
   {
     "type": "object",
     "properties": {
       "__vectool_exclude": { "type": "string" },
       "name": { "type": "string" }
     },
     "additionalProperties": false
   }
   ```

3. **Application code expectations:**
   - If code expects specific fields, ignore the new member:
   ```csharp
   var config = JsonConvert.DeserializeObject<Config>(json);
   // Application will simply ignore __vectool_exclude member
   ```

### Problem: Inconsistent Exclusions

**Symptom:** The same file is sometimes included, sometimes excluded.

**Likely cause:** Layer 1 pattern exclusion is taking precedence.

**Solution:**
- Check `.gitignore` and VecTool pattern exclusions
- File markers (Layer 2) are checked AFTER pattern exclusions (Layer 1)
- If a file matches a pattern, it's excluded before marker check runs

**Debug in SEQ:**
```
file_path = "/path/to/file.cs" | sort by Timestamp desc
```

This shows all exclusion decisions for that file (both pattern and marker entries).

---

## 📋 Copy-Paste Templates

Quick templates for common scenarios. Copy and paste into your files.

### C# Generated File

```csharp
// [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs]
// Auto-generated from schema
// DO NOT EDIT MANUALLY

namespace Generated;

public partial class GeneratedClass
{
}
```

### Python Generated File

```python
# [VECTOOL:EXCLUDE:auto_generated:@Code-Generator-Docs]
# This file is auto-generated
# DO NOT EDIT MANUALLY

class GeneratedClass:
    pass
```

### JavaScript/TypeScript Generated File

```javascript
// [VECTOOL:EXCLUDE:generated_by_swagger:@Swagger-Integration-Docs]
// Auto-generated by Swagger CodeGen
// DO NOT EDIT MANUALLY

export class GeneratedClient {
}
```

### Java Generated File

```java
// [VECTOOL:EXCLUDE:generated_by_proto:@Protobuf-Guide]
// Generated by protoc-gen-java
// DO NOT EDIT MANUALLY

public class GeneratedMessage {
}
```

### XSD Schema File

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- [VECTOOL:EXCLUDE:generated_by_xsd:@XSD-Schema-Docs] -->
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
</xs:schema>
```

### JSON Configuration File

```json
{
  "__vectool_exclude": "[VECTOOL:EXCLUDE:auto_generated:@Config-Generation-Docs]",
  "name": "config",
  "version": "1.0.0"
}
```

### Go Generated File

```go
// [VECTOOL:EXCLUDE:generated_by_proto:@Protobuf-Guide]
// Code generated by protoc. DO NOT EDIT.

package generated

type Generated struct {
}
```

### Rust Generated File

```rust
// [VECTOOL:EXCLUDE:auto_generated:@Build-Process-Documentation]
// This file is auto-generated
// DO NOT EDIT MANUALLY

pub struct Generated {
}
```

---

## 📞 Getting Help

### Check the FAQ

**Q: Can I use both pattern exclusion and file markers?**  
A: Yes! They work together. Layer 1 (patterns) is checked first, then Layer 2 (markers). You can use both.

**Q: What happens if a file has no marker?**  
A: It's not excluded by Layer 2. Layer 1 patterns still apply.

**Q: Can I exclude directories with markers?**  
A: No, markers only work on individual files. For directories, use pattern exclusion.

**Q: How do I know if a marker is working?**  
A: Check SEQ logs for `source = "file_marker"` queries. If your file appears there, the marker is working.

**Q: Can I have multiple markers in one file?**  
A: Only the first marker found is extracted. If you need multiple reasons, list them in a single marker: `[VECTOOL:EXCLUDE:reason1_and_reason2:@docs]`

---

## 🔗 Related Documentation

- **[GUIDE-1.6-Plan-Phase-Versioning.md](./GUIDE-1.6-Plan-Phase-Versioning.md)** - Phase versioning and implementation standards
- **[LogCtx-1.2.md](./LogCtx-1.2.md)** - Structured logging in VecTool
- **[GUIDE 1.1 LogCtx example pack WinForms .md](./GUIDE%201.1%20LogCtx%20example%20pack%20WinForms%20.md)** - Logging examples

---

## 📝 Changelog

**Version 1.0 (2025-11-08)**
- Initial release
- XSD schema support (first-class citizen)
- JSON string value support (`__vectool_exclude`)
- Common exclusion reasons table
- SEQ query examples
- Comprehensive troubleshooting guide
- Copy-paste templates for all supported languages
- Performance characteristics documented

---

**Document Status:** ✅ Production Ready  
**Last Updated:** 2025-11-08  
**Maintained by:** VecTool Development Team
