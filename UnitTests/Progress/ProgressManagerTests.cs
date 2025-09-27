using NUnit.Framework;
using Shouldly;
using System;
using oaiUI.Progress;

namespace UnitTests.Progress
{
    internal sealed class FakeClock : oaiUI.Progress.IClock
    {
        public DateTime UtcNow { get; private set; }
        public FakeClock(DateTime start) { UtcNow = start; }
        public void AdvanceSeconds(double s) => UtcNow = UtcNow.AddSeconds(s);
    }

    [TestFixture]
    public class ProgressManagerTests
    {
        [Test]
        public void Start_ShouldInitialize_AndSnapshotIsZero()
        {
            var clk = new FakeClock(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var pm = new ProgressManager(clk);
            pm.Start(100);

            var snap = pm.Snapshot();
            snap.TotalUnits.ShouldBe(100);
            snap.CompletedUnits.ShouldBe(0);
            snap.Elapsed.ShouldBe(TimeSpan.Zero);
        }

        [Test]
        public void Advance_ShouldIncreaseCompleted_AndDecreaseETA()
        {
            var clk = new FakeClock(DateTime.UnixEpoch);
            var pm = new ProgressManager(clk, alpha: 0.5);
            pm.Start(10);

            clk.AdvanceSeconds(2);
            pm.Advance(2, "file1");
            var eta1 = pm.Snapshot().EstimatedRemaining;

            clk.AdvanceSeconds(2);
            pm.Advance(3, "file2");
            var eta2 = pm.Snapshot().EstimatedRemaining;

            eta2.ShouldBeLessThan(eta1);
        }

        [Test]
        public void ZeroTime_ShouldNotCrash_AndETAUnknown()
        {
            var clk = new FakeClock(DateTime.UnixEpoch);
            var pm = new ProgressManager(clk);
            pm.Start(10);
            pm.Advance(1, "file");

            var snap = pm.Snapshot();
            snap.EstimatedRemaining.TotalSeconds.ShouldBeGreaterThanOrEqualTo(0);
        }

        [Test]
        public void Complete_ShouldSetCompletedEqualsTotal()
        {
            var clk = new FakeClock(DateTime.UnixEpoch);
            var pm = new ProgressManager(clk);
            pm.Start(5);
            pm.Advance(3);
            pm.Complete();

            var snap = pm.Snapshot();
            snap.CompletedUnits.ShouldBe(5);
            snap.RemainingUnits.ShouldBe(0);
        }
    }
}
