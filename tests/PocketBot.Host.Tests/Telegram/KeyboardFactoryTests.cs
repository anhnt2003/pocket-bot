using PocketBot.Core.Menus;
using PocketBot.Core.Texts;
using PocketBot.Host.Telegram;

namespace PocketBot.Host.Tests.Telegram;

public class KeyboardFactoryTests
{
    [Fact]
    public void Inline_keyboard_mirrors_the_menu_rows_and_callbacks()
    {
        var view = new MenuView("x",
        [
            [new MenuButton("📝 Ghi chú", "nav:notes"), new MenuButton("💰 Thu chi", "nav:finance")],
            [new MenuButton("✖️ Đóng menu", "close:main")],
        ]);

        var keyboard = KeyboardFactory.Inline(view);

        keyboard.InlineKeyboard.Select(row => row.Select(b => $"{b.Text}|{b.CallbackData}"))
            .ShouldBe(
            [
                ["📝 Ghi chú|nav:notes", "💰 Thu chi|nav:finance"],
                ["✖️ Đóng menu|close:main"],
            ]);
    }

    [Fact]
    public void Menu_shortcut_is_a_single_persistent_compact_button()
    {
        var keyboard = KeyboardFactory.MenuShortcut();

        keyboard.Keyboard.ShouldHaveSingleItem().ShouldHaveSingleItem().Text.ShouldBe(BotTexts.MenuShortcutButton);
        keyboard.IsPersistent.ShouldBeTrue();
        keyboard.ResizeKeyboard.ShouldBeTrue();
    }
}
