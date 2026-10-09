using PocketBot.Host.Telegram;

namespace PocketBot.Host.Tests.Telegram;

public class PollingBackoffTests
{
    [Fact]
    public void Delay_doubles_after_each_failure_and_caps_at_thirty_seconds()
    {
        var backoff = new PollingBackoff();

        Enumerable.Range(0, 7).Select(_ => backoff.NextDelay().TotalSeconds)
            .ShouldBe([1, 2, 4, 8, 16, 30, 30]);
    }

    [Fact]
    public void Reset_starts_over_from_one_second()
    {
        var backoff = new PollingBackoff();
        backoff.NextDelay();
        backoff.NextDelay();

        backoff.Reset();

        backoff.NextDelay().ShouldBe(TimeSpan.FromSeconds(1));
    }
}
