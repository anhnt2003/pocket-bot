using PocketBot.Core.Menus;

namespace PocketBot.Core.Tests.Menus;

public class MenuCatalogTests
{
    private static readonly MenuCatalog Catalog = new(
        MenuNode.Submenu("main", "🏠 Menu chính", "Chọn chức năng",
            MenuNode.Submenu("finance", "💰 Thu chi", "Ghi thu chi",
                MenuNode.Action("finance.income", "➕ Khoản thu")),
            MenuNode.Submenu("help", "❓ Trợ giúp", "Hướng dẫn")));

    [Fact]
    public void Find_returns_a_nested_node_by_id()
    {
        var node = Catalog.Find("finance.income");

        node.ShouldNotBeNull();
        node.Label.ShouldBe("➕ Khoản thu");
        node.Kind.ShouldBe(MenuNodeKind.Action);
    }

    [Fact]
    public void Find_returns_null_for_an_unknown_id()
    {
        Catalog.Find("does-not-exist").ShouldBeNull();
    }

    [Fact]
    public void PathTo_lists_nodes_from_root_down_to_the_target()
    {
        Catalog.PathTo("finance.income").Select(n => n.Id)
            .ShouldBe(["main", "finance", "finance.income"]);
    }

    [Fact]
    public void PathTo_root_contains_only_the_root()
    {
        Catalog.PathTo("main").Select(n => n.Id).ShouldBe(["main"]);
    }

    [Fact]
    public void ParentOf_returns_the_enclosing_node()
    {
        Catalog.ParentOf("finance.income")!.Id.ShouldBe("finance");
    }

    [Fact]
    public void ParentOf_root_is_null()
    {
        Catalog.ParentOf("main").ShouldBeNull();
    }

    [Fact]
    public void Constructor_rejects_duplicate_ids_with_a_clear_message()
    {
        var tree = MenuNode.Submenu("main", "Main", null,
            MenuNode.Submenu("finance", "A", "a"),
            MenuNode.Action("finance", "B"));

        var error = Should.Throw<ArgumentException>(() => new MenuCatalog(tree));
        error.Message.ShouldContain("Duplicate menu id 'finance'");
    }
}
