using Telegram.Bot.Exceptions;

namespace PocketBot.Host.Telegram;

/// <summary>Recognises the benign Bot API errors by their description (Telegram exposes no finer error codes).</summary>
internal static class TelegramErrors
{
    public static bool IsNotModified(ApiRequestException ex) => Has(ex, "message is not modified");

    public static bool IsUneditable(ApiRequestException ex) =>
        Has(ex, "message to edit not found") || Has(ex, "message can't be edited");

    public static bool IsExpiredQuery(ApiRequestException ex) => Has(ex, "query is too old");

    private static bool Has(ApiRequestException ex, string fragment) =>
        ex.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase);
}
