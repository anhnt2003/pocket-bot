using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PocketBot.Core.Messaging;
using PocketBot.Core.Texts;
using PocketBot.Host.Configuration;
using PocketBot.Host.Telegram;
using PocketBot.Host.Tests.Telegram;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PocketBot.Host.Tests;

/// <summary>The fully wired bot, with only the Telegram HTTP client replaced (and fake time and logs).</summary>
public sealed class PocketBotHostTests : IDisposable
{
    private const string ValidToken = "123456789:AAHdqTcvCH1vGWJxfSeofSAs0K5PALDsaw";
    private const long Me = 42;

    private static readonly User BotAccount = new() { Id = 7, IsBot = true, FirstName = "Pocket", Username = "pocket_bot" };

    private readonly ITelegramBotClient _telegram = Substitute.For<ITelegramBotClient>();
    private readonly WaitTrackingTimeProvider _time = new();
    private readonly Queue<Func<CancellationToken, Task<Update[]>>> _polls = new();
    private readonly CancellationTokenSource _shutdown = new();

    public PocketBotHostTests()
    {
        _telegram.SendRequest(Arg.Any<GetMeRequest>(), Arg.Any<CancellationToken>()).Returns(BotAccount);
        _telegram.SendRequest(Arg.Any<GetUpdatesRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => NextPoll(call.Arg<CancellationToken>()));
    }

    public void Dispose() => _shutdown.Dispose();

