namespace PocketBot.Host.Telegram;

/// <summary>Exponential wait between failed polls (1s, 2s, 4s… capped at 30s) so an outage doesn't spin the CPU.</summary>
internal sealed class PollingBackoff
{
    private static readonly TimeSpan Initial = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan Max = TimeSpan.FromSeconds(30);

    private TimeSpan _next = Initial;

    public TimeSpan NextDelay()
    {
        var delay = _next;
        _next = TimeSpan.FromTicks(Math.Min(_next.Ticks * 2, Max.Ticks));
        return delay;
    }

    public void Reset() => _next = Initial;
}
