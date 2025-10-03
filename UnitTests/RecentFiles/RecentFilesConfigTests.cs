// Path: UnitTests/RecentFiles/RecentFilesConfigTests.cs
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using VecTool.Configuration;

namespace UnitTests.RecentFiles
{
    [TestFixture]
    public class RecentFilesConfigTests
    {
        // Mock reader for predictable test inputs
        private sealed class FakeReader : IAppSettingsReader
        {
            private readonly Dictionary<string, string?> _values;
            public FakeReader(Dictionary<string, string?> values) => _values = values;
            public string? Get(string key) => _values.TryGetValue(key, out var v) ? v : null;
        }

        [Test]
        public void FromAppConfig_WhenKeysArePresent_ShouldLoadValues()
        {
            // Arrange
            var reader = new FakeReader(new Dictionary<string, string?>
            {
                { "recentFilesMaxCount", "50" },
                { "recentFilesRetentionDays", "15" },
                { "recentFilesOutputPath", "TestOutput" }
            });

            // Act
            var config = RecentFilesConfig.FromAppConfig(reader);

            // Assert
            config.MaxCount.ShouldBe(50);
            config.RetentionDays.ShouldBe(15);
            config.OutputPath.ShouldBe("TestOutput");
            config.StorageFilePath.ShouldBe("TestOutput\\recentFiles.json");
        }

        [Test]
        public void FromAppConfig_WhenKeysAreMissing_ShouldUseDefaults()
        {
            // Arrange
            var reader = new FakeReader(new Dictionary<string, string?>());

            // Act
            var config = RecentFilesConfig.FromAppConfig(reader);

            // Assert
            config.MaxCount.ShouldBe(RecentFilesConfig.DefaultMaxCount);
            config.RetentionDays.ShouldBe(RecentFilesConfig.DefaultRetentionDays);
            config.OutputPath.ShouldBe(RecentFilesConfig.DefaultOutputPath);
        }

        [Test]
        public void FromAppConfig_WhenValuesAreInvalidFormat_ShouldUseDefaults()
        {
            // Arrange
            var reader = new FakeReader(new Dictionary<string, string?>
            {
                { "recentFilesMaxCount", "not-a-number" },
                { "recentFilesRetentionDays", "invalid" }
            });

            // Act
            var config = RecentFilesConfig.FromAppConfig(reader);

            // Assert
            config.MaxCount.ShouldBe(RecentFilesConfig.DefaultMaxCount);
            config.RetentionDays.ShouldBe(RecentFilesConfig.DefaultRetentionDays);
        }

        [Test]
        public void Constructor_WithInvalidRanges_ShouldThrowArgumentOutOfRangeException()
        {
            // Assert
            Should.Throw<ArgumentOutOfRangeException>(() => new RecentFilesConfig(0, 30, "path"));
            Should.Throw<ArgumentOutOfRangeException>(() => new RecentFilesConfig(10001, 30, "path"));
            Should.Throw<ArgumentOutOfRangeException>(() => new RecentFilesConfig(100, -1, "path"));
            Should.Throw<ArgumentOutOfRangeException>(() => new RecentFilesConfig(100, 3651, "path"));
        }

        [Test]
        public void Constructor_WithInvalidPath_ShouldThrowArgumentException()
        {
            // Assert
            Should.Throw<ArgumentException>(() => new RecentFilesConfig(100, 30, ""));
            Should.Throw<ArgumentException>(() => new RecentFilesConfig(100, 30, "   "));
            Should.Throw<ArgumentException>(() => new RecentFilesConfig(100, 30, "C:\\inv|lid\\path"));
        }
    }
}
