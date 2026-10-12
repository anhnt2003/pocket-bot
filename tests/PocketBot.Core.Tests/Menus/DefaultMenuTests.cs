using System.Reflection;
using System.Text;
using PocketBot.Core.Commands;
using PocketBot.Core.Menus;
using PocketBot.Core.Texts;

namespace PocketBot.Core.Tests.Menus;

public class DefaultMenuTests
{
    private static readonly MenuCatalog Catalog = new(DefaultMenu.Build());

    private static string HelpBody => Catalog.Find(DefaultMenu.HelpId)!.Body.ShouldNotBeNull();

    private static IEnumerable<MenuNode> AllNodes(MenuNode node) =>
        node.Children.SelectMany(AllNodes).Prepend(node);

    [Fact]
    public void Main_menu_offers_the_five_feature_groups_in_order()
    {
        Catalog.Root.Id.ShouldBe(DefaultMenu.RootId);
        Catalog.Root.Children.Select(n => n.Id)
            .ShouldBe(["notes", "reminders", "finance", "settings", DefaultMenu.HelpId]);
    }

    [Fact]
    public void Main_menu_is_titled_exactly_like_the_home_button_that_leads_to_it()
    {
        Catalog.Root.Label.ShouldBe(BotTexts.HomeButton);
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

    /// <summary>The descriptions the host publishes as Telegram's "/" list (BotBootstrapper); that side is not checked here.</summary>
    [Theory]
    [InlineData(CommandNames.Start, BotTexts.StartCommandDescription)]
    [InlineData(CommandNames.Menu, BotTexts.MenuCommandDescription)]
    [InlineData(CommandNames.Help, BotTexts.HelpCommandDescription)]
    public void Help_lists_each_command_with_the_description_telegrams_command_list_shows(string command, string description)
    {
        HelpBody.ShouldContain($"/{command} — {TelegramHtml.Escape(description)}", Case.Sensitive);
    }

    [Theory]
    [InlineData(BotTexts.BackButton)]
    [InlineData(BotTexts.HomeButton)]
    [InlineData(BotTexts.CloseMenuButton)]
    [InlineData(BotTexts.OpenMenuButton)]
    [InlineData(BotTexts.MenuShortcutButton)]
    public void Help_names_each_button_by_its_actual_label(string button)
    {
        HelpBody.ShouldContain(TelegramHtml.Escape(button), Case.Sensitive);
    }

    [Fact]
    public void Every_submenu_has_children_or_a_body()
    {
        AllNodes(Catalog.Root)
            .Where(n => n.Kind == MenuNodeKind.Submenu)
            .ShouldAllBe(n => n.Children.Count > 0 || !string.IsNullOrWhiteSpace(n.Body));
    }

    /// <summary>Compares by value: catches new wording typed inline, not a re-typed copy of an existing constant.</summary>
    [Fact]
    public void Every_label_and_body_is_copy_from_BotTexts()
    {
        static IEnumerable<Type> WithNested(Type type) => type.GetNestedTypes().SelectMany(WithNested).Prepend(type);

        var botTexts = WithNested(typeof(BotTexts))
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
            .Where(f => f.IsLiteral)
            .Select(f => f.GetRawConstantValue())
            .OfType<string>()
            .ToHashSet();

        AllNodes(Catalog.Root)
            .SelectMany(n => new[] { n.Label, n.Body })
            .OfType<string>()
            .Where(text => !botTexts.Contains(text))
            .ShouldBeEmpty();
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
