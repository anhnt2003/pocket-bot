using Telegram.Bot.Exceptions;

namespace PocketBot.Host.Telegram;

/// <summary>
/// Classifies Telegram failures. The benign Bot API errors are recognised by their description
/// (Telegram exposes no finer error codes).
/// </summary>
internal static class TelegramErrors
{
    /// <summary>No network, a timeout, rate limiting or Telegram itself failing: worth retrying later.</summary>
    public static bool IsTransient(Exception ex) => ex switch
    {
        ApiRequestException api => api.ErrorCode is 429 or >= 500,
        RequestException or HttpRequestException => true,
        _ => false,
    };

    public static bool IsNotModified(ApiRequestException ex) => Has(ex, "message is not modified");

    public static bool IsUneditable(ApiRequestException ex) =>
        Has(ex, "message to edit not found") || Has(ex, "message can't be edited");

    public static bool IsExpiredQuery(ApiRequestException ex) => Has(ex, "query is too old");

    private static bool Has(ApiRequestException ex, string fragment) =>
        ex.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase);
}
