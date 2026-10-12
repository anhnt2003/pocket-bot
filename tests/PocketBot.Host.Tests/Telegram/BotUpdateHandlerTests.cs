using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PocketBot.Core.Texts;
using PocketBot.Host.Telegram;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PocketBot.Host.Tests.Telegram;

/// <summary>The real wiring from update to Bot API request, with only the Telegram HTTP client replaced.</summary>
public sealed class BotUpdateHandlerTests : IDisposable
{
    private const long ChatId = 1001;
    private static readonly User Anh = new() { Id = 42, FirstName = "Anh" };

    private readonly ITelegramBotClient _client = Substitute.For<ITelegramBotClient>();
    private readonly ServiceProvider _provider;
    private readonly BotUpdateHandler _handler;

    public BotUpdateHandlerTests()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telegram:BotToken"] = "123456789:AAHdqTcvCH1vGWJxfSeofSAs0K5PALDsaw",
            ["Telegram:AllowedUserIds:0"] = "42",
        }).Build();
        _provider = new ServiceCollection()
            .AddFakeLogging()
            .AddPocketBot(config)
            .AddSingleton(_client)
            .BuildServiceProvider();
        _handler = _provider.GetRequiredService<BotUpdateHandler>();
    }

    public void Dispose() => _provider.Dispose();

    private FakeLogCollector Logs => _provider.GetFakeLogCollector();

    private static Update TextFromAnh(string text) =>
        new() { Message = new Message { Id = 5, From = Anh, Chat = new Chat { Id = ChatId, Type = ChatType.Private }, Text = text } };

    private void SendingTheMenuFails(Exception failure) =>
        _client.SendRequest(Arg.Is<SendMessageRequest>(r => r.ReplyMarkup is InlineKeyboardMarkup), Arg.Any<CancellationToken>())
            .ThrowsAsync(failure);

    private static RequestException NetworkDown() =>
        new("Bot API Service Failure: HttpRequestException: network down", new HttpRequestException("network down"));

    [Fact]
    public async Task Incoming_message_is_routed_to_the_bot()
    {
        await _handler.HandleAsync(TextFromAnh("/menu"), CancellationToken.None);

        var menu = _client.Sent<SendMessageRequest>().ShouldHaveSingleItem();
        menu.ChatId.Identifier.ShouldBe(ChatId);
        menu.ReplyMarkup.ShouldBeOfType<InlineKeyboardMarkup>();
    }

    [Fact]
    public async Task Update_the_bot_cannot_act_on_is_ignored()
    {
        var edited = new Update { EditedMessage = TextFromAnh("/menu").Message };

        await _handler.HandleAsync(edited, CancellationToken.None);

        _client.SentRequests().ShouldBeEmpty();
    }

    [Fact]
    public async Task Failure_while_handling_is_contained_and_the_user_is_told_to_retry()
    {
        var failure = NetworkDown();
        SendingTheMenuFails(failure);

        await Should.NotThrowAsync(() => _handler.HandleAsync(TextFromAnh("/menu"), CancellationToken.None));

        var notice = _client.Sent<SendMessageRequest>().Last();
        notice.ChatId.Identifier.ShouldBe(ChatId);
        notice.Text.ShouldBe(BotTexts.GenericError);
        notice.ReplyMarkup.ShouldBeNull();
        Logs.GetSnapshot().ShouldContain(r => r.Level == LogLevel.Error && r.Exception == failure);
    }

    [Fact]
    public async Task Failure_to_deliver_the_error_notice_is_contained_too()
    {
        SendingTheMenuFails(NetworkDown());
        _client.SendRequest(Arg.Is<SendMessageRequest>(r => r.Text == BotTexts.GenericError), Arg.Any<CancellationToken>())
            .ThrowsAsync(NetworkDown());

        await Should.NotThrowAsync(() => _handler.HandleAsync(TextFromAnh("/menu"), CancellationToken.None));

        Logs.LatestRecord.Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public async Task Shutdown_cancellation_is_not_reported_as_an_error()
    {
        using var shutdown = new CancellationTokenSource();
        await shutdown.CancelAsync();
        SendingTheMenuFails(new OperationCanceledException(shutdown.Token));

        await Should.ThrowAsync<OperationCanceledException>(() => _handler.HandleAsync(TextFromAnh("/menu"), shutdown.Token));

        _client.Sent<SendMessageRequest>().ShouldNotContain(r => r.Text == BotTexts.GenericError);
        Logs.GetSnapshot().ShouldNotContain(r => r.Level >= LogLevel.Warning);
    }
}
