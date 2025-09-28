# VecTool.Constants Library

## Purpose
Centralized XML string constants library for VecTool to eliminate magic strings and improve maintainability.

## Architecture

### Namespace Organization
- `Constants.Tags` - Core XML tag constants
- `Constants.TestStrings` - Test-specific constants  
- `Constants.Attributes` - XML attribute name constants
- `Constants.TagBuilder` - Helper for safe tag construction

### Integration Pattern
// OLD: Magic strings 🚫
body.Append($"file name="{fileName}"");

// NEW: Constants ✅
body.Append(TagBuilder.BuildFileNameTag(fileName));

## Usage Guidelines

1. **NEVER** use magic strings for XML tags
2. **ALWAYS** use `TagBuilder` for formatted tags  
3. **TestStrings** only in test projects
4. **Document** new constants with XML docs

## Integration Projects
- DocXHandler.csproj
- OaiUI.csproj  
- UnitTests.csproj
- Core.csproj
