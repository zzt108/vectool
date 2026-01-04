// Path: OaiUI/AboutForm.cs
using Vectool.UI.Versioning;
using VecTool.Configuration.Helpers;

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
            _versions = versions.ThrowIfNull(nameof(versions));
            InitializeComponent();
            Populate();

            WireUpCopyToClipboard();
        }

        private void WireUpCopyToClipboard()
        {
            // Make labels look interactive and copy their values on click
            HookCopy(lblInformational);
            HookCopy(lblFileVersion);
            HookCopy(lblAssemblyVersion);
            HookCopy(lblBuild);
            HookCopy(lblCommit);
        }

        private void HookCopy(Label label)
        {
            if (label == null) return;
            label.Cursor = Cursors.Hand;
            label.Click -= LabelCopyOnClick; // idempotent hookup
            label.Click += LabelCopyOnClick;
        }

        // Copies the value-part after ':' if present; otherwise the whole text
        private void LabelCopyOnClick(object? sender, EventArgs e)
        {
            if (sender is not Label l || string.IsNullOrWhiteSpace(l.Text)) return;

            var text = l.Text;
            var value = text;
            var idx = text.IndexOf(':');
            if (idx >= 0 && idx + 1 < text.Length)
                value = text.Substring(idx + 1).Trim();

            try
            {
                Clipboard.SetText(value);
                // Optional: brief visual hint could be added later (status bar, tooltip), but not required
            }
            catch
            {
                // Intentionally swallow to keep this a non-breaking enhancement
            }
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

        // Named click-handler for Designer event binding without renaming existing methods
        private void BtnCloseClick(object? sender, EventArgs e)
        {
            this.Close();
        }
    }
}