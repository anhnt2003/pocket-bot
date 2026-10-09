using PocketBot.Core.Menus;
using PocketBot.Core.Tests.Fakes;
using PocketBot.Core.Texts;

namespace PocketBot.Core.Tests.Routing;

public class MenuCallbackTests
{
    private readonly BotHarness _bot = new();

    private static AnsweredCallback Answered(string? toast = null) => new(BotHarness.CallbackId, toast);

    private static EditedMenu Edited(MenuView view) => new(BotHarness.ChatId, BotHarness.MenuMessageId, view);

    [Theory]
    [InlineData("nav:finance", "finance")]
    [InlineData("nav:help", "help")]
    [InlineData("nav:main", "main")]
    public async Task Navigating_edits_the_menu_in_place_and_answers_once(string data, string pageId)
    {
        await _bot.Tap(data);

        _bot.Chat.Calls.Count.ShouldBe(2);
        _bot.Chat.Calls[0].ShouldBeOfType<EditedMenu>().ShouldBeEquivalentTo(Edited(_bot.Page(pageId)));
        _bot.Chat.Calls[1].ShouldBe(Answered());
    }

    [Fact]
    public async Task Closing_collapses_the_menu_to_a_single_reopen_button()
    {
        await _bot.Tap("close:finance");

        _bot.Chat.Calls.Count.ShouldBe(2);
        _bot.Chat.Calls[0].ShouldBeOfType<EditedMenu>()
            .ShouldBeEquivalentTo(Edited(MenuRenderer.RenderCollapsed(_bot.Catalog.Find("finance")!)));
        _bot.Chat.Calls[1].ShouldBe(Answered());
    }

    [Fact]
    public async Task Reopening_restores_the_page_that_was_open_before_closing()
    {
        await _bot.Tap("close:finance");
        var reopen = _bot.Chat.Calls.OfType<EditedMenu>().Single().View.Rows.Single().Single();
        _bot.Chat.Calls.Clear();

        await _bot.Tap(reopen.CallbackData);

        _bot.Chat.Calls.Count.ShouldBe(2);
        _bot.Chat.Calls[0].ShouldBeOfType<EditedMenu>().ShouldBeEquivalentTo(Edited(_bot.Page("finance")));
        _bot.Chat.Calls[1].ShouldBe(Answered());
    }

    [Fact]
    public async Task Unbuilt_action_shows_a_coming_soon_toast_without_touching_the_menu()
    {
        await _bot.Tap("act:finance.income");

        _bot.Chat.Calls.ShouldHaveSingleItem().ShouldBe(Answered(BotTexts.ComingSoon("➕ Khoản thu")));
        BotTexts.ComingSoon("➕ Khoản thu").ShouldContain("➕ Khoản thu");
    }

    [Fact]
    public async Task Registered_feature_action_runs_and_its_toast_is_shown()
    {
        var summary = new RecordingAction("finance.summary", toast: "📊 Tháng này: 0đ");
        var bot = new BotHarness(summary);

        await bot.Tap("act:finance.summary");

        summary.Received.ShouldHaveSingleItem().Data.ShouldBe("act:finance.summary");
        bot.Chat.Calls.ShouldHaveSingleItem().ShouldBe(Answered("📊 Tháng này: 0đ"));
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("nav:removed-page")]
    [InlineData("act:removed-action")]
    [InlineData("nav:finance.income")]
    [InlineData("open:finance.income")]
    [InlineData("act:finance")]
    public async Task Stale_or_invalid_button_resets_to_the_main_menu_with_a_notice(string data)
    {
        await _bot.Tap(data);

        _bot.Chat.Calls.Count.ShouldBe(2);
        _bot.Chat.Calls[0].ShouldBeOfType<EditedMenu>().ShouldBeEquivalentTo(Edited(_bot.Page(DefaultMenu.RootId)));
        _bot.Chat.Calls[1].ShouldBe(Answered(BotTexts.MenuOutdated));
    }

    [Fact]
    public async Task Callback_is_still_answered_when_editing_the_menu_fails()
    {
        _bot.Chat.FailEditsWith = new InvalidOperationException("telegram is down");

        await Should.ThrowAsync<InvalidOperationException>(() => _bot.Tap("nav:finance"));

        _bot.Chat.Calls.ShouldHaveSingleItem().ShouldBe(Answered());
    }

    private sealed class RecordingAction(string nodeId, string? toast) : IMenuAction
    {
        public List<Updates.IncomingCallback> Received { get; } = [];

        public string NodeId => nodeId;

        public Task<string?> ExecuteAsync(Updates.IncomingCallback callback, CancellationToken cancellationToken)
        {
            Received.Add(callback);
            return Task.FromResult(toast);
        }
    }
}
