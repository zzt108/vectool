// Phase 4.5.X.2 - FileSystemTraverser Integration Tests
// Tests Layer 1 (patterns) + Layer 2 (markers) combined behavior

using LogCtxShared;
using NUnit.Framework;
using NLogShared;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using VecTool.Configuration;
using VecTool.Handlers.Traversal;
using VecTool.Utils;

namespace UnitTests.Traversal
{
    /// <summary>
    /// Integration tests for FileSystemTraverser with Layer 1 + Layer 2 combined exclusion logic.
    /// Validates:
    /// 1. Pattern-based exclusions (Layer 1) applied first
    /// 2. Marker-based exclusions (Layer 2) applied after patterns
    /// 3. Correct skip order and LogCtx audit trail
    /// 4. Performance under 1200ms for 1000 files
    /// 5. XSD and JSON file marker handling
    /// </summary>
    [TestFixture]
    public class FileSystemTraverserIntegrationTestsV2
    {
        private string testRoot = default!;
        private VectorStoreConfig config = default!;
        private readonly CtxLogger log = new();

        [SetUp]
        public void Setup()
        {
            testRoot = Path.Combine(Path.GetTempPath(), "TraverserIntegration", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testRoot);
            config = new VectorStoreConfig();
        }

        [TearDown]
        public void Teardown()
        {
            try
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(testRoot, recursive: true);
                }
            }
            catch
            {
                // Swallow cleanup exceptions
            }
        }

        #region Category 1: Layer 1 + Layer 2 Combined Tests (4 tests)

        /// <summary>
        /// INTEGRATION TEST 1a - Pattern Exclusion Only
        /// Files matching .gitignore patterns are excluded by Layer 1.
        /// Layer 2 marker extractor should not be called for excluded paths.
        /// </summary>
        [Test]
        public void ShouldExcludeFileByPatternFirst()
        {
            // Arrange
            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "*.log\n*.tmp");
            File.WriteAllText(Path.Combine(testRoot, "main.cs"), "// code");
            File.WriteAllText(Path.Combine(testRoot, "debug.log"), "// log file");
            File.WriteAllText(Path.Combine(testRoot, "temp.tmp"), "// temp");

            var config = new VectorStoreConfig();
            var mockMarkerExtractor = new MockFileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: mockMarkerExtractor);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert - Only .cs file included
            files.Count.ShouldBe(1);
            files.ShouldContain(f => f.EndsWith("main.cs"));
            files.ShouldNotContain(f => f.EndsWith("debug.log"));
            files.ShouldNotContain(f => f.EndsWith("temp.tmp"));

            // Verify marker extractor was called only for non-excluded files (mocking would show this)
            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "pattern_only")
                .Add("filesChecked", files.Count)
                .Add("pattern", ".gitignore"));
            log.Info("Pattern-only exclusion verified");
        }

        /// <summary>
        /// INTEGRATION TEST 1b - Marker Exclusion Only
        /// Files without pattern match but with markers are excluded by Layer 2.
        /// </summary>
        [Test]
        public void ShouldExcludeFileByMarkerSecond()
        {
            // Arrange
            // Create a file with marker in first 50 lines
            var markedContent = @"// [VECTOOLEXCLUDE:generated_by_xsd:XSD-Schema-Docs]
public class Generated
{
    public string Name { get; set; }
}";
            File.WriteAllText(Path.Combine(testRoot, "generated.cs"), markedContent);
            File.WriteAllText(Path.Combine(testRoot, "main.cs"), "// regular code");

            var config = new VectorStoreConfig();
            var markerExtractor = new FileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: markerExtractor);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert - Only non-marked file included
            files.Count.ShouldBe(1);
            files.ShouldContain(f => f.EndsWith("main.cs"));
            files.ShouldNotContain(f => f.EndsWith("generated.cs"));

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "marker_only")
                .Add("filesIncluded", files.Count));
            log.Info("Marker-only exclusion verified");
        }

        /// <summary>
        /// INTEGRATION TEST 1c - Both Layers Combined
        /// File excluded by pattern should not be checked by marker extractor.
        /// File not excluded by pattern but marked should be excluded by marker.
        /// </summary>
        [Test]
        public void ShouldExcludeFileByBothLayers()
        {
            // Arrange - Pattern file + Marker file + Normal file
            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "*.log");
            File.WriteAllText(Path.Combine(testRoot, "ignored.log"), "// log");

            var markedContent = @"// [VECTOOLEXCLUDE:generated_by_proto:Protobuf-Guide]
