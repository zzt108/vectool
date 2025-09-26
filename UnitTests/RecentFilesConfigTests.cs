// File: UnitTests/RecentFilesConfigTests.cs
// Verifies Step 1 config behavior: defaults, valid values, edge cases.

using NUnit.Framework;
using Shouldly;
using System.Collections.Specialized;
using System.IO;
using DocXHandler;

namespace UnitTests
{
    [TestFixture]
    public class RecentFilesConfigTests
    {
        [Test]
        public void Defaults_WhenKeysMissing_ShouldApplyPlanDefaults()
        {
            var nvc = new NameValueCollection();
            var cfg = RecentFilesConfig.FromAppSettings(nvc);

            cfg.MaxCount.ShouldBe(10);
            cfg.RetentionDays.ShouldBe(15);

            // Default path is %AppData%/VecTool/Generated normalized by the class.
            cfg.OutputPath.ShouldEndWith(Path.Combine("VecTool", "Generated"));
        }

        [Test]
        public void Reads_Valid_Values_ShouldUseProvidedSettings()
        {
            var nvc = new NameValueCollection
            {
                ["recentFilesMaxCount"] = "20",
                ["recentFilesRetentionDays"] = "30",
                ["recentFilesOutputPath"] = "%TEMP%/MyVecTool/Out"
            };

            var cfg = RecentFilesConfig.FromAppSettings(nvc);

            cfg.MaxCount.ShouldBe(20);
            cfg.RetentionDays.ShouldBe(30);

            // Ensure environment variables are expanded and separators normalized
            cfg.OutputPath.ShouldContain("MyVecTool");
            cfg.OutputPath.ShouldEndWith(Path.Combine("MyVecTool", "Out"));
        }

        [Test]
        public void Edge_Values_ShouldFallbackToDefaults()
        {
            var nvc = new NameValueCollection
            {
                ["recentFilesMaxCount"] = "0",     // invalid (non-positive)
                ["recentFilesRetentionDays"] = "-5",// invalid (non-positive)
                ["recentFilesOutputPath"] = ""      // invalid (blank)
            };

            var cfg = RecentFilesConfig.FromAppSettings(nvc);

            cfg.MaxCount.ShouldBe(10);
            cfg.RetentionDays.ShouldBe(15);
            cfg.OutputPath.ShouldEndWith(Path.Combine("VecTool", "Generated"));
        }
    }
}
