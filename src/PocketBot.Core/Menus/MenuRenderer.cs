using PocketBot.Core.Texts;

namespace PocketBot.Core.Menus;

/// <summary>Turns a menu page into the text + keyboard the user sees.</summary>
public sealed class MenuRenderer(MenuCatalog catalog)
{
    private const int Columns = 2;

    public MenuView Render(MenuNode page)
    {
        var rows = page.Children
            .Select(ButtonFor)
            .Chunk(Columns)
            .Select(IReadOnlyList<MenuButton> (row) => row)
            .ToList();

        if (NavigationRow(page) is { } navigation)
        {
            rows.Add(navigation);
        }

        rows.Add([Button(BotTexts.CloseMenuButton, new CallbackAction.Close(page.Id))]);

        return new MenuView(Header(page), rows);
    }

    /// <summary>The folded state: one button that reopens <paramref name="page"/> where the user left off.</summary>
    public static MenuView RenderCollapsed(MenuNode page) =>
        new(BotTexts.MenuCollapsed, [[Button(BotTexts.OpenMenuButton, new CallbackAction.Open(page.Id))]]);

    private string Header(MenuNode page)
    {
        var title = $"<b>{TelegramHtml.Escape(page.Label)}</b>";
        var path = catalog.PathTo(page.Id);
        var breadcrumb = path.Count > 1
            ? $"\n<i>{TelegramHtml.Escape(string.Join(" › ", path.Select(n => n.Label)))}</i>"
            : string.Empty;
        return $"{title}{breadcrumb}\n\n{page.Body}";
    }

    /// <summary>Back to the parent; plus a shortcut home when the parent is not already home.</summary>
    private List<MenuButton>? NavigationRow(MenuNode page)
    {
        if (catalog.ParentOf(page.Id) is not { } parent)
        {
            return null;
        }

        List<MenuButton> row = [Button(BotTexts.BackButton, new CallbackAction.Navigate(parent.Id))];
        if (parent.Id != catalog.Root.Id)
        {
            row.Add(Button(BotTexts.HomeButton, new CallbackAction.Navigate(catalog.Root.Id)));
        }

        return row;
    }

    private static MenuButton ButtonFor(MenuNode child) =>
        Button(child.Label, child.Kind == MenuNodeKind.Action
            ? new CallbackAction.Execute(child.Id)
            : new CallbackAction.Navigate(child.Id));

    private static MenuButton Button(string label, CallbackAction action) => new(label, action.Encode());
}