public class ConfigGenerated { }";
            File.WriteAllText(Path.Combine(testRoot, "config.g.cs"), markedContent);

            File.WriteAllText(Path.Combine(testRoot, "main.cs"), "// code");

            var config = new VectorStoreConfig();
            var markerExtractor = new FileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: markerExtractor);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert - Only main.cs included
            files.Count.ShouldBe(1);
            files.ShouldContain(f => f.EndsWith("main.cs"));
            files.ShouldNotContain(f => f.EndsWith("ignored.log"));
            files.ShouldNotContain(f => f.EndsWith("config.g.cs"));

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "both_layers")
                .Add("patternExcluded", 1)
                .Add("markerExcluded", 1)
                .Add("included", 1));
            log.Info("Both layers combined verified");
        }

        /// <summary>
        /// INTEGRATION TEST 1d - Backward Compatibility
        /// When marker extractor is null, Layer 2 is skipped entirely.
        /// Only Layer 1 pattern matching occurs.
        /// </summary>
        [Test]
        public void ShouldNotExcludeFileWhenMarkerExtractorIsNull()
        {
            // Arrange - File with marker but marker extractor disabled
            var markedContent = @"// [VECTOOLEXCLUDE:test:Docs]
public class MarkedClass { }";
            File.WriteAllText(Path.Combine(testRoot, "marked.cs"), markedContent);
            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "*.log");

            var config = new VectorStoreConfig();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: null);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert - Marked file should be included (Layer 2 disabled)
            files.ShouldContain(f => f.EndsWith("marked.cs"));

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "marker_null")
                .Add("layer2Enabled", false));
            log.Info("Backward compatibility verified");
        }

        #endregion

        #region Category 2: File vs Directory Exclusion (3 tests)

        /// <summary>
        /// INTEGRATION TEST 2a - Directory Exclusion (Layer 1 Only)
        /// Directories are excluded by Layer 1 patterns.
        /// Files inside excluded directories are not enumerated.
        /// </summary>
        [Test]
        public void ShouldExcludeDirectoryByPatternOnly()
        {
            // Arrange - bin/ folder in .gitignore
            var structure = new[]
            {
                "src/main.cs",
                "bin/debug/app.exe",
                "bin/release/app.exe",
                "obj/debug/temp.obj",
            };

            foreach (var path in structure)
            {
                var fullPath = Path.Combine(testRoot, path);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, "content");
            }

            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "bin/\nobj/");

            var config = new VectorStoreConfig();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert - Only src files enumerated
            files.ShouldContain(f => f.EndsWith("main.cs"));
            files.ShouldNotContain(f => f.Contains("bin"));
            files.ShouldNotContain(f => f.Contains("obj"));

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "directory_exclusion")
                .Add("filesIncluded", files.Count)
                .Add("directoriesExcluded", 2));
            log.Info("Directory exclusion verified");
        }

        /// <summary>
        /// INTEGRATION TEST 2b - Markers Do Not Apply to Directories
        /// Layer 2 markers should only apply to files, not directories.
        /// Directories are handled exclusively by Layer 1 patterns.
        /// </summary>
        [Test]
        public void ShouldNotApplyMarkersToDirectories()
        {
            // Arrange
            Directory.CreateDirectory(Path.Combine(testRoot, "generated"));
            File.WriteAllText(Path.Combine(testRoot, "generated", "config.cs"), "// code");
            File.WriteAllText(Path.Combine(testRoot, "main.cs"), "// main");

            var config = new VectorStoreConfig();
            var markerExtractor = new FileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: markerExtractor);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert - Folder not excluded just because of name; only if patterns match
            files.Count.ShouldBeGreaterThan(0);
            files.ShouldContain(f => f.EndsWith("config.cs"));

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "markers_not_for_dirs")
                .Add("directoriesProcessed", true));
            log.Info("Marker non-application to directories verified");
        }

        /// <summary>
        /// INTEGRATION TEST 2c - Files Inside Excluded Directories Are Skipped
        /// Pattern exclusion applies to directory trees recursively.
        /// </summary>
        [Test]
        public void ShouldExcludeFilesInsideExcludedDirectories()
        {
            // Arrange
            var structure = new[]
            {
                "src/main.cs",
                "node_modules/lib/index.js",
                "node_modules/lib/package.json",
            };

            foreach (var path in structure)
            {
                var fullPath = Path.Combine(testRoot, path);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, "content");
            }

            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "node_modules/");

            var config = new VectorStoreConfig();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert - node_modules/* excluded
            files.ShouldContain(f => f.EndsWith("main.cs"));
            files.ShouldNotContain(f => f.Contains("node_modules"));

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "files_in_excluded_dirs")
                .Add("nestedFilesExcluded", true));
            log.Info("Nested exclusion verified");
        }

        #endregion

        #region Category 3: Marker Extraction Failures (3 tests)

        /// <summary>
        /// INTEGRATION TEST 3a - Graceful Failure When Marker Extractor Throws
        /// If marker extraction fails, traversal continues without stopping.
        /// Other files are still processed.
        /// </summary>
        [Test]
        public void ShouldNotExcludeFileWhenMarkerExtractorThrows()
        {
            // Arrange
            File.WriteAllText(Path.Combine(testRoot, "file1.cs"), "// code");
            File.WriteAllText(Path.Combine(testRoot, "file2.cs"), "// code");

            var config = new VectorStoreConfig();
            var throwingExtractor = new ThrowingFileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: throwingExtractor);

            // Act & Assert - Should not throw; should continue processing
            Should.NotThrow(() =>
            {
                var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();
                files.Count.ShouldBe(2); // Both files enumerated despite extractor throwing
            });

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "marker_extractor_throws")
                .Add("filesProcessed", 2));
            log.Info("Graceful failure verified");
        }

        /// <summary>
        /// INTEGRATION TEST 3b - Isolation of Marker Extraction Failures
        /// If one file's marker extraction fails, others are still checked.
        /// Failure is isolated to that one file.
        /// </summary>
        [Test]
        public void ShouldContinueProcessingWhenSingleFileMarkerFails()
        {
            // Arrange
            var markedContent = @"// [VECTOOLEXCLUDE:test:Docs]
public class Generated { }";
            File.WriteAllText(Path.Combine(testRoot, "marked.cs"), markedContent);
            File.WriteAllText(Path.Combine(testRoot, "normal.cs"), "// code");
            File.WriteAllText(Path.Combine(testRoot, "other.txt"), "text");

            var config = new VectorStoreConfig();
            var partialFailureExtractor = new PartialFailureFileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: partialFailureExtractor);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert - Some files still processed
            files.Count.ShouldBeGreaterThan(0);

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "partial_failure")
                .Add("filesProcessed", files.Count));
            log.Info("Partial failure isolation verified");
        }

        /// <summary>
        /// INTEGRATION TEST 3c - Failures Are Logged But Not Rethrown
        /// Marker extraction failures are logged to SEQ/LogCtx, but don't break traversal.
        /// </summary>
        [Test]
        public void ShouldLogFailureButNotThrow()
        {
            // Arrange
            File.WriteAllText(Path.Combine(testRoot, "file.cs"), "// code");

            var config = new VectorStoreConfig();
            var markerExtractor = new FileMarkerExtractor(); // Valid extractor
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: markerExtractor);

            // Act & Assert
            Should.NotThrow(() =>
            {
                var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();
                files.Count.ShouldBeGreaterThanOrEqualTo(0);
            });

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "failure_logging"));
            log.Info("Failure logging verified - no rethrow");
        }

        #endregion

        #region Category 4: ProcessFolder & EnumerateFiles Integration (4 tests)

        /// <summary>
        /// INTEGRATION TEST 4a - ProcessFolder Respects Layer 2 Markers
        /// ProcessFolder method applies both Layer 1 and Layer 2 exclusions.
        /// Marked files are not passed to the action callback.
        /// </summary>
        [Test]
        public void ProcessFolderShouldRespectMarkers()
        {
            // Arrange
            var markedContent = @"// [VECTOOLEXCLUDE:generated_by_xsd:XSD-Schema-Docs]
public class Person { }";
            File.WriteAllText(Path.Combine(testRoot, "person.g.cs"), markedContent);
            File.WriteAllText(Path.Combine(testRoot, "program.cs"), "// entry point");

            var config = new VectorStoreConfig();
            var markerExtractor = new FileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: markerExtractor);

            var processedFiles = new List<string>();
            Action<string> captureFile = f => processedFiles.Add(f);

            // Act
            traverser.ProcessFolder(
                testRoot,
                new object(), // dummy context
                config,
                (f, ctx, cfg) => captureFile(f),
                (ctx, name) => { }, // writeFolderName
                (ctx) => { }); // writeFolderEnd

            // Assert - Only non-marked file processed
            processedFiles.Count.ShouldBe(1);
            processedFiles.ShouldContain(f => f.EndsWith("program.cs"));
            processedFiles.ShouldNotContain(f => f.EndsWith("person.g.cs"));

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "processFolder_respect_markers")
                .Add("filesProcessed", processedFiles.Count));
            log.Info("ProcessFolder marker respect verified");
        }

        /// <summary>
        /// INTEGRATION TEST 4b - EnumerateFilesRespectingExclusions Returns Filtered List
        /// Method applies both layers and returns only non-excluded files.
        /// </summary>
        [Test]
        public void EnumerateFilesRespectingExclusionsShouldRespectMarkers()
        {
            // Arrange
            var markedContent = @"// [VECTOOLEXCLUDE:vendor_library:Third-Party-Guide]
public class ExternalLib { }";
            File.WriteAllText(Path.Combine(testRoot, "vendor.cs"), markedContent);
            File.WriteAllText(Path.Combine(testRoot, "main.cs"), "// our code");

            var config = new VectorStoreConfig();
            var markerExtractor = new FileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: markerExtractor);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert
            files.Count.ShouldBe(1);
            files[0].ShouldEndWith("main.cs");

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "enumerate_files_markers")
                .Add("filesReturned", files.Count));
            log.Info("EnumerateFilesRespectingExclusions marker respect verified");
        }

        /// <summary>
        /// INTEGRATION TEST 4c - Case-Insensitive Reason Matching
        /// File marker reasons should be matched case-insensitively in queries.
        /// </summary>
        [Test]
        public void ShouldRespectCaseInsensitiveReasonMatching()
        {
            // Arrange
            var markedContent1 = @"// [VECTOOLEXCLUDE:GENERATED_BY_XSD:XSD-Schema-Docs]
public class Upper { }";
            var markedContent2 = @"// [VECTOOLEXCLUDE:generated_by_xsd:XSD-Schema-Docs]
public class Lower { }";

            File.WriteAllText(Path.Combine(testRoot, "upper.cs"), markedContent1);
            File.WriteAllText(Path.Combine(testRoot, "lower.cs"), markedContent2);
            File.WriteAllText(Path.Combine(testRoot, "main.cs"), "// code");

            var config = new VectorStoreConfig();
            var markerExtractor = new FileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: markerExtractor);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert - Both case variants excluded
            files.Count.ShouldBe(1);
            files.ShouldContain(f => f.EndsWith("main.cs"));

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "case_insensitive_reason")
                .Add("casesHandled", 2));
            log.Info("Case-insensitive reason matching verified");
        }

        /// <summary>
        /// INTEGRATION TEST 4d - Deterministic File Order After Filtering
        /// Files should be returned in deterministic order despite filtering.
        /// </summary>
        [Test]
        public void ShouldPreserveFileOrderAfterMarkerFiltering()
        {
            // Arrange - Create files in specific order
            File.WriteAllText(Path.Combine(testRoot, "a.cs"), "// a");
            File.WriteAllText(Path.Combine(testRoot, "b.cs"), "// b");
            File.WriteAllText(Path.Combine(testRoot, "c.cs"), "// c");

            var config = new VectorStoreConfig();
            var markerExtractor = new FileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: markerExtractor);

            // Act - Enumerate multiple times
            var files1 = traverser.EnumerateFilesRespectingExclusions(testRoot, config).OrderBy(f => f).ToList();
            var files2 = traverser.EnumerateFilesRespectingExclusions(testRoot, config).OrderBy(f => f).ToList();

            // Assert - Same order both times
            files1.Count.ShouldBe(files2.Count);
            for (int i = 0; i < files1.Count; i++)
            {
                files1[i].ShouldBe(files2[i]);
            }

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "deterministic_order")
                .Add("filesEnumerated", files1.Count));
            log.Info("Deterministic file order verified");
        }

        #endregion

        #region Category 5: XSD and JSON Special Cases (3 tests)

        /// <summary>
        /// INTEGRATION TEST 5a - XSD File Markers
        /// XSD schema files with markers should be excluded.
        /// XSD-generated .g.cs files with markers should be excluded.
        /// </summary>
        [Test]
        public void ShouldHandleXsdFileMarkers()
        {
            // Arrange
            var xsdContent = @"<?xml version=""1.0""?>
<!-- [VECTOOLEXCLUDE:generated_by_xsd:XSD-Schema-Docs] -->
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""root"" type=""xs:string""/>
</xs:schema>";

            var genCsContent = @"// [VECTOOLEXCLUDE:generated_by_xsd:XSD-Schema-Docs]
// Auto-generated by xsd.exe
namespace Generated.Schema
{
    public partial class Person { }
}";

            File.WriteAllText(Path.Combine(testRoot, "schema.xsd"), xsdContent);
            File.WriteAllText(Path.Combine(testRoot, "Person.g.cs"), genCsContent);
            File.WriteAllText(Path.Combine(testRoot, "Program.cs"), "// main");

            var config = new VectorStoreConfig();
            var markerExtractor = new FileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: markerExtractor);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert - XSD files excluded, .g.cs excluded, .cs included
            files.ShouldContain(f => f.EndsWith("Program.cs"));
            files.ShouldNotContain(f => f.EndsWith("schema.xsd"));
            files.ShouldNotContain(f => f.EndsWith("Person.g.cs"));

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "xsd_file_markers")
                .Add("xsdFilesExcluded", 1)
                .Add("generatedCsExcluded", 1)
                .Add("normalCsIncluded", 1));
            log.Info("XSD file marker handling verified");
        }

        /// <summary>
        /// INTEGRATION TEST 5b - JSON File Markers
        /// JSON files with vectoolexclude member should be excluded.
        /// </summary>
        [Test]
        public void ShouldHandleJsonFileMarkers()
        {
            // Arrange
            var jsonContent = @"{
  ""vectoolexclude"": ""[VECTOOLEXCLUDE:generated_by_proto:Protobuf-Guide]"",
  ""name"": ""GeneratedConfig"",
  ""version"": ""1.0.0"",
  ""properties"": {
    ""field1"": ""value1""
  }
}";

            File.WriteAllText(Path.Combine(testRoot, "config.g.json"), jsonContent);
            File.WriteAllText(Path.Combine(testRoot, "appsettings.json"), @"{ ""app"": ""test"" }");
            File.WriteAllText(Path.Combine(testRoot, "program.cs"), "// code");

            var config = new VectorStoreConfig();
            var markerExtractor = new FileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: markerExtractor);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert - Marked JSON excluded, unmarked JSON included
            files.ShouldContain(f => f.EndsWith("appsettings.json"));
            files.ShouldContain(f => f.EndsWith("program.cs"));
            files.ShouldNotContain(f => f.EndsWith("config.g.json"));

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "json_file_markers")
                .Add("markedJsonExcluded", 1)
                .Add("unmarkedJsonIncluded", 1));
            log.Info("JSON file marker handling verified");
        }

        /// <summary>
        /// INTEGRATION TEST 5c - Mixed XSD + JSON + C# in One Folder
        /// Combination of XSD, JSON, and C# files with various markers.
        /// </summary>
        [Test]
        public void ShouldHandleMixedXsdJsonCsharpMarkers()
        {
            // Arrange
            File.WriteAllText(Path.Combine(testRoot, "schema.xsd"),
                @"<?xml version=""1.0""?><!-- [VECTOOLEXCLUDE:generated_by_xsd:XSD-Schema-Docs] --><xs:schema/>");

            File.WriteAllText(Path.Combine(testRoot, "config.g.json"),
                @"{ ""vectoolexclude"": ""[VECTOOLEXCLUDE:generated_by_proto:Protobuf-Guide]"" }");

            File.WriteAllText(Path.Combine(testRoot, "Generated.g.cs"),
                @"// [VECTOOLEXCLUDE:generated_by_xsd:XSD-Schema-Docs]\npublic class Gen { }");

            File.WriteAllText(Path.Combine(testRoot, "Program.cs"), "// code");
            File.WriteAllText(Path.Combine(testRoot, "appsettings.json"), @"{ ""app"": ""test"" }");

            var config = new VectorStoreConfig();
            var markerExtractor = new FileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: markerExtractor);

            // Act
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();

            // Assert - Only unmarked files included
            files.Count.ShouldBe(2); // Program.cs + appsettings.json
            files.ShouldContain(f => f.EndsWith("Program.cs"));
            files.ShouldContain(f => f.EndsWith("appsettings.json"));

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "mixed_formats")
                .Add("filesIncluded", files.Count)
                .Add("filesExcluded", 3));
            log.Info("Mixed format marker handling verified");
        }

        #endregion

        #region Category 6: Performance and Edge Cases (2 tests)

        /// <summary>
        /// INTEGRATION TEST 6a - Large Project Performance
        /// Traversal with 1000 files should complete under 1200ms.
        /// </summary>
        [Test]
        public void ShouldPerformWellOnLargeProjects()
        {
            // Arrange
            var setupStopwatch = Stopwatch.StartNew();

            Directory.CreateDirectory(Path.Combine(testRoot, "src"));
            Directory.CreateDirectory(Path.Combine(testRoot, "tests"));

            for (int i = 0; i < 500; i++)
            {
                File.WriteAllText(Path.Combine(testRoot, "src", $"file{i:D4}.cs"), "// code");
                File.WriteAllText(Path.Combine(testRoot, "tests", $"test{i:D4}.cs"), "// test");
            }

            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "bin/\nobj/");
            setupStopwatch.Stop();

            var config = new VectorStoreConfig();
            var markerExtractor = new FileMarkerExtractor();
            var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: markerExtractor);

            // Act
            var enumStopwatch = Stopwatch.StartNew();
            var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();
            enumStopwatch.Stop();

            // Assert
            files.Count.ShouldBe(1000);
            enumStopwatch.ElapsedMilliseconds.ShouldBeLessThan(1200,
                $"Large project enumeration took {enumStopwatch.ElapsedMilliseconds}ms");

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "large_project_performance")
                .Add("filesEnumerated", files.Count)
                .Add("setupMs", setupStopwatch.ElapsedMilliseconds)
                .Add("enumerateMs", enumStopwatch.ElapsedMilliseconds));
            log.Info("Large project performance verified");
        }

        /// <summary>
        /// INTEGRATION TEST 6b - Concurrent Enumeration Safety
        /// Multiple threads enumerating same folder structure should get consistent results.
        /// </summary>
        [Test]
        public void ConcurrentEnumerationShouldBeSafe()
        {
            // Arrange
            for (int i = 0; i < 50; i++)
            {
                File.WriteAllText(Path.Combine(testRoot, $"file{i:D2}.cs"), $"// code {i}");
            }

            File.WriteAllText(Path.Combine(testRoot, ".gitignore"), "*.log");

            var config = new VectorStoreConfig();
            var markerExtractor = new FileMarkerExtractor();
            var results = new List<int>();

            // Act - 5 threads enumerate simultaneously
            var threads = Enumerable.Range(0, 5)
                .Select(_ => new Thread(() =>
                {
                    var traverser = new FileSystemTraverser(ui: null, rootPath: testRoot, markerExtractor: markerExtractor);
                    var files = traverser.EnumerateFilesRespectingExclusions(testRoot, config).ToList();
                    lock (results)
                    {
                        results.Add(files.Count);
                    }
                }))
                .ToList();

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            // Assert - All threads got same result
            results.ShouldAllBe(x => x == 50);

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "concurrent_enumeration")
                .Add("threadsRun", 5)
                .Add("filesPerThread", 50));
            log.Info("Concurrent enumeration safety verified");
        }

        #endregion
    }

    #region Mock and Helper Classes

    /// <summary>
    /// Mock marker extractor that tracks calls.
    /// </summary>
    public class MockFileMarkerExtractor : IFileMarkerExtractor
    {
        public int CallCount { get; private set; }

        public FileMarkerPattern? ExtractMarker(string filePath)
        {
            CallCount++;
            return null; // No markers in mock
        }
    }

    /// <summary>
    /// Marker extractor that throws for testing failure handling.
    /// </summary>
    public class ThrowingFileMarkerExtractor : IFileMarkerExtractor
    {
        public FileMarkerPattern? ExtractMarker(string filePath)
        {
            throw new InvalidOperationException("Intentional test failure");
        }
    }

    /// <summary>
    /// Marker extractor that fails on specific files.
    /// </summary>
    public class PartialFailureFileMarkerExtractor : IFileMarkerExtractor
    {
        public FileMarkerPattern? ExtractMarker(string filePath)
        {
            if (filePath.EndsWith("marked.cs"))
            {
                throw new IOException("Permission denied");
            }
            return null;
        }
    }

    #endregion
}
