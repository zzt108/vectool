using LogCtxShared;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;
using System.Globalization;
using System.Text.Json;
using VecTool.Handlers.Traversal;

namespace UnitTests.Traversal
{
    [TestFixture]
    public class ExclusionPropsTests
    {
        private readonly ILogger logger = TestLogger.For<ExclusionPropsTests>();

        #region Assertion Helpers

        private static void AssertCommonKeys(IDictionary<string, object> props, params string[] expectedKeys)
        {
            props.ShouldNotBeNull();
            foreach (var key in expectedKeys)
                props.Keys.ShouldContain(key);
            props.Keys.ShouldContain("timestamp_utc");
        }

        private static void AssertTimestamp(string timestamp)
        {
            timestamp.ShouldNotBeNullOrEmpty();
            var parsed = DateTime.ParseExact(timestamp, "O", null, DateTimeStyles.RoundtripKind);
            parsed.Kind.ShouldBe(DateTimeKind.Utc);
            (DateTime.UtcNow - parsed).TotalSeconds.ShouldBeLessThan(60);
        }

        #endregion Assertion Helpers

        #region Category 1: Pattern-Based Exclusion Props (2 tests)

        [Test]
        public void CreatePatternProps_IncludesAllRequiredKeys()
        {
            var props = ExclusionProps.CreatePatternProps(
                "home/user/project/file.g.cs",
                "*.g.cs",
                ".vtignore"
            );

            AssertCommonKeys(props, "exclusion_layer", "item_path", "pattern", "source_file");
            props["exclusion_layer"].ShouldBe("layer_1_pattern");
            props["item_path"].ShouldBe("home/user/project/file.g.cs");
            props["pattern"].ShouldBe("*.g.cs");
            props["source_file"].ShouldBe(".vtignore");
            AssertTimestamp((string)props["timestamp_utc"]);
        }

        [TestCase("app/debug.log", "*.log", ".gitignore")]
        [TestCase("app/Generated.g.cs", "*.g.cs", ".vtignore")]
        [TestCase("app/bin", "bin", ".gitignore")]
        [TestCase("app/node_modules", "node_modules", ".gitignore")]
        public void CreatePatternProps_HandlesVariousPatternTypes(string itemPath, string pattern, string sourceFile)
        {
            var props = ExclusionProps.CreatePatternProps(itemPath, pattern, sourceFile);

            props["pattern"].ShouldBe(pattern);
            props["item_path"].ShouldBe(itemPath);
            props["exclusion_layer"].ShouldBe("layer_1_pattern");
        }

        #endregion Category 1: Pattern-Based Exclusion Props (2 tests)

        #region Category 2: Marker-Based Exclusion Props (3 tests)

        [Test]
        public void CreateMarkerProps_IncludesAllRequiredKeys()
        {
            var props = ExclusionProps.CreateMarkerProps(
                "app/src/Generated.g.cs",
                "generated_by_xsd",
                "XSD-Schema-Docs",
                3
            );

            AssertCommonKeys(props, "exclusion_layer", "file_path", "reason", "space_reference", "line_number");
            props["exclusion_layer"].ShouldBe("layer_2_marker");
            props["file_path"].ShouldBe("app/src/Generated.g.cs");
            props["reason"].ShouldBe("generated_by_xsd");
            props["space_reference"].ShouldBe("XSD-Schema-Docs");
            ((int)props["line_number"]).ShouldBe(3);
        }

        [Test]
        public void CreateMarkerProps_DefaultsNullSpaceReferenceToNone()
        {
            var props = ExclusionProps.CreateMarkerProps(
                "app/file.cs",
                "vendor_library",
                null,
                1
            );

            props["space_reference"].ShouldBe("no space reference");
        }

        [TestCase("generated_by_xsd", "XSD-Schema-Docs")]
        [TestCase("generated_by_proto", "Protobuf-Guide")]
        [TestCase("vendor_library", "Third-Party-Guide")]
        [TestCase("test_harness", "Testing-Generated")]
        public void CreateMarkerProps_HandlesVariousReasonTypes(string reason, string reference)
        {
            var props = ExclusionProps.CreateMarkerProps("app/file.cs", reason, reference, 1);

            props["reason"].ShouldBe(reason);
            props["space_reference"].ShouldBe(reference);
            props["exclusion_layer"].ShouldBe("layer_2_marker");
        }

