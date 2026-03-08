# **UPDATED PLAN: Enhanced Metadata for MD/DOCX/PDF Exports (v4.7.p4)**

## **🎯 Goal Update**

Add rich file metadata to **all three export formats** (MD, DOCX, PDF):

- **Markdown**: XML wrapper + MD hybrid (AI-optimized)
- **DOCX**: Custom document properties + tables
- **PDF**: PDF metadata + embedded tables

***

## **📐 Format-Specific Metadata Embedding**

### **1. Markdown Export (XML-Markdown Hybrid)**

Same as before - XML structure with markdown content in CDATA.

```xml
<file path="Configuration/IIgnorePatternMatcher.cs" 
      lines="1-28" 
      loc="28" 
      language="csharp" 
      modified="2026-01-03T16:03:16">
  <content><![CDATA[
```csharp
namespace VecTool.Configuration.Exclusion;
public interface IIgnorePatternMatcher : IDisposable { }
```

]]></content>
</file>

```

### **2. DOCX Export (OpenXML Tables + Document Properties)**

**Approach:**
- **Document-level metadata** → Custom Document Properties
- **File-level metadata** → Tables before each code block

```csharp
// In DocxExportHandler.cs - Add document properties
private void AddDocumentMetadata(WordprocessingDocument document, ExportMetadata metadata)
{
    var customProps = document.CustomFilePropertiesPart 
        ?? document.AddCustomFilePropertiesPart();
    
    var properties = new Properties();
    properties.Append(new CustomDocumentProperty
    {
        Name = "TotalFiles",
        VTInt32 = new VTInt32(metadata.TotalFiles.ToString())
    });
    properties.Append(new CustomDocumentProperty
    {
        Name = "TotalLOC",
        VTInt32 = new VTInt32(metadata.TotalLoc.ToString())
    });
    properties.Append(new CustomDocumentProperty
    {
        Name = "ExportDate",
        VTFileTime = new VTFileTime(metadata.ExportDate.ToString("O"))
    });
    
    customProps.Properties = properties;
}

// Before each file - insert metadata table
private void AddFileMetadataTable(Body body, FileMetadata metadata)
{
    var table = new Table();
    
    // Table row: Path | LOC | Language | Modified
    var row = new TableRow();
    row.Append(new TableCell(new Paragraph(new Run(new Text(metadata.Path)))));
    row.Append(new TableCell(new Paragraph(new Run(new Text($"LOC: {metadata.LinesOfCode}")))));
    row.Append(new TableCell(new Paragraph(new Run(new Text($"Lang: {metadata.Language}")))));
    row.Append(new TableCell(new Paragraph(new Run(new Text($"Modified: {metadata.Modified:yyyy-MM-dd}")))));
    
    table.Append(row);
    body.Append(table);
}
```


### **3. PDF Export (QuestPDF Tables + PDF Metadata)**

**Approach:**

- **Document metadata** → PDF document properties
- **File metadata** → Tables in PDF layout

```csharp
// In PdfExportHandler.cs - Add PDF metadata
Document.Create(container =>
{
    container.Page(page =>
    {
        // Document metadata
        page.DocumentMetadata = new DocumentMetadata
        {
            Title = "VecTool Codebase Export",
            Author = "VecTool v4.7.p4",
            Subject = $"{metadata.TotalFiles} files, {metadata.TotalLoc} LOC",
            CreationDate = metadata.ExportDate
        };
        
        page.Content().Column(column =>
        {
            // File metadata table
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Path
                    columns.RelativeColumn(1); // LOC
                    columns.RelativeColumn(1); // Language
                    columns.RelativeColumn(2); // Modified
                });
                
                table.Cell().Text("Path").Bold();
                table.Cell().Text("LOC").Bold();
                table.Cell().Text("Language").Bold();
                table.Cell().Text("Modified").Bold();
                
                table.Cell().Text(fileMetadata.Path);
                table.Cell().Text(fileMetadata.LinesOfCode.ToString());
                table.Cell().Text(fileMetadata.Language);
                table.Cell().Text(fileMetadata.Modified.ToString("yyyy-MM-dd"));
            });
            
            // Then code content
            column.Item().Text(content).FontFamily("Courier New").FontSize(8);
        });
    });
}).GeneratePdf(outputPath);
```


***

## **🏗️ Revised Implementation Plan (5 Phases)**

### **Phase 1: Core Metadata Infrastructure** (Day 1-2)

Same as before - create `FileMetadata`, `ExportMetadata`, `MetadataCollector`.

**New:** Add format-agnostic interfaces

```csharp
namespace VecTool.Export.Metadata;

public interface IMetadataWriter
{
    void WriteDocumentMetadata(ExportMetadata metadata);
    void WriteFileMetadata(FileMetadata metadata, string content);
}
```