    private static IConfiguration Config(string? token = ValidToken) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telegram:BotToken"] = token,
            ["Telegram:AllowedUserIds:0"] = Me.ToString(System.Globalization.CultureInfo.InvariantCulture),
        }).Build();

    private ServiceProvider Build(IConfiguration config, bool fakeTelegram = true)
    {
        var services = new ServiceCollection().AddFakeLogging().AddPocketBot(config).AddSingleton<TimeProvider>(_time);
        if (fakeTelegram)
        {
            services.AddSingleton(_telegram);
        }

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    [Fact]
    public void Composition_wires_the_telegram_adapter_behind_core()
    {
        using var provider = Build(Config(), fakeTelegram: false);

        provider.GetRequiredService<ITelegramBotClient>().ShouldBeOfType<TelegramBotClient>();
        provider.GetRequiredService<IChatGateway>().ShouldBeOfType<TelegramChatGateway>();
        provider.GetServices<IHostedService>().ShouldHaveSingleItem().ShouldBeOfType<BotPollingService>();
        provider.GetRequiredService<IOptions<TelegramOptions>>().Value.AllowedUserIds.ShouldBe([Me]);
    }

    [Fact]
    public void Missing_token_fails_options_validation()
    {
        using var provider = Build(Config(token: ""));

        Should.Throw<OptionsValidationException>(() => provider.GetRequiredService<IOptions<TelegramOptions>>().Value);
    }

    [Fact]
    public async Task Polled_menu_command_is_answered_with_the_main_menu()
    {
        using var stop = new CancellationTokenSource();
        var menuCommand = new Update
        {
            Id = 1,
            Message = new Message
            {
                Id = 5,
                From = new User { Id = Me, FirstName = "Anh" },
                Chat = new Chat { Id = 1001, Type = ChatType.Private },
                Text = "/menu",
            },
        };
        var polls = 0;
        _telegram.SendRequest(Arg.Any<GetUpdatesRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (++polls == 1)
            {
                return [menuCommand];
            }

            stop.Cancel();
            return Array.Empty<Update>();
        });
        using var provider = Build(Config());
        var service = provider.GetServices<IHostedService>().Single();

        await service.StartAsync(stop.Token);
        await WhenStopped(stop.Token);
        await service.StopAsync(CancellationToken.None);

        var reply = _telegram.Sent<SendMessageRequest>().ShouldHaveSingleItem();
        reply.ChatId.Identifier.ShouldBe(1001);
        reply.ReplyMarkup.ShouldBeOfType<InlineKeyboardMarkup>();
    }

    [Fact]
    public async Task Rejected_token_stops_the_bot_instead_of_polling()
    {
        _telegram.SendRequest(Arg.Any<GetMeRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiRequestException("Unauthorized", 401));
        using var provider = Build(Config());
        var service = (BackgroundService)provider.GetServices<IHostedService>().Single();

        await service.StartAsync(CancellationToken.None);

        await Should.ThrowAsync<ApiRequestException>(() => service.ExecuteTask!);
        _telegram.Sent<GetUpdatesRequest>().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(401, "Unauthorized")]
    [InlineData(404, "Not Found")]
    [InlineData(400, "Bad Request: invalid parameter")]
    public async Task Startup_refused_by_telegram_is_reported_as_critical_and_not_retried(int errorCode, string description)
    {
        ConnectingFails(new ApiRequestException(description, errorCode));
        using var provider = Build(Config());
        var bot = await StartBot(provider);

        await CrashedWith<ApiRequestException>(bot);

        _telegram.Sent<GetMeRequest>().ShouldHaveSingleItem();
        _time.WaitsStarted.ShouldBe(0);
        provider.GetFakeLogCollector().LatestRecord.Level.ShouldBe(LogLevel.Critical);
    }

    [Fact]
    public async Task Unexpected_startup_errors_stop_the_bot_instead_of_being_retried()
    {
        ConnectingFails(new InvalidOperationException("bug"));
        using var provider = Build(Config());
        var bot = await StartBot(provider);

        await CrashedWith<InvalidOperationException>(bot);

        _time.WaitsStarted.ShouldBe(0);
        _telegram.Sent<GetUpdatesRequest>().ShouldBeEmpty();
    }

    public static TheoryData<Exception> TransientStartupFailures => new()
    {
        // No network yet (Telegram.Bot wraps HttpClient failures and timeouts)
        new RequestException("Bot API Service Failure: HttpRequestException: nodename nor servname provided", new HttpRequestException()),
        new RequestException("Bot API Request timed out", new TaskCanceledException()),
        new HttpRequestException("Network is unreachable"),
        // Telegram throttling us or having trouble itself
        new ApiRequestException("Too Many Requests: retry after 5", 429),
        new ApiRequestException("Internal Server Error", 500),
        new ApiRequestException("Bad Gateway", 502),
    };

    [Theory]
    [MemberData(nameof(TransientStartupFailures))]
    public async Task Startup_keeps_retrying_transient_failures_with_growing_waits_then_polls(Exception failure)
    {
        ConnectingFails(failure, failure);
        using var provider = Build(Config());
        var bot = await StartBot(provider);

        await WaitsFor(seconds: 1);
        await WaitsFor(seconds: 2);
        await StoppedByItself(bot);

        _telegram.Sent<GetMeRequest>().Count.ShouldBe(3);
        _telegram.Sent<GetUpdatesRequest>().ShouldNotBeEmpty();
        Warnings(provider).Select(r => r.GetStructuredStateValue("DelaySeconds")).ShouldBe(["1", "2"]);
    }

    [Fact]
    public async Task Waits_start_over_from_one_second_once_connected()
    {
        ConnectingFails(NetworkDown(), NetworkDown());
        Polls(Fails);
        using var provider = Build(Config());
        var bot = await StartBot(provider);

        await WaitsFor(seconds: 1); // startup retries
        await WaitsFor(seconds: 2);
        await WaitsFor(seconds: 1); // first polling failure
        await StoppedByItself(bot);
    }

    [Fact]
    public async Task Shutdown_while_waiting_to_retry_startup_exits_cleanly()
    {
        ConnectingFails(NetworkDown());
        using var provider = Build(Config());
        var bot = await StartBot(provider);
        (await _time.NextWait()).ShouldBe(TimeSpan.FromSeconds(1));

        await StopBot(bot);

        bot.ExecuteTask!.IsCompletedSuccessfully.ShouldBeTrue();
        _telegram.Sent<GetUpdatesRequest>().ShouldBeEmpty();
        provider.GetFakeLogCollector().GetSnapshot().ShouldNotContain(r => r.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task Each_batch_is_answered_then_confirmed_by_the_next_poll()
    {
        Polls(Returning(MenuCommand(updateId: 7, chatId: 1001), MenuCommand(updateId: 8, chatId: 1002)), Empty);
        using var provider = Build(Config());

        await StoppedByItself(await StartBot(provider));

        _telegram.Sent<SendMessageRequest>().Select(r => r.ChatId.Identifier).ShouldBe([1001, 1002]);
        _telegram.Sent<GetUpdatesRequest>().Select(r => r.Offset).ShouldBe([null, 9, 9]);
    }

    [Fact]
    public async Task Polls_are_long_and_ask_only_for_messages_and_button_taps()
    {
        Polls(Empty);
        using var provider = Build(Config());

        await StoppedByItself(await StartBot(provider));

        // Telegram must answer a long poll before the HTTP client gives up on it
        var httpTimeout = (int)new TelegramBotClient(ValidToken).Timeout.TotalSeconds;
        foreach (var poll in _telegram.Sent<GetUpdatesRequest>())
        {
            poll.Timeout.ShouldNotBeNull().ShouldBeInRange(1, httpTimeout - 1);
            poll.AllowedUpdates.ShouldBe([UpdateType.Message, UpdateType.CallbackQuery]);
        }
    }

    [Fact]
    public async Task Polling_failures_wait_longer_each_time()
    {
        Polls(Fails, Fails, Fails);
        using var provider = Build(Config());
        var bot = await StartBot(provider);

        await WaitsFor(seconds: 1);
        await WaitsFor(seconds: 2);
        await WaitsFor(seconds: 4);
        await StoppedByItself(bot);

        Warnings(provider).Select(r => r.GetStructuredStateValue("DelaySeconds")).ShouldBe(["1", "2", "4"]);
    }

    [Fact]
    public async Task Any_successful_poll_even_an_empty_one_resets_the_wait_to_one_second()
    {
        Polls(Fails, Fails, Empty, Fails);
        using var provider = Build(Config());
        var bot = await StartBot(provider);

        await WaitsFor(seconds: 1);
        await WaitsFor(seconds: 2);
        await WaitsFor(seconds: 1);
        await StoppedByItself(bot);
    }

    [Fact]
    public async Task A_failing_update_does_not_delay_the_next_poll()
    {
        _telegram.SendRequest(Arg.Is<SendMessageRequest>(r => r.ReplyMarkup is InlineKeyboardMarkup), Arg.Any<CancellationToken>())
            .ThrowsAsync(NetworkDown());
        Polls(Returning(MenuCommand()));
        using var provider = Build(Config());

        await StoppedByItself(await StartBot(provider));

        _telegram.Sent<SendMessageRequest>().Last().Text.ShouldBe(BotTexts.GenericError);
        _telegram.Sent<GetUpdatesRequest>().Count.ShouldBe(2);
        _time.WaitsStarted.ShouldBe(0);
    }

    [Fact]
    public async Task An_update_that_cannot_be_read_is_logged_and_skipped_without_stopping_the_bot()
    {
        // Telegram always names the chat; a bug reading one update must still not stop the bot or come back forever
        var unreadable = MenuCommand(updateId: 7);
        unreadable.Message!.Chat = null!;
        Polls(Returning(unreadable, MenuCommand(updateId: 8)));
        using var provider = Build(Config());

        await StoppedByItself(await StartBot(provider));

        _telegram.Sent<SendMessageRequest>().ShouldHaveSingleItem().ChatId.Identifier.ShouldBe(1001);
        _telegram.Sent<GetUpdatesRequest>().Select(r => r.Offset).ShouldBe([null, 9]);
        _time.WaitsStarted.ShouldBe(0);
        provider.GetFakeLogCollector().GetSnapshot().ShouldContain(r => r.Level == LogLevel.Error);
    }

    [Fact]
    public async Task Shutdown_during_a_long_poll_exits_cleanly()
    {
        var polling = new TaskCompletionSource();
        Polls(cancellationToken =>
        {
            polling.SetResult();
            return LongPollUntilShutdown(cancellationToken);
        });
        using var provider = Build(Config());
        var bot = await StartBot(provider);
        await polling.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        await StopBot(bot);

        bot.ExecuteTask!.IsCompletedSuccessfully.ShouldBeTrue();
        provider.GetFakeLogCollector().GetSnapshot().ShouldNotContain(r => r.Level >= LogLevel.Warning);
    }

    [Fact]
    public async Task Shutdown_while_waiting_to_retry_a_poll_exits_cleanly()
    {
        Polls(Fails);
        using var provider = Build(Config());
        var bot = await StartBot(provider);
        (await _time.NextWait()).ShouldBe(TimeSpan.FromSeconds(1));

        await StopBot(bot);

        bot.ExecuteTask!.IsCompletedSuccessfully.ShouldBeTrue();
        provider.GetFakeLogCollector().GetSnapshot().ShouldNotContain(r => r.Level >= LogLevel.Error);
    }

    private static Update MenuCommand(int updateId = 1, long chatId = 1001) => new()
    {
        Id = updateId,
        Message = new Message
        {
            Id = 5,
            From = new User { Id = Me, FirstName = "Anh" },
            Chat = new Chat { Id = chatId, Type = ChatType.Private },
            Text = "/menu",
        },
    };

    private static RequestException NetworkDown() =>
        new("Bot API Service Failure: HttpRequestException: network down", new HttpRequestException("network down"));

    /// <summary>The first getMe calls fail with these, later ones succeed.</summary>
    private void ConnectingFails(params Exception[] failures)
    {
        var attempts = 0;
        _telegram.SendRequest(Arg.Any<GetMeRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
            attempts < failures.Length ? Task.FromException<User>(failures[attempts++]) : Task.FromResult(BotAccount));
    }

    /// <summary>Scripts what Telegram answers to the next getUpdates calls; once the script runs out, the host shuts down.</summary>
    private void Polls(params Func<CancellationToken, Task<Update[]>>[] polls)
    {
        foreach (var poll in polls)
        {
            _polls.Enqueue(poll);
        }
    }

    private Task<Update[]> NextPoll(CancellationToken cancellationToken)
    {
        if (_polls.TryDequeue(out var poll))
        {
            return poll(cancellationToken);
        }

        _shutdown.Cancel();
        return Task.FromResult(Array.Empty<Update>());
    }

    private static Func<CancellationToken, Task<Update[]>> Returning(params Update[] updates) => _ => Task.FromResult(updates);

    private static Task<Update[]> Empty(CancellationToken cancellationToken) => Task.FromResult(Array.Empty<Update>());

    private static Task<Update[]> Fails(CancellationToken cancellationToken) => Task.FromException<Update[]>(NetworkDown());

    private static async Task<Update[]> LongPollUntilShutdown(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return [];
    }

    private async Task<BackgroundService> StartBot(ServiceProvider provider)
    {
        var bot = (BackgroundService)provider.GetServices<IHostedService>().Single();
        await bot.StartAsync(_shutdown.Token);
        return bot;
    }

    /// <summary>Stops the bot as the host does on shutdown, giving up (instead of hanging) if it ignores the request.</summary>
    private static async Task StopBot(BackgroundService bot)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await bot.StopAsync(timeout.Token);
    }

    /// <summary>The bot ran through the scripted polls and stopped without a fault.</summary>
    private static Task StoppedByItself(BackgroundService bot) => bot.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

    private static Task<T> CrashedWith<T>(BackgroundService bot)
        where T : Exception =>
        Should.ThrowAsync<T>(() => bot.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5)));

    /// <summary>Checks the bot is now waiting exactly this long, then lets the time pass.</summary>
    private async Task WaitsFor(int seconds)
    {
        (await _time.NextWait()).ShouldBe(TimeSpan.FromSeconds(seconds));
        _time.Advance(TimeSpan.FromSeconds(seconds));
    }

    private static IEnumerable<FakeLogRecord> Warnings(ServiceProvider provider) =>
        provider.GetFakeLogCollector().GetSnapshot().Where(r => r.Level == LogLevel.Warning);

    private static Task WhenStopped(CancellationToken token)
    {
        var stopped = new TaskCompletionSource();
        token.Register(stopped.SetResult);
        return stopped.Task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
    }
}
