using PocketBot.Core.Messaging;
using PocketBot.Core.Routing;
using PocketBot.Core.Texts;
using PocketBot.Core.Updates;
using Telegram.Bot.Types;

namespace PocketBot.Host.Telegram;

/// <summary>Glue between the polling loop and Core. Never lets a failing update stop the loop.</summary>
internal sealed partial class BotUpdateHandler(
    UpdateRouter router,
    IChatGateway chat,
    ILogger<BotUpdateHandler> logger)
{
    /// <summary>Routes the update to Core; on failure, logs it and (once the chat is known) tells the user to retry.</summary>
    /// <exception cref="OperationCanceledException">Only once <paramref name="cancellationToken"/> is cancelled (shutdown).</exception>
    public async Task HandleAsync(Update update, CancellationToken cancellationToken)
    {
        IncomingUpdate? incoming = null;
        try
        {
            incoming = TelegramUpdateMapper.Map(update);
            if (incoming is null)
            {
                LogIgnored(update.Id, update.Type);
                return;
            }

            await router.RouteAsync(incoming, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            LogUpdateFailed(ex, update.Id, incoming?.From.Id);
            if (incoming is not null)
            {
                await NotifyFailureAsync(incoming, cancellationToken);
            }
        }
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
    private partial void LogUpdateFailed(Exception exception, int updateId, long? userId);

    [LoggerMessage(LogLevel.Warning, "Could not tell chat {ChatId} about a failure")]
    private partial void LogNotifyFailed(Exception exception, long chatId);
}
