using System.Threading.Channels;
using Microsoft.Extensions.Time.Testing;

namespace PocketBot.Host.Tests;

/// <summary>
/// Fake time that also tells a test when code running in the background starts waiting on it,
/// so the test only advances the clock once the wait is in place.
/// </summary>
internal sealed class WaitTrackingTimeProvider : FakeTimeProvider
{
    private readonly Channel<TimeSpan> _waits = Channel.CreateUnbounded<TimeSpan>();
    private int _waitsStarted;

    public int WaitsStarted => Volatile.Read(ref _waitsStarted);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        Interlocked.Increment(ref _waitsStarted);
        _waits.Writer.TryWrite(dueTime);
        return timer;
    }

    /// <summary>Length of the next wait the code under test starts (already scheduled once this returns).</summary>
    public async Task<TimeSpan> NextWait() =>
        await _waits.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
}
