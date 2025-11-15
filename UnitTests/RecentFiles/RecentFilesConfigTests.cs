// ✅ FULL FILE VERSION
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using VecTool.Core.Configuration;
using VecTool.Core.Models;

namespace UnitTests.RecentFiles
{
    [TestFixture]
    public class RecentFilesConfigTests
    {
        #region Test Helper - Mock Reader

        private sealed class FakeReader : IAppSettingsReader
        {
            private readonly Dictionary<string, string?> _values;

            public FakeReader(Dictionary<string, string?> values)
            {
                _values = values;
            }

            public string? Get(string key) =>
                _values.TryGetValue(key, out var v) ? v : null;
        }

        #endregion

        #region FromAppConfig Tests

        [Test]
        public void FromAppConfig_AllKeysPresent_LoadsValues()
        {
            var reader = new FakeReader(new Dictionary<string, string?>
            {
                ["recentFilesMaxCount"] = "50",
                ["recentFilesRetentionDays"] = "15",
                ["recentFilesOutputPath"] = "TestOutput"
            });

            var config = RecentFilesConfig.FromAppConfig(reader);

            config.MaxCount.ShouldBe(50);
            config.RetentionDays.ShouldBe(15);
            config.OutputPath.ShouldBe("TestOutput");
            config.StorageFilePath.ShouldBe(@"TestOutput\recentFiles.json");
        }

        [Test]
        public void FromAppConfig_KeysMissing_UsesDefaults()
        {
            var reader = new FakeReader(new Dictionary<string, string?>());

            var config = RecentFilesConfig.FromAppConfig(reader);

            config.MaxCount.ShouldBe(RecentFilesConfig.DefaultMaxCount);
            config.RetentionDays.ShouldBe(RecentFilesConfig.DefaultRetentionDays);
            config.OutputPath.ShouldBe(RecentFilesConfig.DefaultOutputPath);
        }

        [Test]
        public void FromAppConfig_InvalidFormat_UsesDefaults()
        {
            var reader = new FakeReader(new Dictionary<string, string?>
            {
                ["recentFilesMaxCount"] = "not-a-number",
                ["recentFilesRetentionDays"] = "invalid"
            });

            var config = RecentFilesConfig.FromAppConfig(reader);

            config.MaxCount.ShouldBe(RecentFilesConfig.DefaultMaxCount);
            config.RetentionDays.ShouldBe(RecentFilesConfig.DefaultRetentionDays);
        }

        #endregion

        #region Constructor Validation Tests

        [TestCase(0, 30, "path", "MaxCount must be between")]
        [TestCase(10001, 30, "path", "MaxCount must be between")]
        [TestCase(-1, 30, "path", "MaxCount must be between")]
        [TestCase(100, -1, "path", "RetentionDays must be between")]
        [TestCase(100, 3651, "path", "RetentionDays must be between")]
        public void Constructor_InvalidRanges_ThrowsArgumentOutOfRangeException(
            int maxCount,
            int retentionDays,
            string outputPath,
            string expectedMessageFragment)
        {
            var ex = Should.Throw<ArgumentOutOfRangeException>(
                () => new RecentFilesConfig(maxCount, retentionDays, outputPath));

            ex.Message.ShouldContain(expectedMessageFragment);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_InvalidPath_NullOrWhitespace_ThrowsArgumentException(string? invalidPath)
        {
            Should.Throw<ArgumentException>(
                () => new RecentFilesConfig(100, 30, invalidPath!));
        }

        [Test]
        public void Constructor_InvalidPath_IllegalCharacters_ThrowsArgumentException()
        {
            Should.Throw<ArgumentException>(
                () => new RecentFilesConfig(100, 30, @"C:\|id"));
        }

        [Test]
        public void Constructor_ValidParameters_CreatesInstance()
        {
            var config = new RecentFilesConfig(100, 30, "Generated");

            config.MaxCount.ShouldBe(100);
            config.RetentionDays.ShouldBe(30);
            config.OutputPath.ShouldBe("Generated");
            config.StorageFilePath.ShouldBe(@"Generated\recentFiles.json");
        }

        #endregion
    }
}
