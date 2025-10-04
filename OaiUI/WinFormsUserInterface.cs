using VecTool.Handlers;

namespace oaiUI
{
    public class WinFormsUserInterface : IUserInterface
    {
        private readonly ToolStripStatusLabel _statusLabel;
        private readonly ToolStripProgressBar _progressBar;
        public int TotalWork { get; set; }

        public WinFormsUserInterface(ToolStripStatusLabel statusLabel, ToolStripProgressBar progressBar)
        {
            _statusLabel = statusLabel;
            _progressBar = progressBar;
        }

        private static int GetTotalFolders(List<string> selectedFolders)
        {
            return selectedFolders.Sum(folder =>
                            Directory.GetDirectories(folder, "*", SearchOption.AllDirectories).Count());
        }

        public void WorkStart(string str, List<string> selectedFolders)
        {
            TotalWork = GetTotalFolders(selectedFolders);
            _statusLabel.Text = str;
            UpdateProgress(0);
        }

        public void WorkFinish()
        {
            _statusLabel.Text = "Finished " + _statusLabel.Text;
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

        public void UpdateProgress(int current)
        {
            // ToolStripProgressBar doesn't have InvokeRequired, but its parent StatusStrip does
            if (_progressBar.Owner?.InvokeRequired == true)
            {
                _progressBar.Owner.Invoke(new Action(() => UpdateProgressInternal(current, TotalWork)));
            }
            else
            {
                UpdateProgressInternal(current, TotalWork);
            }
        }

        private void UpdateProgressInternal(int current, int maximum)
        {
            _progressBar.Maximum = maximum;
            _progressBar.Value = current;
            Application.DoEvents();
        }

        public void UpdateStatus(string statusText)
        {
            _statusLabel.Text = statusText;
        }
    }
}
