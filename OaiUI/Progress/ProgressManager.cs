namespace oaiUI.Progress
{
    public sealed class ProgressUpdatedEventArgs : EventArgs
    {
        public ProgressInfo LogInformation { get; }
        public string? CurrentItem { get; }
        public ProgressUpdatedEventArgs(ProgressInfo info, string? currentItem)
        {
            LogInformation = info ?? throw new ArgumentNullException(nameof(info));
            CurrentItem = currentItem;
        }
    }

    public interface IProgressReporter
    {
        event EventHandler<ProgressUpdatedEventArgs>? ProgressChanged;
        void Start(int totalUnits);
        void Advance(int delta = 1, string? currentItem = null);
        void SetCurrentItem(string? currentItem);
        void Complete();
        ProgressInfo Snapshot();
    }

    public sealed class ProgressManager : IProgressReporter
    {
        private readonly IClock _clock;
        private readonly object _sync = new object();
        private DateTime _startUtc;
        private int _totalUnits;
        private int _completed;
        private string? _currentItem;
        private double _emaRate; // exponential moving average of items/sec
        private DateTime _lastSampleUtc;
        private int _lastSampleCompleted;

        // Smoothing factor: higher = faster reaction, lower = smoother curve
        private readonly double _alpha;

        public event EventHandler<ProgressUpdatedEventArgs>? ProgressChanged;

        public ProgressManager(IClock? clock = null, double alpha = 0.25)
        {
            _clock = clock ?? new SystemClock();
            _alpha = Math.Clamp(alpha, 0.05, 0.9);
        }

        public void Start(int totalUnits)
        {
            if (totalUnits < 0) throw new ArgumentOutOfRangeException(nameof(totalUnits));
            lock (_sync)
            {
                _totalUnits = totalUnits;
                _completed = 0;
                _currentItem = null;
                _startUtc = _clock.UtcNow;
                _lastSampleUtc = _startUtc;
                _lastSampleCompleted = 0;
                _emaRate = 0.0;
                Raise();
            }
        }

        public void Advance(int delta = 1, string? currentItem = null)
        {
            if (delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
            lock (_sync)
            {
                _completed = Math.Min(_totalUnits, _completed + delta);
                if (!string.IsNullOrWhiteSpace(currentItem))
                {
                    _currentItem = currentItem;
                }
                UpdateRate();
                Raise();
            }
        }

        public void SetCurrentItem(string? currentItem)
        {
            lock (_sync)
            {
                _currentItem = currentItem;
                Raise();
            }
        }

        public void Complete()
        {
            lock (_sync)
            {
                _completed = _totalUnits;
                UpdateRate();
                Raise();
            }
        }

        public ProgressInfo Snapshot()
        {
            lock (_sync)
            {
                return new ProgressInfo(_startUtc, _totalUnits, _completed, _emaRate, _clock);
            }
        }

        private void UpdateRate()
        {
            var now = _clock.UtcNow;
            var dt = (now - _lastSampleUtc).TotalSeconds;
            var dCompleted = _completed - _lastSampleCompleted;

            if (dt > 0.0001)
            {
                var instRate = dCompleted / dt;
                if (_emaRate <= 0)
                    _emaRate = instRate;
                else
                    _emaRate = _alpha * instRate + (1 - _alpha) * _emaRate;

                _lastSampleUtc = now;
                _lastSampleCompleted = _completed;
            }
        }

        private void Raise()
        {
            var snapshot = new ProgressInfo(_startUtc, _totalUnits, _completed, _emaRate, _clock);
            ProgressChanged?.Invoke(this, new ProgressUpdatedEventArgs(snapshot, _currentItem));
        }
    }
}
