using PocketBot.Core.Menus;
using PocketBot.Core.Messaging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;

namespace PocketBot.Host.Telegram;

/// <summary>The only place that talks to the Telegram Bot API on Core's behalf.</summary>
internal sealed partial class TelegramChatGateway(ITelegramBotClient client, ILogger<TelegramChatGateway> logger) : IChatGateway
{
    public Task SendTextAsync(long chatId, string html, bool withMenuShortcut, CancellationToken cancellationToken) =>
        client.SendMessage(
            chatId,
            html,
            ParseMode.Html,
            replyMarkup: withMenuShortcut ? KeyboardFactory.MenuShortcut() : null,
            cancellationToken: cancellationToken);

    public Task SendMenuAsync(long chatId, MenuView view, CancellationToken cancellationToken) =>
        client.SendMessage(
            chatId,
            view.Html,
            ParseMode.Html,
            replyMarkup: KeyboardFactory.Inline(view),
            cancellationToken: cancellationToken);

    public async Task EditMenuAsync(long chatId, int messageId, MenuView view, CancellationToken cancellationToken)
    {
        try
        {
            await client.EditMessageText(
                chatId,
                messageId,
                view.Html,
                ParseMode.Html,
                replyMarkup: KeyboardFactory.Inline(view),
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException ex) when (TelegramErrors.IsNotModified(ex))
        {
            // Double tap on the same button: the menu already shows this page.
            LogEditSkipped(chatId, messageId);
        }
        catch (ApiRequestException ex) when (TelegramErrors.IsUneditable(ex))
        {
            // Message deleted or too old to edit (48h): show the menu as a fresh message instead.
            LogEditFallback(chatId, messageId, ex.Message);
            await SendMenuAsync(chatId, view, cancellationToken);
        }
    }

    public async Task AnswerCallbackAsync(string callbackId, string? toast, CancellationToken cancellationToken)
    {
        try
        {
            await client.AnswerCallbackQuery(callbackId, toast, cancellationToken: cancellationToken);
        }
        catch (ApiRequestException ex) when (TelegramErrors.IsExpiredQuery(ex))
        {
            // The user already saw the spinner time out; nothing left to answer.
            LogCallbackExpired(callbackId);
        }
    }

    [LoggerMessage(LogLevel.Debug, "Menu {ChatId}/{MessageId} unchanged, edit skipped")]
    private partial void LogEditSkipped(long chatId, int messageId);

    [LoggerMessage(LogLevel.Information, "Menu {ChatId}/{MessageId} cannot be edited ({Reason}); sent a new one")]
    private partial void LogEditFallback(long chatId, int messageId, string reason);

    [LoggerMessage(LogLevel.Debug, "Callback {CallbackId} expired before it could be answered")]
    private partial void LogCallbackExpired(string callbackId);
}
