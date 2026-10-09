namespace PocketBot.Core.Menus;

public enum MenuNodeKind
{
    /// <summary>Opens a page. A submenu without children is an information page (e.g. Help).</summary>
    Submenu,

    /// <summary>Leaf that triggers an <see cref="IMenuAction"/>.</summary>
    Action,
}

public sealed record MenuNode(string Id, string Label, MenuNodeKind Kind, string? Body, IReadOnlyList<MenuNode> Children)
{
    public static MenuNode Submenu(string id, string label, string? body, params MenuNode[] children) =>
        new(id, label, MenuNodeKind.Submenu, body, children);

    public static MenuNode Action(string id, string label) =>
        new(id, label, MenuNodeKind.Action, Body: null, Children: []);
}
