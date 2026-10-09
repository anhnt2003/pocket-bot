namespace PocketBot.Core.Menus;

public sealed record MenuButton(string Label, string CallbackData);

/// <summary>A rendered menu page: Telegram HTML text plus inline keyboard rows.</summary>
public sealed record MenuView(string Html, IReadOnlyList<IReadOnlyList<MenuButton>> Rows);