***

### **Phase 2: Markdown XML Writer** (Day 3)

Same as before - `XmlMarkdownWriter` implements `IMetadataWriter`.

***

### **Phase 3: DOCX Metadata Writer** (Day 4)

**New file:** `Export.Docx/DocxMetadataWriter.cs`

```csharp
namespace VecTool.Export.Docx;

public sealed class DocxMetadataWriter : IMetadataWriter
{
    private readonly WordprocessingDocument _document;
    private readonly Body _body;
    
    public DocxMetadataWriter(WordprocessingDocument document)
    {
        _document = document;
        _body = document.MainDocumentPart!.Document.Body!;
    }
    
    public void WriteDocumentMetadata(ExportMetadata metadata)
    {
        // Add custom document properties
        var customProps = _document.CustomFilePropertiesPart 
            ?? _document.AddCustomFilePropertiesPart();
        
        var properties = new Properties();
        properties.Append(CreateProperty("TotalFiles", metadata.TotalFiles.ToString()));
        properties.Append(CreateProperty("TotalLOC", metadata.TotalLoc.ToString()));
        properties.Append(CreateProperty("ExportDate", metadata.ExportDate.ToString("O")));
        properties.Append(CreateProperty("Version", metadata.Version));
        
        customProps.Properties = properties;
        
        // Add summary table to document body
        AddSummaryTable(metadata);
    }
    
    public void WriteFileMetadata(FileMetadata metadata, string content)
    {
        // Add metadata table
        var table = CreateMetadataTable(metadata);
        _body.Append(table);
        
        // Add file content
        _body.Append(new Paragraph(new Run(new Text(content)
        {
            Space = SpaceProcessingModeValues.Preserve
        })));
    }
    
    private Table CreateMetadataTable(FileMetadata metadata)
    {
        var table = new Table();
        
        // Header row
        var headerRow = new TableRow();
        headerRow.Append(CreateCell("Path", bold: true));
        headerRow.Append(CreateCell("LOC", bold: true));
        headerRow.Append(CreateCell("Language", bold: true));
        headerRow.Append(CreateCell("Modified", bold: true));
        table.Append(headerRow);
        
        // Data row
        var dataRow = new TableRow();
        dataRow.Append(CreateCell(metadata.Path));
        dataRow.Append(CreateCell(metadata.LinesOfCode.ToString()));
        dataRow.Append(CreateCell(metadata.Language));
        dataRow.Append(CreateCell(metadata.Modified.ToString("yyyy-MM-dd HH:mm")));
        table.Append(dataRow);
        
        return table;
    }
    
    private TableCell CreateCell(string text, bool bold = false)
    {
        var run = new Run(new Text(text));
        if (bold) run.RunProperties = new RunProperties(new Bold());
        
        return new TableCell(new Paragraph(run));
    }
    
    private CustomDocumentProperty CreateProperty(string name, string value)
    {
        return new CustomDocumentProperty
        {
            Name = name,
            VTLPWStr = new VTLPWStr(value)
        };
    }
    
    private void AddSummaryTable(ExportMetadata metadata)
    {
        var summaryTable = new Table();
        
        summaryTable.Append(CreateSummaryRow("Export Date", metadata.ExportDate.ToString("yyyy-MM-dd HH:mm")));
        summaryTable.Append(CreateSummaryRow("Version", metadata.Version));
        summaryTable.Append(CreateSummaryRow("Total Files", metadata.TotalFiles.ToString()));
        summaryTable.Append(CreateSummaryRow("Total LOC", metadata.TotalLoc.ToString()));
        
        _body.Append(summaryTable);
        _body.Append(new Paragraph()); // Spacer
    }
    
    private TableRow CreateSummaryRow(string label, string value)
    {
        var row = new TableRow();
        row.Append(CreateCell(label, bold: true));
        row.Append(CreateCell(value));
        return row;
    }
}
```

**Modified file:** `Export.Docx/DocxExportHandler.cs`

```csharp
public void ConvertSelectedFoldersToDocx(
    List<string> folderPaths, 
    string outputPath, 
    VectorStoreConfig vectorStoreConfig)
{
    using var document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
    var mainPart = document.AddMainDocumentPart();
    mainPart.Document = new Document(new Body());
    
    var metadataWriter = new DocxMetadataWriter(document);
    var metadataCollector = new MetadataCollector(log);
    
    // Collect all files
    var allFiles = new List<(string path, string content)>();
    foreach (var folder in folderPaths)
    {
        var files = traverser.EnumerateFilesRespectingExclusions(folder, vectorStoreConfig);
        foreach (var file in files)
        {
            var content = PathHelpers.SafeReadAllText(file);
            allFiles.Add((file, content));
        }
    }
    
    // Write document metadata
    var exportMetadata = new ExportMetadata
    {
        TotalFiles = allFiles.Count,
        TotalLoc = allFiles.Sum(f => f.content.Split('\n').Length),
        Version = VersionInfo.DisplayVersion
    };
    metadataWriter.WriteDocumentMetadata(exportMetadata);
    
    // Write each file with metadata
    foreach (var (path, content) in allFiles)
    {
        var fileMetadata = metadataCollector.CollectFileMetadata(path, content);
        metadataWriter.WriteFileMetadata(fileMetadata, content);
    }
    
    mainPart.Document.Save();
    
    // Register in recent files
    RecentFilesManager?.RegisterGeneratedFile(...);
}
```


