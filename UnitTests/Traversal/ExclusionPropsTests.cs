using LogCtxShared;
using NLogShared;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using VecTool.Handlers.Traversal;

namespace UnitTests.Traversal
{
    [TestFixture]
    public class ExclusionPropsTests
    {
        private readonly CtxLogger log = new();

        #region Assertion Helpers

        private static void AssertCommonKeys(Props props, params string[] expectedKeys)
        {
            props.ShouldNotBeNull();
            foreach (var key in expectedKeys)
                props.Keys.ShouldContain(key);
            props.Keys.ShouldContain("timestamputc");
        }

        private static void AssertTimestamp(string timestamp)
        {
            timestamp.ShouldNotBeNullOrEmpty();
            var parsed = DateTime.ParseExact(timestamp, "O", null, DateTimeStyles.RoundtripKind);
            parsed.Kind.ShouldBe(DateTimeKind.Utc);
            (DateTime.UtcNow - parsed).TotalSeconds.ShouldBeLessThan(60);
        }

        #endregion

        #region Category 1: Pattern-Based Exclusion Props (2 tests)

        [Test]
        public void CreatePatternProps_IncludesAllRequiredKeys()
        {
            var props = ExclusionProps.CreatePatternProps(
                "/home/user/project/file.g.cs", "*.g.cs", ".vtignore");

            AssertCommonKeys(props, "exclusionlayer", "itempath", "pattern", "sourcefile");
            props["exclusionlayer"].ShouldBe("layer1:pattern");
            props["itempath"].ShouldBe("/home/user/project/file.g.cs");
            props["pattern"].ShouldBe("*.g.cs");
            props["sourcefile"].ShouldBe(".vtignore");
            AssertTimestamp((string)props["timestamputc"]);
        }

        [TestCase("app/debug.log", "*.log", ".gitignore")]
        [TestCase("app/Generated.g.cs", "*.g.cs", ".vtignore")]
        [TestCase("app/bin", "bin/", ".gitignore")]
        [TestCase("app/node_modules", "node_modules/", ".gitignore")]
        public void CreatePatternProps_HandlesVariousPatternTypes(
            string itemPath, string pattern, string sourceFile)
        {
            var props = ExclusionProps.CreatePatternProps(itemPath, pattern, sourceFile);

            props["pattern"].ShouldBe(pattern);
            props["itempath"].ShouldBe(itemPath);
            props["exclusionlayer"].ShouldBe("layer1:pattern");
        }

        #endregion

        #region Category 2: Marker-Based Exclusion Props (3 tests)

        [Test]
        public void CreateMarkerProps_IncludesAllRequiredKeys()
        {
            var props = ExclusionProps.CreateMarkerProps(
                "app/src/Generated.g.cs", "generated:by:xsd", "XSD-Schema-Docs", 3);

            AssertCommonKeys(props, "exclusionlayer", "filepath", "reason",
                "spacereference", "linenumber");
            props["exclusionlayer"].ShouldBe("layer2:marker");
            props["filepath"].ShouldBe("app/src/Generated.g.cs");
            props["reason"].ShouldBe("generated:by:xsd");
            props["spacereference"].ShouldBe("XSD-Schema-Docs");
            ((int)props["linenumber"]).ShouldBe(3);
        }

        [Test]
        public void CreateMarkerProps_DefaultsNullSpaceReferenceToNone()
        {
            var props = ExclusionProps.CreateMarkerProps(
                "app/file.cs", "vendorlibrary", null, 1);

            props["spacereference"].ShouldBe("no space reference");
        }

        [TestCase("generated:by:xsd", "XSD-Schema-Docs")]
        [TestCase("generated:by:proto", "Protobuf-Guide")]
        [TestCase("vendorlibrary", "Third-Party-Guide")]
        [TestCase("testharness", "Testing-Generated")]
        public void CreateMarkerProps_HandlesVariousReasonTypes(string reason, string reference)
        {
            var props = ExclusionProps.CreateMarkerProps("app/file.cs", reason, reference, 1);

            props["reason"].ShouldBe(reason);
            props["spacereference"].ShouldBe(reference);
            props["exclusionlayer"].ShouldBe("layer2:marker");
        }

