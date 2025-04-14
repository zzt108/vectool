using DocXHandler;

namespace oaiUI
{
    public class WinFormsUserInterface : IUserInterface
    {
        private readonly ToolStripStatusLabel _statusLabel;
        private readonly ProgressBar _progressBar;

        public WinFormsUserInterface(ToolStripStatusLabel statusLabel, ProgressBar progressBar)
        {
            _statusLabel = statusLabel;
            _progressBar = progressBar;
        }

        public void ShowMessage(string message, string title = "Information", MessageType type = MessageType.Information)
        {
            MessageBoxIcon icon = MessageBoxIcon.Information;
            switch (type)
            {
                case MessageType.Warning:
                    icon = MessageBoxIcon.Warning;
                    break;
                case MessageType.Error:
                    icon = MessageBoxIcon.Error;
                    break;
            }

            MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
        }

        public void UpdateProgress(int current, int maximum)
        {
            if (_progressBar.InvokeRequired)
            {
                _progressBar.Invoke(new Action(() => UpdateProgressInternal(current, maximum)));
            }
            else
            {
                UpdateProgressInternal(current, maximum);
            }
        }

        private void UpdateProgressInternal(int current, int maximum)
        {
            _progressBar.Maximum = maximum;
            _progressBar.Value = current;
            _progressBar.Update();
            Application.DoEvents();
        }

        public void UpdateStatus(string statusText)
        {
            _statusLabel.Text = statusText;
        }
    }
}
