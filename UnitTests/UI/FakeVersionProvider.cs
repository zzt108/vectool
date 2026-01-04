using NUnit.Framework;
using Shouldly;
using System.Windows.Forms;
using Vectool.OaiUI;
using Vectool.UI.Versioning;

namespace UnitTests.UI
{
    public sealed class FakeVersionProvider : IVersionProvider
    {
        public string ApplicationName => "VecTool";
        public string AssemblyVersion => "4.0.0.0";
        public string FileVersion => "4.0.12345.1243";
        public string InformationalVersion => "4.0.p0-202510051243-abc1234";
        public string? CommitShort => "abc1234";
        public DateTime? BuildTimestampUtc => new DateTime(2025, 10, 05, 12, 43, 00, DateTimeKind.Utc);
    }

    [TestFixture]
    public class AboutFormTests
    {
        [Test]
        public void AboutForm_displays_expected_fields()
        {
            using var form = new AboutForm(new FakeVersionProvider());
            form.Show(); // ensure handle created

            var lbl = FindLabel(form, "lblInformational");
            lbl.ShouldNotBeNull();
            lbl!.Text.ShouldContain("4.0.p0-202510051243-abc1234");

            FindLabel(form, "lblFileVersion")!.Text.ShouldContain("4.0.12345.1243");
            FindLabel(form, "lblAssemblyVersion")!.Text.ShouldContain("4.0.0.0");
            FindLabel(form, "lblBuild")!.Text.ShouldContain("2025-10-05 12:43");
            FindLabel(form, "lblCommit")!.Text.ShouldContain("abc1234");
        }

        [Test]
        public void Parser_handles_missing_commit_and_bad_patterns()
        {
            VersionInfoParser.TryParseCommitShort("4.0.p0-202510051243").ShouldBeNull();
            VersionInfoParser.TryParseBuildTimestampUtc("4.0.p0").ShouldBeNull();
            var ts = VersionInfoParser.TryParseBuildTimestampUtc("x-202512312359-deadbee");
            ts.ShouldNotBeNull();
            ts!.Value.Kind.ShouldBe(DateTimeKind.Utc);
            ts.Value.Year.ShouldBe(2026); // UTC - will be new year already
            ts.Value.Month.ShouldBe(1);
            ts.Value.Day.ShouldBe(1);
        }

        private static Label? FindLabel(Form f, string name) =>
            f.Controls.Find(name, true).OfType<Label>().FirstOrDefault();
    }
}
