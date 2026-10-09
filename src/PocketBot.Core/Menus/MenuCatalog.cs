namespace PocketBot.Core.Menus;

/// <summary>Indexed, read-only view over the menu tree.</summary>
public sealed class MenuCatalog
{
    private readonly Dictionary<string, MenuNode> _nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MenuNode> _parents = new(StringComparer.Ordinal);

    public MenuCatalog(MenuNode root)
    {
        Root = root;
        Index(root, parent: null);
    }

    public MenuNode Root { get; }

    public MenuNode? Find(string id) => _nodes.GetValueOrDefault(id);

    public MenuNode? ParentOf(string id) => _parents.GetValueOrDefault(id);

    /// <summary>Nodes from the root down to <paramref name="id"/> (inclusive) — the breadcrumb trail.</summary>
    public IReadOnlyList<MenuNode> PathTo(string id)
    {
        var path = new List<MenuNode>();
        for (var node = Find(id); node is not null; node = ParentOf(node.Id))
        {
            path.Add(node);
        }

        path.Reverse();
        return path;
    }

    private void Index(MenuNode node, MenuNode? parent)
    {
        if (!_nodes.TryAdd(node.Id, node))
        {
            throw new ArgumentException($"Duplicate menu id '{node.Id}'.", nameof(node));
        }

        if (parent is not null)
        {
            _parents.Add(node.Id, parent);
        }

        foreach (var child in node.Children)
        {
            Index(child, node);
        }
    }
}
