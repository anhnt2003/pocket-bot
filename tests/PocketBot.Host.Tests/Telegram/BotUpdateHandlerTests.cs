using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PocketBot.Core;
using PocketBot.Core.Access;
using PocketBot.Core.Menus;
using PocketBot.Core.Messaging;
using PocketBot.Core.Routing;
using PocketBot.Core.Texts;
using PocketBot.Host.Telegram;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PocketBot.Host.Tests.Telegram;

public class BotUpdateHandlerTests
{
    private const long ChatId = 1001;
    private static readonly User Anh = new() { Id = 42, FirstName = "Anh" };

    private readonly IChatGateway _chat = Substitute.For<IChatGateway>();
    private readonly ITelegramBotClient _client = Substitute.For<ITelegramBotClient>();
    private readonly FakeTimeProvider _time = new();
    private readonly BotUpdateHandler _handler;

    public BotUpdateHandlerTests()
    {
        var router = new ServiceCollection()
            .AddPocketBotCore()
            .AddSingleton(new AccessPolicy([Anh.Id]))
            .AddSingleton(_chat)
            .BuildServiceProvider()
            .GetRequiredService<UpdateRouter>();
        _handler = new BotUpdateHandler(router, _chat, new PollingBackoff(), _time, NullLogger<BotUpdateHandler>.Instance);
    }

    private static Update TextFromAnh(string text) =>
        new() { Message = new Message { Id = 5, From = Anh, Chat = new Chat { Id = ChatId, Type = ChatType.Private }, Text = text } };

    [Fact]
    public async Task Incoming_message_is_routed_to_the_bot()
    {
        await _handler.HandleUpdateAsync(_client, TextFromAnh("/menu"), CancellationToken.None);

        await _chat.Received(1).SendMenuAsync(ChatId, Arg.Any<MenuView>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_the_bot_cannot_act_on_is_ignored()
    {
        var edited = new Update { EditedMessage = TextFromAnh("/menu").Message };

        await _handler.HandleUpdateAsync(_client, edited, CancellationToken.None);

        _chat.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Failure_while_handling_is_contained_and_the_user_is_told_to_retry()
    {
        _chat.SendMenuAsync(Arg.Any<long>(), Arg.Any<MenuView>(), Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("network down"));

        await Should.NotThrowAsync(() => _handler.HandleUpdateAsync(_client, TextFromAnh("/menu"), CancellationToken.None));

        await _chat.Received(1).SendTextAsync(ChatId, BotTexts.GenericError, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failure_to_deliver_the_error_notice_is_contained_too()
    {
        _chat.SendMenuAsync(Arg.Any<long>(), Arg.Any<MenuView>(), Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("network down"));
        _chat.SendTextAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("still down"));

        await Should.NotThrowAsync(() => _handler.HandleUpdateAsync(_client, TextFromAnh("/menu"), CancellationToken.None));
    }

    [Fact]
    public async Task Shutdown_cancellation_is_not_reported_as_an_error()
    {
        using var shutdown = new CancellationTokenSource();
        await shutdown.CancelAsync();
        _chat.SendMenuAsync(Arg.Any<long>(), Arg.Any<MenuView>(), Arg.Any<CancellationToken>()).ThrowsAsync(new OperationCanceledException(shutdown.Token));

        await Should.ThrowAsync<OperationCanceledException>(() => _handler.HandleUpdateAsync(_client, TextFromAnh("/menu"), shutdown.Token));

        await _chat.DidNotReceive().SendTextAsync(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Polling_errors_wait_longer_each_time()
    {
        var first = _handler.HandleErrorAsync(_client, new HttpRequestException(), HandleErrorSource.PollingError, CancellationToken.None);
        await WaitsFor(first, TimeSpan.FromSeconds(1));

        var second = _handler.HandleErrorAsync(_client, new HttpRequestException(), HandleErrorSource.PollingError, CancellationToken.None);
        await WaitsFor(second, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task A_successful_update_resets_the_polling_backoff()
    {
        await WaitsFor(
            _handler.HandleErrorAsync(_client, new HttpRequestException(), HandleErrorSource.PollingError, CancellationToken.None),
            TimeSpan.FromSeconds(1));

        await _handler.HandleUpdateAsync(_client, TextFromAnh("/menu"), CancellationToken.None);

        await WaitsFor(
            _handler.HandleErrorAsync(_client, new HttpRequestException(), HandleErrorSource.PollingError, CancellationToken.None),
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Non_polling_errors_are_logged_without_delaying()
    {
        var task = _handler.HandleErrorAsync(_client, new InvalidOperationException(), HandleErrorSource.FatalError, CancellationToken.None);

        task.IsCompleted.ShouldBeTrue();
        await task;
    }

    private async Task WaitsFor(Task task, TimeSpan delay)
    {
        _time.Advance(delay - TimeSpan.FromMilliseconds(1));
        task.IsCompleted.ShouldBeFalse();

        _time.Advance(TimeSpan.FromMilliseconds(1));
        await task.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