        #endregion Category 2: Marker-Based Exclusion Props (3 tests)

        #region Category 3: LogError and Summary Props (2 tests)

        [Test]
        public void CreateMarkerErrorProps_IncludesAllRequiredKeys()
        {
            var props = ExclusionProps.CreateMarkerErrorProps(
                "app/Config.g.cs",
                "UnauthorizedAccessException",
                "Access denied"
            );

            AssertCommonKeys(props, "exclusion_layer", "file_path", "error_type", "error_message");
            props["exclusion_layer"].ShouldBe("layer_2_marker_error");
            props["file_path"].ShouldBe("app/Config.g.cs");
            props["error_type"].ShouldBe("UnauthorizedAccessException");
            props["error_message"].ShouldBe("Access denied");
        }

        [Test]
        public void CreateSummaryProps_AggregatesExclusionStatistics()
        {
            var props = ExclusionProps.CreateSummaryProps(1500, 425, 12, 2);

            AssertCommonKeys(props, "operation", "files_processed", "files_excluded_layer_1_pattern",
                           "files_excluded_layer_2_marker", "marker_extraction_errors");
            props["operation"].ShouldBe("folder_traversal_summary");
            ((int)props["files_processed"]).ShouldBe(1500);
            ((int)props["files_excluded_layer_1_pattern"]).ShouldBe(425);
            ((int)props["files_excluded_layer_2_marker"]).ShouldBe(12);
            ((int)props["marker_extraction_errors"]).ShouldBe(2);
        }

        #endregion Category 3: LogError and Summary Props (2 tests)

        #region Category 4: Directory Exclusion Props (1 test)

        [Test]
        public void CreateDirectoryExclusionProps_HandlesTreeExclusions()
        {
            var props = ExclusionProps.CreateDirectoryExclusionProps(
                "app/node_modules",
                "node_modules",
                1247
            );

            AssertCommonKeys(props, "exclusion_layer", "directory_path", "pattern", "items_skipped");
            props["exclusion_layer"].ShouldBe("layer_1_directory");
            props["directory_path"].ShouldBe("app/node_modules");
            props["pattern"].ShouldBe("node_modules");
            ((int)props["items_skipped"]).ShouldBe(1247);
        }

        #endregion Category 4: Directory Exclusion Props (1 test)

        #region Category 5: Props Serialization / SEQ Compatibility (3 tests)

        [Test]
        public void PatternProps_SerializeToJsonCorrectly()
        {
            var props = ExclusionProps.CreatePatternProps("app/file.log", "*.log", ".gitignore");
            var json = JsonSerializer.Serialize(props);

            json.ShouldContain("exclusion_layer");
            json.ShouldContain("layer_1_pattern");
            json.ShouldContain("item_path");
            json.ShouldContain("pattern");

            var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            deserialized.ShouldNotBeNull();
            deserialized!.Keys.Count.ShouldBe(props.Keys.Count);
        }

        [Test]
        public void MarkerProps_SerializeToJsonCorrectly()
        {
            var props = ExclusionProps.CreateMarkerProps(
                "app/Generated.g.cs",
                "generated_by_xsd",
                "XSD-Schema-Docs",
                3
            );
            var json = JsonSerializer.Serialize(props);

            json.ShouldContain("line_number\":3");

            var doc = JsonDocument.Parse(json);
            doc.RootElement.TryGetProperty("exclusion_layer", out var layer);
            layer.GetString().ShouldBe("layer_2_marker");
            doc.RootElement.TryGetProperty("line_number", out var lineNum);
            lineNum.GetInt32().ShouldBe(3);
        }

