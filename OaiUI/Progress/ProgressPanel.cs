using System.ComponentModel;
using oaiUI.Progress;

namespace oaiUI.Controls
{
    public sealed class ProgressPanel : UserControl
    {
        private readonly ProgressBar _progressBar;
        private readonly Label _lblCurrentItem;
        private readonly Label _lblTimeRemaining;

        [Browsable(false)]
        public IProgressReporter? Reporter
        {
            get => _reporter;
            set
            {
                if (_reporter != null)
                {
                    _reporter.ProgressChanged -= OnProgressChanged;
                }
                _reporter = value;
                if (_reporter != null)
                {
                    _reporter.ProgressChanged += OnProgressChanged;
                    Render(_reporter.Snapshot(), null);
                }
            }
        }

        private IProgressReporter? _reporter;

        public ProgressPanel()
        {
            Dock = DockStyle.Top;
            Height = 56;
            BackColor = Color.FromArgb(32, 32, 32);

            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 14,
                Style = ProgressBarStyle.Continuous,
                ForeColor = Color.LimeGreen
            };

            var panelLabels = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = Color.FromArgb(32, 32, 32)
            };

            _lblCurrentItem = new Label
            {
                Dock = DockStyle.Left,
                Width = 400,
                ForeColor = Color.Gainsboro,
                Text = "Current: –",
            };

            _lblTimeRemaining = new Label
            {
                Dock = DockStyle.Right,
                Width = 200,
                ForeColor = Color.Gainsboro,
                Text = "ETA: –",
                TextAlign = ContentAlignment.MiddleRight
            };

            panelLabels.Controls.Add(_lblTimeRemaining);
            panelLabels.Controls.Add(_lblCurrentItem);

            Controls.Add(panelLabels);
            Controls.Add(_progressBar);
        }
        private void OnProgressChanged(object? sender, ProgressUpdatedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Render(e.LogInformation, e.CurrentItem)));
            }
            else
            {
                Render(e.LogInformation, e.CurrentItem);
            }
        }

        private void Render(ProgressInfo info, string? currentItem)
        {
            _progressBar.Maximum = Math.Max(1, info.TotalUnits);
            _progressBar.Value = Math.Min(info.TotalUnits, info.CompletedUnits);

            _lblCurrentItem.Text = string.IsNullOrWhiteSpace(currentItem)
                ? "Current: –"
                : $"Current: {currentItem}";

            _lblTimeRemaining.Text = info.CompletedUnits == 0
                ? "ETA: –"
                : $"ETA: {Format(info.EstimatedRemaining)}";
        }

        private static string Format(TimeSpan ts)
        {
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
            return $"{ts.Seconds}s";
        }
    }
}
