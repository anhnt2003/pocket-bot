using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PocketBot.Host.Configuration;
using PocketBot.Host.Telegram;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace PocketBot.Host.Tests.Telegram;

public class BotBootstrapperTests
{
    private readonly ITelegramBotClient _client = Substitute.For<ITelegramBotClient>();
    private readonly FakeLogger<BotBootstrapper> _logger = new();

    public BotBootstrapperTests()
    {
        _client.SendRequest(Arg.Any<GetMeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new User { Id = 7, IsBot = true, FirstName = "Pocket", Username = "pocket_bot" });
    }

    private BotBootstrapper Bootstrapper(bool dropPending = false, long[]? allowed = null) =>
        new(_client, Microsoft.Extensions.Options.Options.Create(new TelegramOptions
        {
            BotToken = "ignored",
            DropPendingUpdates = dropPending,
            AllowedUserIds = allowed ?? [42],
        }), _logger);

    private List<IRequest> Requests() => _client.SentRequests();

    [Fact]
    public async Task Startup_verifies_the_token_then_switches_to_polling_then_publishes_commands()
    {
        var me = await Bootstrapper().InitializeAsync(CancellationToken.None);

        me.Username.ShouldBe("pocket_bot");
        Requests().Select(r => r.GetType()).ShouldBe(
            [typeof(GetMeRequest), typeof(DeleteWebhookRequest), typeof(SetMyCommandsRequest)]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Pending_updates_are_dropped_only_when_configured(bool dropPending)
    {
        await Bootstrapper(dropPending).InitializeAsync(CancellationToken.None);

        Requests().OfType<DeleteWebhookRequest>().Single().DropPendingUpdates.ShouldBe(dropPending);
    }

    [Fact]
    public async Task Telegram_command_menu_lists_start_menu_and_help_in_vietnamese()
    {
        await Bootstrapper().InitializeAsync(CancellationToken.None);

        var commands = Requests().OfType<SetMyCommandsRequest>().Single().Commands;
        commands.Select(c => c.Command).ShouldBe(["start", "menu", "help"]);
        commands.ShouldAllBe(c => !string.IsNullOrWhiteSpace(c.Description));
    }

    [Fact]
    public async Task Invalid_token_stops_startup_before_anything_else_happens()
    {
        _client.SendRequest(Arg.Any<GetMeRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiRequestException("Unauthorized", 401));

        await Should.ThrowAsync<ApiRequestException>(() => Bootstrapper().InitializeAsync(CancellationToken.None));

        Requests().ShouldHaveSingleItem().ShouldBeOfType<GetMeRequest>();
    }

    [Fact]
    public async Task Empty_whitelist_warns_the_operator_how_to_find_their_id()
    {
        await Bootstrapper(allowed: []).InitializeAsync(CancellationToken.None);

        _logger.Collector.GetSnapshot().ShouldContain(r =>
            r.Level == LogLevel.Warning && r.Message.Contains("AllowedUserIds", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Configured_whitelist_raises_no_warning()
    {
        await Bootstrapper(allowed: [42]).InitializeAsync(CancellationToken.None);

        _logger.Collector.GetSnapshot().ShouldNotContain(r => r.Level >= LogLevel.Warning);
    }
}