        [Test]
        public void SummaryProps_SerializeWithCorrectTypePreservation()
        {
            var props = ExclusionProps.CreateSummaryProps(1500, 425, 12, 2);
            var json = JsonSerializer.Serialize(props);

            json.ShouldContain("\"files_processed\":1500");
            json.ShouldContain("\"files_excluded_layer_1_pattern\":425");
            json.ShouldContain("\"files_excluded_layer_2_marker\":12");
            json.ShouldContain("\"marker_extraction_errors\":2");

            var doc = JsonDocument.Parse(json);
            doc.RootElement.TryGetProperty("files_processed", out var filesProcessed);
            filesProcessed.GetInt32().ShouldBe(1500);
            doc.RootElement.TryGetProperty("files_excluded_layer_2_marker", out var excluded);
            excluded.GetInt32().ShouldBe(12);
        }

        #endregion Category 5: Props Serialization / SEQ Compatibility (3 tests)

        #region Category 6: Query Compatibility (2 tests)

        [Test]
        public void Props_EnableSeqQueriesByLayerAndSource()
        {
            var patternProps = ExclusionProps.CreatePatternProps("app/file.log", "*.log", ".gitignore");
            var markerProps = ExclusionProps.CreateMarkerProps(
                "app/Gen.g.cs",
                "generated_by_xsd",
                "XSD-Docs",
                1
            );
            var summaryProps = ExclusionProps.CreateSummaryProps(100, 25, 5, 0);

            patternProps["exclusion_layer"].ShouldBe("layer_1_pattern");
            markerProps["exclusion_layer"].ShouldBe("layer_2_marker");
            summaryProps["operation"].ShouldBe("folder_traversal_summary");

            patternProps.Keys.ShouldContain("pattern");
            markerProps.Keys.ShouldContain("reason");
            summaryProps.Keys.ShouldContain("files_excluded_layer_2_marker");
            patternProps["source_file"].ShouldBe(".gitignore");
        }

        [Test]
        public void Props_EnableReasonFilteringForAggregation()
        {
            var reasons = new[] { "generated_by_xsd", "generated_by_proto", "vendor_library" };
            var propsList = reasons
                .Select(r => ExclusionProps.CreateMarkerProps($"app/file{r}.cs", r, "Reference", 1))
                .ToList();

            var grouped = propsList
                .GroupBy(p => (string)p["reason"])
                .ToDictionary(g => g.Key, g => g.Count());

            grouped.Count.ShouldBe(3);
            grouped["generated_by_xsd"].ShouldBe(1);
            grouped["vendor_library"].ShouldBe(1);

            foreach (var reason in reasons)
                grouped.Keys.ShouldContain(reason);
        }

        #endregion Category 6: Query Compatibility (2 tests)

        #region Category 7: Edge Cases and Null Handling (2 tests)

        [TestCase("", "", "")]
        [TestCase(" ", " ", " ")]
        public void PropsBuilders_HandleWhitespaceAndEmptyStrings(string itemPath, string pattern, string sourceFile)
        {
            var props = ExclusionProps.CreatePatternProps(itemPath, pattern, sourceFile);

            props["item_path"].ShouldBe(itemPath);
            props["pattern"].ShouldBe(pattern);
            props["source_file"].ShouldBe(sourceFile);
        }

        [Test]
        public void AllPropsBuilders_UseConsistentTimestampFormat()
        {
            var patternProps = ExclusionProps.CreatePatternProps("app/file.log", "*.log", ".gitignore");
            var markerProps = ExclusionProps.CreateMarkerProps("app/Gen.g.cs", "reason", null, 1);
            var errorProps = ExclusionProps.CreateMarkerErrorProps("app/file.cs", "IOException", "Failed");
            var summaryProps = ExclusionProps.CreateSummaryProps(10, 2, 1, 0);
            var dirProps = ExclusionProps.CreateDirectoryExclusionProps("app/bin", "bin", 5);

            var timestamps = new[]
            {
                (string)patternProps["timestamp_utc"],
                (string)markerProps["timestamp_utc"],
                (string)errorProps["timestamp_utc"],
                (string)summaryProps["timestamp_utc"],
                (string)dirProps["timestamp_utc"]
            };

            foreach (var ts in timestamps)
                AssertTimestamp(ts);
        }

        #endregion Category 7: Edge Cases and Null Handling (2 tests)
    }
}