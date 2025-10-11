using System;
using System.Windows.Forms;
using VecTool.Core.Versioning;

namespace Vectool.OaiUI
{
    public partial class AboutForm : Form
    {
        private readonly IVersionProvider _versions;

        public AboutForm() : this(new AssemblyVersionProvider())
        {
        }

        public AboutForm(IVersionProvider versions)
        {
            _versions = versions ?? throw new ArgumentNullException(nameof(versions));
            InitializeComponent();
            Populate();
        }

        private void Populate()
        {
            this.lblTitle.Text = _versions.ApplicationName;
            this.lblInformational.Text = $"Display: {_versions.InformationalVersion}";
            this.lblFileVersion.Text = $"File: {_versions.FileVersion}";
            this.lblAssemblyVersion.Text = $"Assembly: {_versions.AssemblyVersion}";

            var ts = _versions.BuildTimestampUtc;
            this.lblBuild.Text = ts.HasValue
                ? $"Build: {ts:yyyy-MM-dd HH:mm} UTC"
                : "Build: n/a";

            this.lblCommit.Text = string.IsNullOrWhiteSpace(_versions.CommitShort)
                ? "Commit: n/a"
                : $"Commit: {_versions.CommitShort}";
        }
    }
}