        #endregion

        #region Category 3: Error and Summary Props (2 tests)

        [Test]
        public void CreateMarkerErrorProps_IncludesAllRequiredKeys()
        {
            var props = ExclusionProps.CreateMarkerErrorProps(
                "app/Config.g.cs", "UnauthorizedAccessException", "Access denied");

            AssertCommonKeys(props, "exclusionlayer", "filepath", "errortype", "errormessage");
            props["exclusionlayer"].ShouldBe("layer2:marker:error");
            props["filepath"].ShouldBe("app/Config.g.cs");
            props["errortype"].ShouldBe("UnauthorizedAccessException");
            props["errormessage"].ShouldBe("Access denied");
        }

        [Test]
        public void CreateSummaryProps_AggregatesExclusionStatistics()
        {
            var props = ExclusionProps.CreateSummaryProps(1500, 425, 12, 2);

            AssertCommonKeys(props, "operation", "filesprocessed",
                "filesexcluded:layer1:pattern", "filesexcluded:layer2:marker",
                "markerextractionerrors");
            props["operation"].ShouldBe("foldertraversal:summary");
            ((int)props["filesprocessed"]).ShouldBe(1500);
            ((int)props["filesexcluded:layer1:pattern"]).ShouldBe(425);
            ((int)props["filesexcluded:layer2:marker"]).ShouldBe(12);
            ((int)props["markerextractionerrors"]).ShouldBe(2);
        }

        #endregion

        #region Category 4: Directory Exclusion Props (1 test)

        [Test]
        public void CreateDirectoryExclusionProps_HandlesTreeExclusions()
        {
            var props = ExclusionProps.CreateDirectoryExclusionProps(
                "app/node_modules", "node_modules/", 1247);

            AssertCommonKeys(props, "exclusionlayer", "directorypath", "pattern", "itemsskipped");
            props["exclusionlayer"].ShouldBe("layer1:directory");
            props["directorypath"].ShouldBe("app/node_modules");
            props["pattern"].ShouldBe("node_modules/");
            ((int)props["itemsskipped"]).ShouldBe(1247);
        }

        #endregion

        #region Category 5: Props Serialization & SEQ Compatibility (3 tests)

        [Test]
        public void PatternProps_SerializeToJsonCorrectly()
        {
            var props = ExclusionProps.CreatePatternProps("app/file.log", "*.log", ".gitignore");

            var json = JsonSerializer.Serialize(props);

            json.ShouldContain("exclusionlayer");
            json.ShouldContain("layer1:pattern");
            json.ShouldContain("itempath");
            json.ShouldContain("pattern");

            var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            deserialized.ShouldNotBeNull();
            deserialized!.Keys.Count.ShouldBe(props.Keys.Count);
        }

        [Test]
        public void MarkerProps_SerializeToJsonCorrectly()
        {
            var props = ExclusionProps.CreateMarkerProps(
                "app/Generated.g.cs", "generated:by:xsd", "XSD-Schema-Docs", 3);

            var json = JsonSerializer.Serialize(props);
            json.ShouldContain("\"linenumber\":3");

            var doc = JsonDocument.Parse(json);
            doc.RootElement.TryGetProperty("exclusionlayer", out var layer);
            layer.GetString().ShouldBe("layer2:marker");
            doc.RootElement.TryGetProperty("linenumber", out var lineNum);
            lineNum.GetInt32().ShouldBe(3);
        }

