namespace PocketBot.Core.Texts;

/// <summary>
/// Escaping for Telegram's HTML parse mode. Only &lt; &gt; &amp; are special, so unlike
/// WebUtility.HtmlEncode this keeps Vietnamese diacritics and emoji as-is.
/// </summary>
public static class TelegramHtml
{
    public static string Escape(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
