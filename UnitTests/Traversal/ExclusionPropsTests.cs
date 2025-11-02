// Phase 4.5.X.2 - ExclusionProps Unit Tests
// Tests LogCtx Props builders for file marker + pattern exclusion audit trail

using LogCtxShared;
using NUnit.Framework;
using NLogShared;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using VecTool.Handlers.Traversal;

namespace UnitTests.Traversal
{
    /// <summary>
    /// Unit tests for ExclusionProps static helper methods.
    /// Validates:
    /// 1. Props builder method signatures and parameter handling
    /// 2. Correct key-value pairs for each exclusion type
    /// 3. Null/empty value handling with sensible defaults
    /// 4. JSON serialization compatibility with SEQ logging
    /// 5. Query-ability by source/reason/layer
    /// </summary>
    [TestFixture]
    public class ExclusionPropsTests
    {
        private readonly CtxLogger log = new();

        #region Category 1: Pattern-Based Exclusion Props (2 tests)

        /// <summary>
        /// UNIT TEST 1a - CreatePatternProps Includes All Required Keys
        /// Pattern exclusion Props should contain:
        /// - exclusionlayer: "layer1pattern"
        /// - itempath: full file/directory path
        /// - pattern: matching pattern (e.g., "*.log", ".g.cs")
        /// - sourcefile: ignore file name (".gitignore" or ".vtignore")
        /// - timestamputc: ISO 8601 UTC timestamp
        /// </summary>
        [Test]
        public void CreatePatternPropsIncludesAllRequiredKeys()
        {
            // Arrange
            var itemPath = "/home/user/project/file.g.cs";
            var pattern = ".g.cs";
            var sourceFile = ".vtignore";

            // Act
            var props = ExclusionProps.CreatePatternProps(itemPath, pattern, sourceFile);

            // Assert - Verify Props structure
            props.ShouldNotBeNull();
            
            // Layer identifier
            props.Keys.ShouldContain("exclusionlayer");
            props["exclusionlayer"].ShouldBe("layer1pattern");

            // Item path
            props.Keys.ShouldContain("itempath");
            props["itempath"].ShouldBe(itemPath);

            // Pattern
            props.Keys.ShouldContain("pattern");
            props["pattern"].ShouldBe(pattern);

            // Source file
            props.Keys.ShouldContain("sourcefile");
            props["sourcefile"].ShouldBe(sourceFile);

            // Timestamp - should be ISO 8601 format
            props.Keys.ShouldContain("timestamputc");
            var timestampStr = (string)props["timestamputc"];
            timestampStr.ShouldNotBeNullOrEmpty();
            // Verify it can be parsed back to DateTime
            DateTime.Parse(timestampStr).ShouldNotBe(default(DateTime));

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "pattern_props_all_keys")
                .Add("keysCount", props.Count));
            log.Info("Pattern Props all keys verified");
        }

        /// <summary>
        /// UNIT TEST 1b - CreatePatternProps Handles Various Pattern Types
        /// Should correctly handle patterns like:
        /// - "*.log", "*.tmp" (file extensions)
        /// - ".g.cs", ".g.vb" (generated file markers)
        /// - "bin/", "obj/" (directory patterns)
        /// - "node_modules", "packages" (folder names)
        /// </summary>
        [Test]
        public void CreatePatternPropsHandlesVariousPatternTypes()
        {
            // Arrange
            var testCases = new (string itemPath, string pattern, string sourceFile)[]
            {
                ("/app/debug.log", "*.log", ".gitignore"),
                ("/app/Generated.g.cs", ".g.cs", ".vtignore"),
                ("/app/bin/", "bin/", ".gitignore"),
                ("/app/node_modules/", "node_modules/", ".gitignore"),
            };

            // Act & Assert
            foreach (var (itemPath, pattern, sourceFile) in testCases)
            {
                var props = ExclusionProps.CreatePatternProps(itemPath, pattern, sourceFile);
                props["pattern"].ShouldBe(pattern);
                props["itempath"].ShouldBe(itemPath);
                props["exclusionlayer"].ShouldBe("layer1pattern");
            }

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "pattern_types")
                .Add("casesHandled", testCases.Length));
            log.Info("Pattern types verified");
        }

        #endregion

        #region Category 2: Marker-Based Exclusion Props (3 tests)

        /// <summary>
        /// UNIT TEST 2a - CreateMarkerProps Includes All Required Keys
        /// Marker exclusion Props should contain:
        /// - exclusionlayer: "layer2marker"
        /// - filepath: full path to marked file
        /// - reason: marker reason (e.g., "generated_by_xsd")
        /// - spacereference: optional reference (default: "none")
        /// - linenumber: 1-indexed line number of marker
        /// - timestamputc: ISO 8601 UTC timestamp
        /// </summary>
        [Test]
        public void CreateMarkerPropsIncludesAllRequiredKeys()
        {
            // Arrange
            var filePath = "/app/src/Generated.g.cs";
            var reason = "generated_by_xsd";
            var spaceReference = "XSD-Schema-Docs";
            var lineNumber = 3;

            // Act
            var props = ExclusionProps.CreateMarkerProps(filePath, reason, spaceReference, lineNumber);

            // Assert - Verify Props structure
            props.ShouldNotBeNull();

            // Layer identifier
            props.Keys.ShouldContain("exclusionlayer");
            props["exclusionlayer"].ShouldBe("layer2marker");

            // File path
            props.Keys.ShouldContain("filepath");
            props["filepath"].ShouldBe(filePath);

            // Reason
            props.Keys.ShouldContain("reason");
            props["reason"].ShouldBe(reason);

            // Space reference
            props.Keys.ShouldContain("spacereference");
            props["spacereference"].ShouldBe(spaceReference);

            // Line number
            props.Keys.ShouldContain("linenumber");
            ((int)props["linenumber"]).ShouldBe(lineNumber);

            // Timestamp
            props.Keys.ShouldContain("timestamputc");
            var timestampStr = (string)props["timestamputc"];
            DateTime.Parse(timestampStr).ShouldNotBe(default(DateTime));

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "marker_props_all_keys")
                .Add("keysCount", props.Count));
            log.Info("Marker Props all keys verified");
        }

        /// <summary>
        /// UNIT TEST 2b - CreateMarkerProps With Null SpaceReference Defaults To "none"
        /// When spaceReference is null or whitespace, should default to "none".
        /// Prevents optional fields from becoming null in Props output.
        /// </summary>
        [Test]
        public void CreateMarkerPropsWithNullSpaceReferenceDefaultsToNone()
        {
            // Arrange
            var filePath = "/app/Config.g.cs";
            var reason = "generated_by_proto";
            var lineNumber = 5;

            // Act - Pass null space reference
            var props = ExclusionProps.CreateMarkerProps(filePath, reason, spaceReference: null, lineNumber);

            // Assert
            props["spacereference"].ShouldBe("none");

            // Act - Pass whitespace
            props = ExclusionProps.CreateMarkerProps(filePath, reason, "   ", lineNumber);

            // Assert
            props["spacereference"].ShouldBe("none");

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "marker_null_space_reference")
                .Add("defaultValue", "none"));
            log.Info("Null space reference default verified");
        }

        /// <summary>
        /// UNIT TEST 2c - CreateMarkerProps Handles Various Reason Types
        /// Should correctly capture different exclusion reasons:
        /// - "generated_by_xsd" (XSD-generated code)
        /// - "generated_by_proto" (Protobuf-generated code)
        /// - "vendor_library" (Third-party code)
        /// - "test_harness" (Test-generated code)
        /// </summary>
        [Test]
        public void CreateMarkerPropsHandlesVariousReasonTypes()
        {
            // Arrange
            var testCases = new (string reason, string? reference)[]
            {
                ("generated_by_xsd", "XSD-Schema-Docs"),
                ("generated_by_proto", "Protobuf-Guide"),
                ("vendor_library", "Third-Party-Guide"),
                ("test_harness", "Testing-Generated"),
            };

            // Act & Assert
            foreach (var (reason, reference) in testCases)
            {
                var props = ExclusionProps.CreateMarkerProps("/app/file.cs", reason, reference, 1);
                props["reason"].ShouldBe(reason);
                props["spacereference"].ShouldBe(reference ?? "none");
                props["exclusionlayer"].ShouldBe("layer2marker");
            }

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "marker_reason_types")
                .Add("casesHandled", testCases.Length));
            log.Info("Marker reason types verified");
        }

        #endregion

        #region Category 3: Error and Summary Props (2 tests)

        /// <summary>
        /// UNIT TEST 3a - CreateMarkerErrorProps Includes All Required Keys
        /// Marker error Props should contain:
        /// - exclusionlayer: "layer2markererror"
        /// - filepath: path to file where extraction failed
        /// - errortype: exception type name
        /// - errormessage: error message for debugging
        /// - timestamputc: ISO 8601 UTC timestamp
        /// </summary>
        [Test]
        public void CreateMarkerErrorPropsIncludesAllRequiredKeys()
        {
            // Arrange
            var filePath = "/app/Config.g.cs";
            var errorType = "UnauthorizedAccessException";
            var errorMessage = "Access to the path is denied.";

            // Act
            var props = ExclusionProps.CreateMarkerErrorProps(filePath, errorType, errorMessage);

            // Assert
            props.ShouldNotBeNull();

            props.Keys.ShouldContain("exclusionlayer");
            props["exclusionlayer"].ShouldBe("layer2markererror");

            props.Keys.ShouldContain("filepath");
            props["filepath"].ShouldBe(filePath);

            props.Keys.ShouldContain("errortype");
            props["errortype"].ShouldBe(errorType);

            props.Keys.ShouldContain("errormessage");
            props["errormessage"].ShouldBe(errorMessage);

            props.Keys.ShouldContain("timestamputc");

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "marker_error_props")
                .Add("keysCount", props.Count));
            log.Info("Marker error Props verified");
        }

        /// <summary>
        /// UNIT TEST 3b - CreateSummaryProps Aggregates Exclusion Statistics
        /// Summary Props should contain:
        /// - operation: "foldertraversalsummary"
        /// - filesprocessed: total files processed
        /// - filesexcludedlayer1pattern: files excluded by patterns
        /// - filesexcludedlayer2marker: files excluded by markers
        /// - markerextractionerrors: count of extraction failures
        /// - timestamputc: ISO 8601 UTC timestamp
        /// </summary>
        [Test]
        public void CreateSummaryPropsAggregatesExclusionStatistics()
        {
            // Arrange
            int filesProcessed = 1500;
            int filesExcludedByPattern = 425;
            int filesExcludedByMarker = 12;
            int markerExtractionErrors = 2;

            // Act
            var props = ExclusionProps.CreateSummaryProps(
                filesProcessed,
                filesExcludedByPattern,
                filesExcludedByMarker,
                markerExtractionErrors);

            // Assert
            props.ShouldNotBeNull();

            props.Keys.ShouldContain("operation");
            props["operation"].ShouldBe("foldertraversalsummary");

            props.Keys.ShouldContain("filesprocessed");
            ((int)props["filesprocessed"]).ShouldBe(filesProcessed);

            props.Keys.ShouldContain("filesexcludedlayer1pattern");
            ((int)props["filesexcludedlayer1pattern"]).ShouldBe(filesExcludedByPattern);

            props.Keys.ShouldContain("filesexcludedlayer2marker");
            ((int)props["filesexcludedlayer2marker"]).ShouldBe(filesExcludedByMarker);

            props.Keys.ShouldContain("markerextractionerrors");
            ((int)props["markerextractionerrors"]).ShouldBe(markerExtractionErrors);

            props.Keys.ShouldContain("timestamputc");

            // Verify summary math
            int totalExcluded = filesExcludedByPattern + filesExcludedByMarker;
            int included = filesProcessed - totalExcluded;
            included.ShouldBeGreaterThan(0);

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "summary_props")
                .Add("filesProcessed", filesProcessed)
                .Add("filesExcluded", totalExcluded)
                .Add("filesIncluded", included));
            log.Info("Summary Props verified");
        }

        #endregion

        #region Category 4: Directory Exclusion Props (1 test)

        /// <summary>
        /// UNIT TEST 4a - CreateDirectoryExclusionProps Handles Tree Exclusions
        /// Directory exclusion Props should contain:
        /// - exclusionlayer: "layer1directory"
        /// - directorypath: full path to excluded directory
        /// - pattern: matching pattern that excluded it
        /// - itemsskipped: count of items not enumerated
        /// - timestamputc: ISO 8601 UTC timestamp
        /// </summary>
        [Test]
        public void CreateDirectoryExclusionPropsHandlesTreeExclusions()
        {
            // Arrange
            var directoryPath = "/app/node_modules";
            var pattern = "node_modules/";
            int itemsSkipped = 1247;

            // Act
            var props = ExclusionProps.CreateDirectoryExclusionProps(
                directoryPath,
                pattern,
                itemsSkipped);

            // Assert
            props.ShouldNotBeNull();

            props.Keys.ShouldContain("exclusionlayer");
            props["exclusionlayer"].ShouldBe("layer1directory");

            props.Keys.ShouldContain("directorypath");
            props["directorypath"].ShouldBe(directoryPath);

            props.Keys.ShouldContain("pattern");
            props["pattern"].ShouldBe(pattern);

            props.Keys.ShouldContain("itemsskipped");
            ((int)props["itemsskipped"]).ShouldBe(itemsSkipped);

            props.Keys.ShouldContain("timestamputc");

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "directory_exclusion_props")
                .Add("itemsSkipped", itemsSkipped));
            log.Info("Directory exclusion Props verified");
        }

        #endregion

        #region Category 5: Props Serialization & SEQ Compatibility (3 tests)

        /// <summary>
        /// UNIT TEST 5a - Pattern Props Serialize To JSON Correctly
        /// Props should be JSON-serializable without errors for SEQ ingestion.
        /// All keys should be lowercase, all values should be JSON-compatible types.
        /// </summary>
        [Test]
        public void PatternPropsSerializeToJsonCorrectly()
        {
            // Arrange
            var props = ExclusionProps.CreatePatternProps(
                "/app/file.log",
                "*.log",
                ".gitignore");

            // Act & Assert - Serialize to JSON
            var json = JsonSerializer.Serialize(props);
            json.ShouldNotBeNullOrEmpty();
            json.ShouldContain("exclusionlayer");
            json.ShouldContain("layer1pattern");
            json.ShouldContain("itempath");
            json.ShouldContain("pattern");

            // Act - Deserialize back
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            deserialized.ShouldNotBeNull();
            deserialized.Keys.Count.ShouldBe(props.Keys.Count);

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "pattern_json_serialization")
                .Add("jsonLength", json.Length));
            log.Info("Pattern JSON serialization verified");
        }

        /// <summary>
        /// UNIT TEST 5b - Marker Props Serialize To JSON Correctly
        /// Props should serialize with all types preserved for SEQ queries.
        /// Integer values (linenumber) should remain integers in JSON.
        /// </summary>
        [Test]
        public void MarkerPropsSerializeToJsonCorrectly()
        {
            // Arrange
            var props = ExclusionProps.CreateMarkerProps(
                "/app/Generated.g.cs",
                "generated_by_xsd",
                "XSD-Schema-Docs",
                3);

            // Act & Assert - Serialize
            var json = JsonSerializer.Serialize(props);
            json.ShouldNotBeNullOrEmpty();
            json.ShouldContain("\"linenumber\":3"); // Integer preserved

            // Act - Deserialize
            var doc = JsonDocument.Parse(json);
            doc.RootElement.TryGetProperty("exclusionlayer", out var layer);
            layer.GetString().ShouldBe("layer2marker");

            doc.RootElement.TryGetProperty("linenumber", out var lineNum);
            lineNum.GetInt32().ShouldBe(3);

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "marker_json_serialization")
                .Add("jsonLength", json.Length));
            log.Info("Marker JSON serialization verified");
        }

        /// <summary>
        /// UNIT TEST 5c - Summary Props Serialize With Correct Type Preservation
        /// All numeric fields should serialize as numbers, not strings.
        /// Enables SEQ queries like: exclusionlayer="foldertraversalsummary" AND filesexcludedlayer2marker > 5
        /// </summary>
        [Test]
        public void SummaryPropsSerializeWithCorrectTypePreservation()
        {
            // Arrange
            var props = ExclusionProps.CreateSummaryProps(
                filesProcessed: 1500,
                filesExcludedByPattern: 425,
                filesExcludedByMarker: 12,
                markerExtractionErrors: 2);

            // Act & Assert - Serialize
            var json = JsonSerializer.Serialize(props);
            json.ShouldContain("\"filesprocessed\":1500");
            json.ShouldContain("\"filesexcludedlayer1pattern\":425");
            json.ShouldContain("\"filesexcludedlayer2marker\":12");
            json.ShouldContain("\"markerextractionerrors\":2");

            // Act - Deserialize with type checking
            var doc = JsonDocument.Parse(json);
            doc.RootElement.TryGetProperty("filesprocessed", out var filesProcessed);
            filesProcessed.GetInt32().ShouldBe(1500);

            doc.RootElement.TryGetProperty("filesexcludedlayer2marker", out var excluded);
            excluded.GetInt32().ShouldBe(12);

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "summary_type_preservation")
                .Add("jsonLength", json.Length));
            log.Info("Summary type preservation verified");
        }

        #endregion

        #region Category 6: Query Compatibility (2 tests)

        /// <summary>
        /// UNIT TEST 6a - Props Enable SEQ Queries By Layer And Source
        /// Should enable queries like:
        /// - exclusionlayer = "layer1pattern" AND pattern LIKE "%.g.cs"
        /// - exclusionlayer = "layer2marker" AND reason = "generated_by_xsd"
        /// - operation = "foldertraversalsummary" AND filesexcludedlayer2marker > 10
        /// </summary>
        [Test]
        public void PropsEnableSeqQueriesByLayerAndSource()
        {
            // Arrange - Create various Props types
            var patternProps = ExclusionProps.CreatePatternProps("/app/file.log", "*.log", ".gitignore");
            var markerProps = ExclusionProps.CreateMarkerProps("/app/Gen.g.cs", "generated_by_xsd", "XSD-Docs", 1);
            var summaryProps = ExclusionProps.CreateSummaryProps(100, 25, 5, 0);

            // Act & Verify - Query by layer
            patternProps["exclusionlayer"].ShouldBe("layer1pattern");
            markerProps["exclusionlayer"].ShouldBe("layer2marker");
            summaryProps["operation"].ShouldBe("foldertraversalsummary");

            // Verify queryable fields are present
            patternProps.Keys.ShouldContain("pattern");
            markerProps.Keys.ShouldContain("reason");
            summaryProps.Keys.ShouldContain("filesexcludedlayer2marker");

            // Verify source file queries
            patternProps["sourcefile"].ShouldBe(".gitignore");

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "seq_query_compatibility")
                .Add("patternPropsKeys", patternProps.Count)
                .Add("markerPropsKeys", markerProps.Count));
            log.Info("SEQ query compatibility verified");
        }

        /// <summary>
        /// UNIT TEST 6b - Props Support Reason Filtering For Aggregation
        /// Should enable queries grouping by reason:
        /// - reason = "generated_by_xsd" | count()
        /// - reason = "vendor_library" | count()
        /// Enables reporting on exclusion patterns across codebase.
        /// </summary>
        [Test]
        public void PropsEnableReasonFilteringForAggregation()
        {
            // Arrange - Create marker Props with various reasons
            var reasons = new[] { "generated_by_xsd", "generated_by_proto", "vendor_library" };
            var propsList = new List<Props>();

            foreach (var reason in reasons)
            {
                var props = ExclusionProps.CreateMarkerProps(
                    $"/app/file_{reason}.cs",
                    reason,
                    "Reference",
                    1);
                propsList.Add(props);
            }

            // Act & Verify - Group by reason
            var grouped = propsList
                .GroupBy(p => (string)p["reason"])
                .ToDictionary(g => g.Key, g => g.Count());

            // Assert
            grouped.Count.ShouldBe(3);
            grouped["generated_by_xsd"].ShouldBe(1);
            grouped["vendor_library"].ShouldBe(1);

            // Verify all reasons are queryable
            foreach (var reason in reasons)
            {
                grouped.Keys.ShouldContain(reason);
            }

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "reason_aggregation")
                .Add("reasonsGrouped", grouped.Count));
            log.Info("Reason filtering for aggregation verified");
        }

        #endregion

        #region Category 7: Edge Cases and Null Handling (2 tests)

        /// <summary>
        /// UNIT TEST 7a - Props Builders Handle Whitespace And Empty Strings
        /// Whitespace-only values should be treated as meaningful (not converted to null).
        /// This prevents silent data loss in logging.
        /// </summary>
        [Test]
        public void PropsHandleWhitespaceAndEmptyStrings()
        {
            // Arrange - Whitespace values
            var itemPath = "   ";
            var pattern = "   ";
            var sourceFile = "   ";

            // Act
            var props = ExclusionProps.CreatePatternProps(itemPath, pattern, sourceFile);

            // Assert - Values should be preserved
            props["itempath"].ShouldBe(itemPath);
            props["pattern"].ShouldBe(pattern);
            props["sourcefile"].ShouldBe(sourceFile);

            // Arrange - Empty strings
            var props2 = ExclusionProps.CreatePatternProps("", "", "");

            // Assert - Empty strings preserved
            props2["itempath"].ShouldBe("");
            props2["pattern"].ShouldBe("");

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "whitespace_handling"));
            log.Info("Whitespace handling verified");
        }

        /// <summary>
        /// UNIT TEST 7b - Timestamp Format Consistency Across All Props Builders
        /// All Props should use ISO 8601 UTC format (yyyy-MM-ddTHH:mm:ss.fffZ).
        /// Enables consistent SEQ queries by timestamp.
        /// </summary>
        [Test]
        public void TimestampFormatConsistencyAcrossAllPropsBuilders()
        {
            // Arrange
            var patternProps = ExclusionProps.CreatePatternProps("/app/file.log", "*.log", ".gitignore");
            var markerProps = ExclusionProps.CreateMarkerProps("/app/Gen.g.cs", "generated_by_xsd", null, 1);
            var errorProps = ExclusionProps.CreateMarkerErrorProps("/app/file.cs", "IOException", "Failed");
            var summaryProps = ExclusionProps.CreateSummaryProps(10, 2, 1, 0);
            var dirProps = ExclusionProps.CreateDirectoryExclusionProps("/app/bin", "bin/", 5);

            // Act - Extract all timestamps
            var timestamps = new[]
            {
                (string)patternProps["timestamputc"],
                (string)markerProps["timestamputc"],
                (string)errorProps["timestamputc"],
                (string)summaryProps["timestamputc"],
                (string)dirProps["timestamputc"],
            };

            // Assert - All should parse to valid DateTime
            foreach (var ts in timestamps)
            {
                ts.ShouldNotBeNullOrEmpty();
                var parsed = DateTime.Parse(ts);
                parsed.Kind.ShouldBe(DateTimeKind.Utc);
            }

            // Verify timestamps are in reasonable range (within last minute)
            var now = DateTime.UtcNow;
            foreach (var ts in timestamps)
            {
                var parsed = DateTime.Parse(ts);
                var diff = (now - parsed).TotalSeconds;
                diff.ShouldBeGreaterThanOrEqualTo(0);
                diff.ShouldBeLessThan(60); // Within last 60 seconds
            }

            using var ctx = log.Ctx.Set(new Props()
                .Add("test", "timestamp_consistency")
                .Add("timestampsChecked", timestamps.Length));
            log.Info("Timestamp format consistency verified");
        }

        #endregion
    }
}
