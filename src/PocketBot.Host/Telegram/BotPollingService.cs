using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PocketBot.Host.Telegram;

/// <summary>
/// Connects to Telegram and long-polls for updates until the host stops, handling them one at a time.
/// While Telegram is unreachable (at startup or while polling) it waits with <see cref="PollingBackoff"/>;
/// connecting and every successful poll reset the wait.
/// </summary>
internal sealed partial class BotPollingService(
    BotBootstrapper bootstrapper,
    ITelegramBotClient client,
    BotUpdateHandler updateHandler,
    PollingBackoff backoff,
    TimeProvider time,
    ILogger<BotPollingService> logger) : BackgroundService
{
    /// <summary>How long Telegram may hold a poll open; stays below the HTTP client's default 100s timeout.</summary>
    private const int LongPollSeconds = 50;

    private static readonly UpdateType[] AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await ConnectAsync(stoppingToken);
            await PollAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown.
        }
    }

    /// <summary>
    /// Retries the startup handshake until Telegram is reachable (e.g. the machine booted before its network),
    /// but stops the host when Telegram refuses the bot itself.
    /// </summary>
    private async Task ConnectAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                await bootstrapper.InitializeAsync(stoppingToken);
                backoff.Reset();
                return;
            }
            catch (ApiRequestException ex) when (!TelegramErrors.IsTransient(ex))
            {
                LogStartupRejected(ex, ex.ErrorCode);
                throw;
            }
            catch (Exception ex) when (TelegramErrors.IsTransient(ex))
            {
                var delay = backoff.NextDelay();
                LogStartupFailed(ex, delay.TotalSeconds);
                await Task.Delay(delay, time, stoppingToken);
            }
        }
    }

    private async Task PollAsync(CancellationToken stoppingToken)
    {
        int? offset = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            Update[] updates;
            try
            {
                // The offset confirms every update handled so far, so Telegram stops resending them.
                updates = await client.GetUpdates(
                    offset, timeout: LongPollSeconds, allowedUpdates: AllowedUpdates, cancellationToken: stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                var delay = backoff.NextDelay();
                LogPollingFailed(ex, delay.TotalSeconds);
                await Task.Delay(delay, time, stoppingToken);
                continue;
            }

            backoff.Reset();
            foreach (var update in updates)
            {
                await updateHandler.HandleAsync(update, stoppingToken);
                offset = update.Id + 1;
            }
        }
    }

    [LoggerMessage(LogLevel.Critical,
        "Telegram rejected the bot at startup (HTTP {ErrorCode}). Check Telegram:BotToken from @BotFather")]
    private partial void LogStartupRejected(Exception exception, int errorCode);

    [LoggerMessage(LogLevel.Warning, "Startup handshake with Telegram failed; retrying in {DelaySeconds}s")]
    private partial void LogStartupFailed(Exception exception, double delaySeconds);

    [LoggerMessage(LogLevel.Warning, "Polling Telegram failed; retrying in {DelaySeconds}s")]
    private partial void LogPollingFailed(Exception exception, double delaySeconds);
}
