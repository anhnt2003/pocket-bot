using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PocketBot.Core.Menus;
using PocketBot.Core.Texts;
using PocketBot.Host.Telegram;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PocketBot.Host.Tests.Telegram;

public class TelegramChatGatewayTests
{
    private const long ChatId = 1001;

    private static readonly MenuView Menu = new("<b>🏠 Menu chính</b>", [[new MenuButton("💰 Thu chi", "nav:finance")]]);

    private readonly ITelegramBotClient _client = Substitute.For<ITelegramBotClient>();
    private readonly TelegramChatGateway _gateway;

    public TelegramChatGatewayTests()
    {
        _gateway = new TelegramChatGateway(_client, NullLogger<TelegramChatGateway>.Instance);
    }

    private List<T> Sent<T>() => _client.Sent<T>();

    [Fact]
    public async Task SendText_with_shortcut_sends_html_and_pins_the_menu_button()
    {
        await _gateway.SendTextAsync(ChatId, "<b>Xin chào</b>", withMenuShortcut: true, CancellationToken.None);

        var request = Sent<SendMessageRequest>().ShouldHaveSingleItem();
        request.ChatId.Identifier.ShouldBe(ChatId);
        request.Text.ShouldBe("<b>Xin chào</b>");
        request.ParseMode.ShouldBe(ParseMode.Html);
        request.ReplyMarkup.ShouldBeOfType<ReplyKeyboardMarkup>()
            .Keyboard.Single().Single().Text.ShouldBe(BotTexts.MenuShortcutButton);
    }

    [Fact]
    public async Task SendText_without_shortcut_leaves_the_keyboard_untouched()
    {
        await _gateway.SendTextAsync(ChatId, "hi", withMenuShortcut: false, CancellationToken.None);

        Sent<SendMessageRequest>().ShouldHaveSingleItem().ReplyMarkup.ShouldBeNull();
    }

    [Fact]
    public async Task SendMenu_sends_html_with_inline_buttons()
    {
        await _gateway.SendMenuAsync(ChatId, Menu, CancellationToken.None);

        var request = Sent<SendMessageRequest>().ShouldHaveSingleItem();
        request.ChatId.Identifier.ShouldBe(ChatId);
        request.Text.ShouldBe(Menu.Html);
        request.ParseMode.ShouldBe(ParseMode.Html);
        request.ReplyMarkup.ShouldBeOfType<InlineKeyboardMarkup>()
            .InlineKeyboard.Single().Single().CallbackData.ShouldBe("nav:finance");
    }

    [Fact]
    public async Task EditMenu_replaces_the_menu_message_in_place()
    {
        await _gateway.EditMenuAsync(ChatId, 77, Menu, CancellationToken.None);

        var request = Sent<EditMessageTextRequest>().ShouldHaveSingleItem();
        request.ChatId.Identifier.ShouldBe(ChatId);
        request.MessageId.ShouldBe(77);
        request.Text.ShouldBe(Menu.Html);
        request.ParseMode.ShouldBe(ParseMode.Html);
        request.ReplyMarkup!.InlineKeyboard.Single().Single().CallbackData.ShouldBe("nav:finance");
        Sent<SendMessageRequest>().ShouldBeEmpty();
    }

    [Fact]
    public async Task EditMenu_ignores_a_double_tap_that_changes_nothing()
    {
        EditFails("Bad Request: message is not modified: specified new message content and reply markup are exactly the same");

        await _gateway.EditMenuAsync(ChatId, 77, Menu, CancellationToken.None);

        Sent<SendMessageRequest>().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Bad Request: message to edit not found")]
    [InlineData("Bad Request: message can't be edited")]
    public async Task EditMenu_falls_back_to_a_new_message_when_the_old_one_is_gone(string telegramError)
    {
        EditFails(telegramError);

        await _gateway.EditMenuAsync(ChatId, 77, Menu, CancellationToken.None);

        var fallback = Sent<SendMessageRequest>().ShouldHaveSingleItem();
        fallback.Text.ShouldBe(Menu.Html);
        fallback.ReplyMarkup.ShouldBeOfType<InlineKeyboardMarkup>();
    }

    [Fact]
    public async Task EditMenu_surfaces_unexpected_telegram_errors()
    {
        EditFails("Bad Request: chat not found");

        await Should.ThrowAsync<ApiRequestException>(() => _gateway.EditMenuAsync(ChatId, 77, Menu, CancellationToken.None));
    }

    [Theory]
    [InlineData("🚧 «➕ Khoản thu» đang được phát triển")]
    [InlineData(null)]
    public async Task AnswerCallback_stops_the_spinner_with_an_optional_toast(string? toast)
    {
        await _gateway.AnswerCallbackAsync("cb-1", toast, CancellationToken.None);

        var request = Sent<AnswerCallbackQueryRequest>().ShouldHaveSingleItem();
        request.CallbackQueryId.ShouldBe("cb-1");
        request.Text.ShouldBe(toast);
    }

    [Fact]
    public async Task AnswerCallback_ignores_queries_that_already_expired()
    {
        _client.SendRequest(Arg.Any<AnswerCallbackQueryRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiRequestException("Bad Request: query is too old and response timeout expired or query ID is invalid", 400));

        await Should.NotThrowAsync(() => _gateway.AnswerCallbackAsync("cb-1", null, CancellationToken.None));
    }

    [Fact]
    public async Task AnswerCallback_surfaces_unexpected_telegram_errors()
    {
        _client.SendRequest(Arg.Any<AnswerCallbackQueryRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiRequestException("Unauthorized", 401));

        await Should.ThrowAsync<ApiRequestException>(() => _gateway.AnswerCallbackAsync("cb-1", null, CancellationToken.None));
    }

    private void EditFails(string telegramError) =>
        _client.SendRequest(Arg.Any<EditMessageTextRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiRequestException(telegramError, 400));
}
