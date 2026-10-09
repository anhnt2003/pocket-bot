using PocketBot.Core.Menus;
using PocketBot.Core.Texts;
using Telegram.Bot.Types.ReplyMarkups;

namespace PocketBot.Host.Telegram;

internal static class KeyboardFactory
{
    public static InlineKeyboardMarkup Inline(MenuView view) =>
        new(view.Rows.Select(row => row.Select(b => InlineKeyboardButton.WithCallbackData(b.Label, b.CallbackData))));

    /// <summary>The always-visible "📋 Menu" button under the text box.</summary>
    public static ReplyKeyboardMarkup MenuShortcut() =>
        new(new KeyboardButton(BotTexts.MenuShortcutButton)) { IsPersistent = true, ResizeKeyboard = true };
}