***

### **Phase 4: PDF Metadata Writer** (Day 5)

**New file:** `Export.Pdf/PdfMetadataWriter.cs`

```csharp
namespace VecTool.Export.Pdf;

public sealed class PdfMetadataWriter
{
    public DocumentMetadata CreateDocumentMetadata(ExportMetadata metadata)
    {
        return new DocumentMetadata
        {
            Title = "VecTool Codebase Export",
            Author = $"VecTool {metadata.Version}",
            Subject = $"{metadata.TotalFiles} files, {metadata.TotalLoc} LOC",
            CreationDate = metadata.ExportDate,
            Producer = "VecTool PDF Export Engine",
            Keywords = "codebase,export,AI,LLM"
        };
    }
    
    public void AddMetadataTable(ColumnDescriptor column, FileMetadata metadata)
    {
        column.Item().Table(table =>
        {
            // Define columns
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3); // Path
                columns.RelativeColumn(1); // LOC
                columns.RelativeColumn(1); // Language
                columns.RelativeColumn(2); // Modified
            });
            
            // Header
            table.Cell().Element(CellStyle).Text("Path").Bold();
            table.Cell().Element(CellStyle).Text("LOC").Bold();
            table.Cell().Element(CellStyle).Text("Language").Bold();
            table.Cell().Element(CellStyle).Text("Modified").Bold();
            
            // Data
            table.Cell().Element(CellStyle).Text(metadata.Path).FontSize(8);
            table.Cell().Element(CellStyle).Text(metadata.LinesOfCode.ToString());
            table.Cell().Element(CellStyle).Text(metadata.Language);
            table.Cell().Element(CellStyle).Text(metadata.Modified.ToString("yyyy-MM-dd"));
        });
        
        static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5);
        }
    }
    
    public void AddSummaryTable(ColumnDescriptor column, ExportMetadata metadata)
    {
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1);
                columns.RelativeColumn(2);
            });
            
            AddSummaryRow(table, "Export Date", metadata.ExportDate.ToString("yyyy-MM-dd HH:mm"));
            AddSummaryRow(table, "Version", metadata.Version);
            AddSummaryRow(table, "Total Files", metadata.TotalFiles.ToString());
            AddSummaryRow(table, "Total LOC", metadata.TotalLoc.ToString());
        });
    }
    
    private void AddSummaryRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Element(CellStyle).Text(label).Bold();
        table.Cell().Element(CellStyle).Text(value);
        
        static IContainer CellStyle(IContainer container)
        {
            return container.Padding(5);
        }
    }
}
```

**Modified file:** `Export.Pdf/PdfExportHandler.cs`

```csharp
public void ConvertSelectedFoldersToPdf(
    List<string> folderPaths, 
    string outputPath, 
    VectorStoreConfig vectorStoreConfig)
{
    var metadataWriter = new PdfMetadataWriter();
    var metadataCollector = new MetadataCollector(log);
    
    // Collect files
    var allFiles = new List<(string path, string content)>();
    foreach (var folder in folderPaths)
    {
        var files = traverser.EnumerateFilesRespectingExclusions(folder, vectorStoreConfig);
        foreach (var file in files)
        {
            var content = PathHelpers.SafeReadAllText(file);
            allFiles.Add((file, content));
        }
    }
    
    // Build export metadata
    var exportMetadata = new ExportMetadata
    {
        TotalFiles = allFiles.Count,
        TotalLoc = allFiles.Sum(f => f.content.Split('\n').Length),
        Version = VersionInfo.DisplayVersion
    };
    
    // Generate PDF
    Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            
            // Set document metadata
            page.DocumentMetadata = metadataWriter.CreateDocumentMetadata(exportMetadata);
            
            page.Header()
                .Text("VecTool Codebase Export")
                .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
            
            page.Content().Column(column =>
            {
                // Summary table
                metadataWriter.AddSummaryTable(column, exportMetadata);
                
                column.Item().PaddingVertical(0.5f, Unit.Centimetre);
                
                // Each file with metadata
                foreach (var (path, content) in allFiles)
                {
                    var fileMetadata = metadataCollector.CollectFileMetadata(path, content);
                    
                    // File metadata table
                    metadataWriter.AddMetadataTable(column, fileMetadata);
                    
                    // File content
                    var displayContent = content.Length > 5000 
                        ? content.Substring(0, 5000) + "\n... [truncated] ..." 
                        : content;
                    
                    column.Item()
                        .Border(1)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(5)
                        .Text(displayContent)
                        .FontSize(7)
                        .FontFamily("Courier New");
                    
                    column.Item().PaddingBottom(10); // Spacer
                }
            });
            
            page.Footer()
                .AlignCenter()
                .Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
        });
    }).GeneratePdf(outputPath);
    
    // Register
    RecentFilesManager?.RegisterGeneratedFile(...);
}
```


