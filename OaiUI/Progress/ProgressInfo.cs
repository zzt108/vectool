namespace oaiUI.Progress
{
    // Time abstraction for testability
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    // Immutable snapshot of progress state
    public sealed class ProgressInfo
    {
        public DateTime StartUtc { get; }
        public int TotalUnits { get; }
        public int CompletedUnits { get; }
        public TimeSpan Elapsed => _clock.UtcNow - StartUtc;
        public int RemainingUnits => Math.Max(0, TotalUnits - CompletedUnits);
        public double ThroughputPerSecond => Elapsed.TotalSeconds <= 0.0 ? 0.0 : CompletedUnits / Elapsed.TotalSeconds;
        public double EmaThroughputPerSecond { get; }

        public TimeSpan EstimatedRemaining
        {
            get
            {
                var rate = Math.Max(EmaThroughputPerSecond, 0.000001);
                return TimeSpan.FromSeconds(RemainingUnits / rate);
            }
        }

        private readonly IClock _clock;

        public ProgressInfo(DateTime startUtc, int totalUnits, int completedUnits, double emaThroughputPerSecond, IClock clock)
        {
            if (totalUnits < 0) throw new ArgumentOutOfRangeException(nameof(totalUnits));
            if (completedUnits < 0) throw new ArgumentOutOfRangeException(nameof(completedUnits));
            if (completedUnits > totalUnits) throw new ArgumentOutOfRangeException(nameof(completedUnits));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            StartUtc = startUtc;
            TotalUnits = totalUnits;
            CompletedUnits = completedUnits;
            EmaThroughputPerSecond = emaThroughputPerSecond < 0 ? 0 : emaThroughputPerSecond;
        }

        public ProgressInfo WithCompleted(int completedUnits, double emaThroughputPerSecond)
            => new ProgressInfo(StartUtc, TotalUnits, Math.Min(Math.Max(0, completedUnits), TotalUnits), emaThroughputPerSecond, _clock);
    }
}
