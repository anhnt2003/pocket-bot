using Copy = PocketBot.Core.Texts.BotTexts.Menu;

namespace PocketBot.Core.Menus;

/// <summary>
/// The bot's menu tree; its copy lives in <see cref="Copy"/>. To plug a feature in, register an
/// <see cref="IMenuAction"/> whose NodeId matches one of the action ids below.
/// </summary>
public static class DefaultMenu
{
    public const string RootId = "main";
    public const string HelpId = "help";

    public static MenuNode Build() =>
        MenuNode.Submenu(RootId, Copy.Main, Copy.MainBody,
            MenuNode.Submenu("notes", Copy.Notes, Copy.NotesBody,
                MenuNode.Action("notes.add", Copy.NotesAdd),
                MenuNode.Action("notes.list", Copy.List)),
            MenuNode.Submenu("reminders", Copy.Reminders, Copy.RemindersBody,
                MenuNode.Action("reminders.add", Copy.RemindersAdd),
                MenuNode.Action("reminders.list", Copy.List)),
            MenuNode.Submenu("finance", Copy.Finance, Copy.FinanceBody,
                MenuNode.Action("finance.income", Copy.FinanceIncome),
                MenuNode.Action("finance.expense", Copy.FinanceExpense),
                MenuNode.Action("finance.summary", Copy.FinanceSummary)),
            MenuNode.Submenu("settings", Copy.Settings, Copy.SettingsBody,
                MenuNode.Action("settings.notify", Copy.SettingsNotifications),
                MenuNode.Action("settings.lang", Copy.SettingsLanguage)),
            MenuNode.Submenu(HelpId, Copy.Help, Copy.HelpBody));
}