***

### **Phase 5: Testing \& Validation** (Day 6-7)

#### **5.1 Markdown Tests** (existing + enhanced)

```csharp
[Test]
public void MarkdownExport_ShouldIncludeXmlMetadata()
{
    // ... existing test
    var xml = XDocument.Load(outputPath);
    xml.Descendants("metadata").ShouldNotBeEmpty();
    xml.Descendants("file").First().Attribute("loc").ShouldNotBeNull();
}
```


#### **5.2 DOCX Tests**

```csharp
[Test]
public void DocxExport_ShouldIncludeCustomProperties()
{
    // Arrange & Act
    handler.ConvertSelectedFoldersToDocx(...);
    
    // Assert
    using var doc = WordprocessingDocument.Open(outputPath, false);
    var customProps = doc.CustomFilePropertiesPart?.Properties;
    
    customProps.ShouldNotBeNull();
    customProps.ChildElements
        .OfType<CustomDocumentProperty>()
        .Any(p => p.Name == "TotalFiles")
        .ShouldBeTrue();
}

[Test]
public void DocxExport_ShouldIncludeMetadataTables()
{
    handler.ConvertSelectedFoldersToDocx(...);
    
    using var doc = WordprocessingDocument.Open(outputPath, false);
    var tables = doc.MainDocumentPart.Document.Body.Descendants<Table>();
    
    tables.Count().ShouldBeGreaterThan(0); // At least summary table
}
```


#### **5.3 PDF Tests**

```csharp
[Test]
public void PdfExport_ShouldIncludeMetadata()
{
    handler.ConvertSelectedFoldersToPdf(...);
    
    File.Exists(outputPath).ShouldBeTrue();
    
    // Basic check - PDF should be larger with metadata
    var fileInfo = new FileInfo(outputPath);
    fileInfo.Length.ShouldBeGreaterThan(1000);
}

[Test]
public void PdfExport_ShouldNotCrashWithLargeFiles()
{
    // Create large file (10k lines)
    var largeContent = string.Join("\n", Enumerable.Range(1, 10000).Select(i => $"Line {i}"));
    File.WriteAllText(Path.Combine(testDir, "large.cs"), largeContent);
    
    Should.NotThrow(() => handler.ConvertSelectedFoldersToPdf(...));
}
```


***

## **📊 Deliverables (Updated)**

### **Phase 1:**

- [ ] `Core/Metadata/FileMetadata.cs`
- [ ] `Core/Metadata/ExportMetadata.cs`
- [ ] `Core/Metadata/MetadataCollector.cs`
- [ ] `Export/Metadata/IMetadataWriter.cs`


### **Phase 2:**

- [ ] `Handlers/Export/XmlMarkdownWriter.cs` (MD)


### **Phase 3:**

- [ ] `Export.Docx/DocxMetadataWriter.cs`
- [ ] Modified `Export.Docx/DocxExportHandler.cs`


### **Phase 4:**

- [ ] `Export.Pdf/PdfMetadataWriter.cs`
- [ ] Modified `Export.Pdf/PdfExportHandler.cs`


### **Phase 5:**

- [ ] `UnitTests/Export/MarkdownExportMetadataTests.cs`
- [ ] `UnitTests/Export.Docx/DocxMetadataTests.cs`
- [ ] `UnitTests/Export.Pdf/PdfMetadataTests.cs`

***

## **🎯 Success Criteria (All Formats)**

✅ **Markdown:**

```
- Valid XML with `<metadata>` and `<file>` nodes
```

- Each file has `path`, `lines`, `loc`, `language` attributes
- Content in CDATA with markdown fences

✅ **DOCX:**

- Custom document properties (`TotalFiles`, `TotalLOC`, etc.)
- Summary table at document start
- Metadata table before each file's code block

✅ **PDF:**

- PDF document metadata (Title, Author, Subject, CreationDate)
- Summary table on first page
- Metadata table before each file's code

✅ **Tests:**

- All existing tests pass
- New metadata tests verify format-specific output

***