        [Test]
        public void SummaryProps_SerializeWithCorrectTypePreservation()
        {
            var props = ExclusionProps.CreateSummaryProps(1500, 425, 12, 2);

            var json = JsonSerializer.Serialize(props);
            json.ShouldContain("\"filesprocessed\":1500");
            json.ShouldContain("\"filesexcluded:layer1:pattern\":425");
            json.ShouldContain("\"filesexcluded:layer2:marker\":12");
            json.ShouldContain("\"markerextractionerrors\":2");

            var doc = JsonDocument.Parse(json);
            doc.RootElement.TryGetProperty("filesprocessed", out var filesProcessed);
            filesProcessed.GetInt32().ShouldBe(1500);
            doc.RootElement.TryGetProperty("filesexcluded:layer2:marker", out var excluded);
            excluded.GetInt32().ShouldBe(12);
        }

        #endregion

        #region Category 6: Query Compatibility (2 tests)

        [Test]
        public void Props_EnableSeqQueriesByLayerAndSource()
        {
            var patternProps = ExclusionProps.CreatePatternProps("app/file.log", "*.log", ".gitignore");
            var markerProps = ExclusionProps.CreateMarkerProps(
                "app/Gen.g.cs", "generated:by:xsd", "XSD-Docs", 1);
            var summaryProps = ExclusionProps.CreateSummaryProps(100, 25, 5, 0);

            patternProps["exclusionlayer"].ShouldBe("layer1:pattern");
            markerProps["exclusionlayer"].ShouldBe("layer2:marker");
            summaryProps["operation"].ShouldBe("foldertraversal:summary");
            patternProps.Keys.ShouldContain("pattern");
            markerProps.Keys.ShouldContain("reason");
            summaryProps.Keys.ShouldContain("filesexcluded:layer2:marker");
            patternProps["sourcefile"].ShouldBe(".gitignore");
        }

        [Test]
        public void Props_EnableReasonFilteringForAggregation()
        {
            var reasons = new[] { "generated:by:xsd", "generated:by:proto", "vendorlibrary" };
            var propsList = reasons
                .Select(r => ExclusionProps.CreateMarkerProps($"app/file_{r}.cs", r, "Reference", 1))
                .ToList();

            var grouped = propsList
                .GroupBy(p => (string)p["reason"])
                .ToDictionary(g => g.Key, g => g.Count());

            grouped.Count.ShouldBe(3);
            grouped["generated:by:xsd"].ShouldBe(1);
            grouped["vendorlibrary"].ShouldBe(1);
            foreach (var reason in reasons)
                grouped.Keys.ShouldContain(reason);
        }

        #endregion

        #region Category 7: Edge Cases and Null Handling (2 tests)

        [TestCase("   ", "  ", " ")]
        [TestCase("", "", "")]
        public void PropsBuilders_HandleWhitespaceAndEmptyStrings(
            string itemPath, string pattern, string sourceFile)
        {
            var props = ExclusionProps.CreatePatternProps(itemPath, pattern, sourceFile);

            props["itempath"].ShouldBe(itemPath);
            props["pattern"].ShouldBe(pattern);
            props["sourcefile"].ShouldBe(sourceFile);
        }

        [Test]
        public void AllPropsBuilders_UseConsistentTimestampFormat()
        {
            var patternProps = ExclusionProps.CreatePatternProps("app/file.log", "*.log", ".gitignore");
            var markerProps = ExclusionProps.CreateMarkerProps("app/Gen.g.cs", "reason", null, 1);
            var errorProps = ExclusionProps.CreateMarkerErrorProps("app/file.cs", "IOException", "Failed");
            var summaryProps = ExclusionProps.CreateSummaryProps(10, 2, 1, 0);
            var dirProps = ExclusionProps.CreateDirectoryExclusionProps("app/bin", "bin/", 5);

            var timestamps = new[]
            {
                (string)patternProps["timestamputc"],
                (string)markerProps["timestamputc"],
                (string)errorProps["timestamputc"],
                (string)summaryProps["timestamputc"],
                (string)dirProps["timestamputc"]
            };

            foreach (var ts in timestamps)
                AssertTimestamp(ts);
        }

        #endregion
    }
}
