namespace PocketBot.Core.Texts;

/// <summary>Every user-facing string in one place (Telegram HTML where noted).</summary>
public static class BotTexts
{
    public const string CloseMenuButton = "✖️ Đóng menu";
    public const string BackButton = "⬅️ Quay lại";
    public const string HomeButton = "🏠 Menu chính";
    public const string OpenMenuButton = "📂 Mở menu";

    /// <summary>Persistent reply-keyboard button; tapping it sends this exact text.</summary>
    public const string MenuShortcutButton = "📋 Menu";

    /// <summary>HTML.</summary>
    public const string MenuCollapsed = "📋 <i>Menu đã thu gọn.</i>";

    public static string Welcome(string firstName) =>
        $"""
        👋 Xin chào <b>{TelegramHtml.Escape(firstName)}</b>!
        Mình là <b>Pocket Bot</b> — trợ lý cá nhân của bạn trên Telegram.

        • Bấm <b>{MenuShortcutButton}</b> ở bàn phím bên dưới để mở menu bất cứ lúc nào
        • Gõ /help để xem hướng dẫn
        """;

    public static string UnknownCommand(string command) =>
        $"🤔 Mình chưa hiểu lệnh <code>/{TelegramHtml.Escape(command)}</code>.\nGõ /help để xem hướng dẫn.";

    public static string Echo(string text) =>
        $"""
        💬 Mình đã nhận: «{TelegramHtml.Escape(text)}»

        Gõ /menu hoặc bấm <b>{MenuShortcutButton}</b> để xem các chức năng.
        """;

    /// <summary>HTML.</summary>
    public const string TextOnly = "🙏 Hiện mình chỉ xử lý tin nhắn dạng chữ.";

    /// <summary>Plain text (callback toast).</summary>
    public const string AccessDeniedToast = "⛔ Bạn không có quyền sử dụng bot này.";

    public static string AccessDenied(long userId) =>
        $"""
        ⛔ Đây là bot cá nhân, bạn không có quyền sử dụng.
        🆔 User ID của bạn: <code>{userId}</code>
        """;

    /// <summary>Plain text (callback toast).</summary>
    public static string ComingSoon(string feature) => $"🚧 «{feature}» đang được phát triển";

    /// <summary>Plain text (callback toast).</summary>
    public const string MenuOutdated = "🔄 Menu đã được cập nhật";

    /// <summary>HTML.</summary>
    public const string GenericError = "⚠️ Có lỗi xảy ra, bạn thử lại nhé.";

    /// <summary>Descriptions shown in Telegram's "/" command list.</summary>
    public const string StartCommandDescription = "Khởi động bot";
    public const string MenuCommandDescription = "Mở menu chính";
    public const string HelpCommandDescription = "Hướng dẫn sử dụng";
}
