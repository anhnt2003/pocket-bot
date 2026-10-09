using PocketBot.Core.Updates;
using Telegram.Bot.Types;

namespace PocketBot.Host.Telegram;

/// <summary>Translates Telegram's wide Update type into the few shapes Core understands; null = ignore.</summary>
internal static class TelegramUpdateMapper
{
    public static IncomingUpdate? Map(Update update) => update switch
    {
        { Message: { From: { } from } message } => message.Text is { } text
            ? new IncomingText(message.Chat.Id, ToBotUser(from), message.Id, text)
            : new IncomingUnsupported(message.Chat.Id, ToBotUser(from)),
        { CallbackQuery: { Message: { } message } callback } =>
            new IncomingCallback(message.Chat.Id, ToBotUser(callback.From), message.Id, callback.Id, callback.Data ?? string.Empty),
        _ => null,
    };

    private static BotUser ToBotUser(User user) => new(user.Id, user.FirstName, user.Username);
}
