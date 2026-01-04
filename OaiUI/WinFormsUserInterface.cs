// Path: Vectool.UI/OaiUI/WinFormsUserInterface.cs

using VecTool.Configuration.Helpers;
using VecTool.Handlers;

namespace oaiUI
{
    /// <summary>
    /// WinForms implementation of IUserInterface that marshals all UI updates to the UI thread.
    /// </summary>
    public class WinFormsUserInterface : IUserInterface
    {
        private readonly ToolStripStatusLabel _statusLabel;
        private readonly ToolStripProgressBar _progressBar;
        private readonly Control _uiControl;

        public int TotalWork { get; set; }

        public WinFormsUserInterface(ToolStripStatusLabel statusLabel, ToolStripProgressBar progressBar)
        {
            _statusLabel = statusLabel.ThrowIfNull(nameof(statusLabel));
            _progressBar = progressBar.ThrowIfNull(nameof(progressBar));

            var owner = (Control?)_statusLabel.Owner ?? (Control?)_progressBar.Owner;
            _uiControl = owner.ThrowIfNull(nameof(owner), null, "StatusStrip owner not set. Initialize after InitializeComponent().");

            // Initialize safe defaults on the UI thread.
            InvokeOnUi(() =>
            {
                _statusLabel.Text = "Ready";
                _progressBar.Visible = false;
                _progressBar.Minimum = 0;
                _progressBar.Maximum = 1;
                _progressBar.Value = 0;
                _progressBar.Style = ProgressBarStyle.Continuous;
            });
        }

        public void WorkStart(string workText, IEnumerable<string> selectedFolders)
        {
            TotalWork = GetTotalFolders(selectedFolders.ToList());
            InvokeOnUi(() =>
            {
                _statusLabel.Text = workText;
                _progressBar.Visible = true;
                _progressBar.Minimum = 0;
                _progressBar.Maximum = Math.Max(1, TotalWork);
                _progressBar.Value = 0;
                _progressBar.Style = ProgressBarStyle.Continuous;
            });
        }

        public void WorkFinish()
        {
            InvokeOnUi(() =>
            {
                _progressBar.Value = 0;
                _progressBar.Visible = false;
                _statusLabel.Text = "Finished";
            });
        }

        public void ShowMessage(string message, string title = "Information", MessageType type = MessageType.Information)
        {
            var icon = MessageBoxIcon.Information;
            if (type == MessageType.Warning) icon = MessageBoxIcon.Warning;
            else if (type == MessageType.LogError) icon = MessageBoxIcon.Error;

            InvokeOnUi(() =>
            {
                var ownerForm = _uiControl.FindForm();
                if (ownerForm != null)
                {
                    MessageBox.Show(ownerForm, message, title, MessageBoxButtons.OK, icon);
                }
                else
                {
                    MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
                }
            });
        }

        public void UpdateProgress(int current)
        {
            var maximum = Math.Max(1, TotalWork);
            if (current < 0) current = 0;
            if (current > maximum) current = maximum;

            InvokeOnUi(() =>
            {
                _progressBar.Maximum = maximum;
                _progressBar.Value = current;
            });
        }

        public void UpdateStatus(string statusText)
        {
            InvokeOnUi(() => _statusLabel.Text = statusText);
        }

        private void InvokeOnUi(Action action)
        {
            if (_uiControl.InvokeRequired)
            {
                _uiControl.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }

        private static int GetTotalFolders(List<string> selectedFolders)
        {
            if (selectedFolders == null || selectedFolders.Count == 0) return 1;
            var total = 0;
            foreach (var folder in selectedFolders)
            {
                try
                {
                    // Count this folder + subfolders to give a bounded progress scale.
                    total += 1 + Directory.GetDirectories(folder, "*", SearchOption.AllDirectories).Length;
                }
                catch
                {
                    total += 1;
                }
            }
            return Math.Max(1, total);
        }
    }
}