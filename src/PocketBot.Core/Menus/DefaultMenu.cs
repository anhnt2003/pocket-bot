namespace PocketBot.Core.Menus;

/// <summary>
/// The bot's menu tree. Bodies are Telegram HTML. To plug a feature in, register an
/// <see cref="IMenuAction"/> whose NodeId matches one of the action ids below.
/// </summary>
public static class DefaultMenu
{
    public const string RootId = "main";
    public const string HelpId = "help";

    public static MenuNode Build() =>
        MenuNode.Submenu(RootId, "🏠 Menu chính", "Chọn chức năng bạn cần 👇",
            MenuNode.Submenu("notes", "📝 Ghi chú", "Lưu lại ý tưởng và việc cần nhớ.",
                MenuNode.Action("notes.add", "➕ Thêm ghi chú"),
                MenuNode.Action("notes.list", "📋 Danh sách")),
            MenuNode.Submenu("reminders", "⏰ Nhắc việc", "Đặt lời nhắc để không bỏ lỡ việc quan trọng.",
                MenuNode.Action("reminders.add", "➕ Tạo nhắc việc"),
                MenuNode.Action("reminders.list", "📋 Danh sách")),
            MenuNode.Submenu("finance", "💰 Thu chi", "Ghi lại khoản thu, khoản chi và xem tổng kết.",
                MenuNode.Action("finance.income", "➕ Khoản thu"),
                MenuNode.Action("finance.expense", "➖ Khoản chi"),
                MenuNode.Action("finance.summary", "📊 Tổng kết")),
            MenuNode.Submenu("settings", "⚙️ Cài đặt", "Tuỳ chỉnh bot theo ý bạn.",
                MenuNode.Action("settings.notify", "🔔 Thông báo"),
                MenuNode.Action("settings.lang", "🌐 Ngôn ngữ")),
            MenuNode.Submenu(HelpId, "❓ Trợ giúp",
                """
                <b>Các lệnh</b>
                /start — Khởi động bot
                /menu — Mở menu chính
                /help — Xem hướng dẫn này

                <b>Dùng menu</b>
                • Bấm vào một nhóm để xem chức năng bên trong
                • ⬅️ Quay lại / 🏠 Menu chính để di chuyển
                • ✖️ Đóng menu để thu gọn, 📂 Mở menu để mở lại đúng trang đang xem
                • Nút 📋 Menu ở bàn phím luôn sẵn sàng để mở menu mới
                """));
}
