using System.Text;
using PocketBot.Core.Menus;

namespace PocketBot.Core.Tests.Menus;

public class DefaultMenuTests
{
    private static readonly MenuCatalog Catalog = new(DefaultMenu.Build());

    private static IEnumerable<MenuNode> AllNodes(MenuNode node) =>
        node.Children.SelectMany(AllNodes).Prepend(node);

    [Fact]
    public void Main_menu_offers_the_five_feature_groups_in_order()
    {
        Catalog.Root.Id.ShouldBe(DefaultMenu.RootId);
        Catalog.Root.Children.Select(n => n.Id)
            .ShouldBe(["notes", "reminders", "finance", "settings", DefaultMenu.HelpId]);
    }

    [Theory]
    [InlineData("notes", new[] { "notes.add", "notes.list" })]
    [InlineData("reminders", new[] { "reminders.add", "reminders.list" })]
    [InlineData("finance", new[] { "finance.income", "finance.expense", "finance.summary" })]
    [InlineData("settings", new[] { "settings.notify", "settings.lang" })]
    public void Each_feature_group_exposes_its_actions(string groupId, string[] actionIds)
    {
        var group = Catalog.Find(groupId)!;

        group.Children.Select(n => n.Id).ShouldBe(actionIds);
        group.Children.ShouldAllBe(n => n.Kind == MenuNodeKind.Action);
    }

    [Fact]
    public void Help_is_an_information_page_with_usage_instructions()
    {
        var help = Catalog.Find(DefaultMenu.HelpId)!;

        help.Kind.ShouldBe(MenuNodeKind.Submenu);
        help.Children.ShouldBeEmpty();
        help.Body.ShouldNotBeNull();
        help.Body.ShouldContain("/menu");
    }

    [Fact]
    public void Every_submenu_has_children_or_a_body()
    {
        AllNodes(Catalog.Root)
            .Where(n => n.Kind == MenuNodeKind.Submenu)
            .ShouldAllBe(n => n.Children.Count > 0 || !string.IsNullOrWhiteSpace(n.Body));
    }

    [Fact]
    public void Every_callback_fits_telegrams_64_byte_limit()
    {
        var callbacks = AllNodes(Catalog.Root).SelectMany(n => new CallbackAction[]
        {
            new CallbackAction.Navigate(n.Id),
            new CallbackAction.Close(n.Id),
            new CallbackAction.Open(n.Id),
            new CallbackAction.Execute(n.Id),
        });

        callbacks.ShouldAllBe(c => Encoding.UTF8.GetByteCount(c.Encode()) <= 64);
    }
}
