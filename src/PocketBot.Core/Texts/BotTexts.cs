using PocketBot.Core.Commands;

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

    /// <summary>Copy of <see cref="Menus.DefaultMenu"/>: labels are plain text, bodies are HTML.</summary>
    public static class Menu
    {
        /// <summary>The main menu is titled like the button that leads to it.</summary>
        public const string Main = HomeButton;

        /// <summary>HTML.</summary>
        public const string MainBody = "Chọn chức năng bạn cần 👇";

        /// <summary>Shared by every feature's "list" action.</summary>
        public const string List = "📋 Danh sách";

        public const string Notes = "📝 Ghi chú";

        /// <summary>HTML.</summary>
        public const string NotesBody = "Lưu lại ý tưởng và việc cần nhớ.";

        public const string NotesAdd = "➕ Thêm ghi chú";

        public const string Reminders = "⏰ Nhắc việc";

        /// <summary>HTML.</summary>
        public const string RemindersBody = "Đặt lời nhắc để không bỏ lỡ việc quan trọng.";

        public const string RemindersAdd = "➕ Tạo nhắc việc";

        public const string Finance = "💰 Thu chi";

        /// <summary>HTML.</summary>
        public const string FinanceBody = "Ghi lại khoản thu, khoản chi và xem tổng kết.";

        public const string FinanceIncome = "➕ Khoản thu";
        public const string FinanceExpense = "➖ Khoản chi";
        public const string FinanceSummary = "📊 Tổng kết";

        public const string Settings = "⚙️ Cài đặt";

        /// <summary>HTML.</summary>
        public const string SettingsBody = "Tuỳ chỉnh bot theo ý bạn.";

        public const string SettingsNotifications = "🔔 Thông báo";
        public const string SettingsLanguage = "🌐 Ngôn ngữ";

        public const string Help = "❓ Trợ giúp";

        /// <summary>
        /// HTML. Built from the command descriptions and button labels so its wording cannot drift from them;
        /// a new command still needs its own line here. Interpolated labels must stay free of &lt; &gt; &amp;.
        /// </summary>
        public const string HelpBody =
            $"""
            <b>Các lệnh</b>
            /{CommandNames.Start} — {StartCommandDescription}
            /{CommandNames.Menu} — {MenuCommandDescription}
            /{CommandNames.Help} — {HelpCommandDescription}

            <b>Dùng menu</b>
            • Bấm vào một nhóm để xem chức năng bên trong
            • {BackButton} / {HomeButton} để di chuyển
            • {CloseMenuButton} để thu gọn, {OpenMenuButton} để mở lại đúng trang đang xem
            • Nút {MenuShortcutButton} ở bàn phím luôn sẵn sàng để mở menu mới
            """;
    }
}
