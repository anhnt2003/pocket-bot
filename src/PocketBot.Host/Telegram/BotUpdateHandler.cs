using PocketBot.Core.Messaging;
using PocketBot.Core.Routing;
using PocketBot.Core.Texts;
using PocketBot.Core.Updates;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace PocketBot.Host.Telegram;

/// <summary>
/// Glue between Telegram's polling loop and Core. Never lets a failing update stop the loop,
/// and slows polling down while Telegram is unreachable.
/// </summary>
internal sealed partial class BotUpdateHandler(
    UpdateRouter router,
    IChatGateway chat,
    PollingBackoff backoff,
    TimeProvider time,
    ILogger<BotUpdateHandler> logger) : IUpdateHandler
{
    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        backoff.Reset();
        if (TelegramUpdateMapper.Map(update) is not { } incoming)
        {
            LogIgnored(update.Id, update.Type);
            return;
        }

        try
        {
            await router.RouteAsync(incoming, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            LogUpdateFailed(ex, update.Id, incoming.From.Id);
            await NotifyFailureAsync(incoming, cancellationToken);
        }
    }

    public async Task HandleErrorAsync(
        ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        if (source != HandleErrorSource.PollingError)
        {
            LogReceiverError(exception, source);
            return;
        }

        var delay = backoff.NextDelay();
        LogPollingFailed(exception, delay.TotalSeconds);
        await Task.Delay(delay, time, cancellationToken);
    }

    private async Task NotifyFailureAsync(IncomingUpdate incoming, CancellationToken cancellationToken)
    {
        try
        {
            await chat.SendTextAsync(incoming.ChatId, BotTexts.GenericError, withMenuShortcut: false, cancellationToken);
        }
        catch (Exception ex)
        {
            LogNotifyFailed(ex, incoming.ChatId);
        }
    }

    [LoggerMessage(LogLevel.Debug, "Ignored update {UpdateId} of type {UpdateType}")]
    private partial void LogIgnored(int updateId, global::Telegram.Bot.Types.Enums.UpdateType updateType);

    [LoggerMessage(LogLevel.Error, "Failed to handle update {UpdateId} from user {UserId}")]
    private partial void LogUpdateFailed(Exception exception, int updateId, long userId);

    [LoggerMessage(LogLevel.Warning, "Could not tell chat {ChatId} about a failure")]
    private partial void LogNotifyFailed(Exception exception, long chatId);

    [LoggerMessage(LogLevel.Warning, "Polling Telegram failed; retrying in {DelaySeconds}s")]
    private partial void LogPollingFailed(Exception exception, double delaySeconds);

    [LoggerMessage(LogLevel.Error, "Telegram receiver error ({Source})")]
    private partial void LogReceiverError(Exception exception, HandleErrorSource source);
}
