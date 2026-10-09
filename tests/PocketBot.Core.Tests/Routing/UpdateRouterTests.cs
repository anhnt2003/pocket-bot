using PocketBot.Core.Menus;
using PocketBot.Core.Tests.Fakes;
using PocketBot.Core.Texts;

namespace PocketBot.Core.Tests.Routing;

public class UpdateRouterTests
{
    private readonly BotHarness _bot = new();

    [Fact]
    public async Task Start_greets_by_name_pins_the_menu_shortcut_and_opens_the_main_menu()
    {
        await _bot.SendText("/start");

        _bot.Chat.Calls.Count.ShouldBe(2);
        var welcome = _bot.Chat.Calls[0].ShouldBeOfType<SentText>();
        welcome.Html.ShouldBe(BotTexts.Welcome("Anh"));
        welcome.Html.ShouldContain("Anh");
        welcome.WithMenuShortcut.ShouldBeTrue();
        _bot.Chat.Calls[1].ShouldBeOfType<SentMenu>().View.ShouldBeEquivalentTo(_bot.Page(DefaultMenu.RootId));
    }

    [Fact]
    public async Task Start_escapes_html_in_the_users_name()
    {
        await _bot.SendText("/start", BotHarness.Me with { FirstName = "<Anh & Co>" });

        _bot.Chat.Calls[0].ShouldBeOfType<SentText>().Html.ShouldContain("<b>&lt;Anh &amp; Co&gt;</b>");
    }

    [Theory]
    [InlineData("/menu")]
    [InlineData(BotTexts.MenuShortcutButton)]
    public async Task Menu_command_or_keyboard_shortcut_sends_a_fresh_main_menu(string text)
    {
        await _bot.SendText(text);

        _bot.Chat.Calls.ShouldHaveSingleItem()
            .ShouldBeOfType<SentMenu>().View.ShouldBeEquivalentTo(_bot.Page(DefaultMenu.RootId));
    }

    [Fact]
    public async Task Help_command_opens_the_help_page()
    {
        await _bot.SendText("/help");

        _bot.Chat.Calls.ShouldHaveSingleItem()
            .ShouldBeOfType<SentMenu>().View.ShouldBeEquivalentTo(_bot.Page(DefaultMenu.HelpId));
    }

    [Fact]
    public async Task Unknown_command_explains_and_points_to_help()
    {
        await _bot.SendText("/xyz");

        var reply = _bot.Chat.Calls.ShouldHaveSingleItem().ShouldBeOfType<SentText>();
        reply.Html.ShouldBe(BotTexts.UnknownCommand("xyz"));
        reply.Html.ShouldContain("/xyz");
        reply.Html.ShouldContain("/help");
    }

    [Fact]
    public async Task Plain_text_is_acknowledged_with_the_message_echoed_safely()
    {
        await _bot.SendText("<b>hi</b> & bye");

        var reply = _bot.Chat.Calls.ShouldHaveSingleItem().ShouldBeOfType<SentText>();
        reply.Html.ShouldBe(BotTexts.Echo("<b>hi</b> & bye"));
        reply.Html.ShouldContain("&lt;b&gt;hi&lt;/b&gt; &amp; bye");
        reply.WithMenuShortcut.ShouldBeFalse();
    }

    [Fact]
    public async Task Non_text_message_gets_a_text_only_notice()
    {
        await _bot.SendUnsupported();

        _bot.Chat.Calls.ShouldHaveSingleItem().ShouldBeOfType<SentText>().Html.ShouldBe(BotTexts.TextOnly);
    }

    [Theory]
    [InlineData("/start")]
    [InlineData("/menu")]
    [InlineData("hello")]
    public async Task Stranger_is_refused_and_shown_their_own_user_id(string text)
    {
        await _bot.SendText(text, BotHarness.Stranger);

        var reply = _bot.Chat.Calls.ShouldHaveSingleItem().ShouldBeOfType<SentText>();
        reply.Html.ShouldBe(BotTexts.AccessDenied(BotHarness.Stranger.Id));
        reply.Html.ShouldContain("<code>666</code>");
    }

    [Fact]
    public async Task Stranger_sending_non_text_is_refused_too()
    {
        await _bot.SendUnsupported(BotHarness.Stranger);

        _bot.Chat.Calls.ShouldHaveSingleItem().ShouldBeOfType<SentText>()
            .Html.ShouldBe(BotTexts.AccessDenied(BotHarness.Stranger.Id));
    }

    [Fact]
    public async Task Stranger_tapping_an_old_menu_only_gets_a_refusal_toast()
    {
        await _bot.Tap("nav:finance", BotHarness.Stranger);

        _bot.Chat.Calls.ShouldHaveSingleItem()
            .ShouldBe(new AnsweredCallback(BotHarness.CallbackId, BotTexts.AccessDeniedToast));
    }
}
