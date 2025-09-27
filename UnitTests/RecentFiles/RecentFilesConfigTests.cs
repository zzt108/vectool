// Path: UnitTests/RecentFiles/RecentFilesConfigTests.cs
using NUnit.Framework;
using Shouldly;
using System;
using DocXHandler.RecentFiles;
using System.Configuration;

namespace UnitTests.RecentFiles
{
    internal sealed class FakeReader : IAppSettingsReader
    {
        private readonly Func<string, string?> _get;
        public FakeReader(Func<string, string?> get) => _get = get;
        public string? Get(string key) => _get(key);
    }

    [TestFixture]
    public class RecentFilesConfigTests
    {

        [Test]
        public void ConfigShouldLoadFromAppConfig()
        {
            // DEBUG: check what files exist and what ConfigurationManager reads
            var assemblyLoc = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var dir = Path.GetDirectoryName(assemblyLoc) ?? "";
            Console.WriteLine($"Assembly location: {assemblyLoc}");
            Console.WriteLine($"Directory: {dir}");

            var exeConfig = Path.Combine(dir, "testhost.exe.config");
            var dllConfig = Path.Combine(dir, "testhost.dll.config");
            var appConfig = Path.Combine(dir, "UnitTests.dll.config");

            Console.WriteLine($"testhost.exe.config exists: {File.Exists(exeConfig)}");
            Console.WriteLine($"testhost.dll.config exists: {File.Exists(dllConfig)}");
            Console.WriteLine($"UnitTests.dll.config exists: {File.Exists(appConfig)}");

            // What values does ConfigurationManager actually read?
            var maxCount = ConfigurationManager.AppSettings["recentFilesMaxCount"];
            var retentionDays = ConfigurationManager.AppSettings["recentFilesRetentionDays"];
            var outputPath = ConfigurationManager.AppSettings["recentFilesOutputPath"];

            Console.WriteLine($"ConfigurationManager reads: maxCount='{maxCount}', retentionDays='{retentionDays}', outputPath='{outputPath}'");

            var cfg = RecentFilesConfig.FromAppConfig();
            cfg.MaxCount.ShouldBe(7);
            cfg.RetentionDays.ShouldBe(30);
            cfg.OutputPath.ShouldContain("VecTool");
        }

        [Test]
        public void DefaultsShouldBeReasonable()
        {
            var cfg = RecentFilesConfig.FromReader(new FakeReader(_ => null));
            cfg.MaxCount.ShouldBe(10);
            cfg.RetentionDays.ShouldBe(15);
            cfg.OutputPath.ShouldNotBeNullOrWhiteSpace();
        }

        [TestCase("0")]
        [TestCase("-5")]
        [TestCase("not-a-number")]
        public void InvalidValuesShouldBeRejected(string badValue)
        {
            var reader = new FakeReader(key =>
                key == "recentFilesMaxCount" ? badValue :
                key == "recentFilesRetentionDays" ? "15" :
                key == "recentFilesOutputPath" ? "%APPDATA%\\VecTool\\Generated" :
                null);

            Should.Throw<ArgumentOutOfRangeException>(() => RecentFilesConfig.FromReader(reader));
        }
    }
}
