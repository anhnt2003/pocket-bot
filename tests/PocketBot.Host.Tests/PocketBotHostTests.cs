using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PocketBot.Core.Messaging;
using PocketBot.Host.Configuration;
using PocketBot.Host.Telegram;
using PocketBot.Host.Tests.Telegram;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PocketBot.Host.Tests;

/// <summary>The fully wired bot, with only the Telegram HTTP client replaced.</summary>
public class PocketBotHostTests
{
    private const string ValidToken = "123456789:AAHdqTcvCH1vGWJxfSeofSAs0K5PALDsaw";
    private const long Me = 42;

    private readonly ITelegramBotClient _telegram = Substitute.For<ITelegramBotClient>();

    public PocketBotHostTests()
    {
        _telegram.SendRequest(Arg.Any<GetMeRequest>(), Arg.Any<CancellationToken>())
            .Returns(new User { Id = 7, IsBot = true, FirstName = "Pocket", Username = "pocket_bot" });
    }

    private static IConfiguration Config(string? token = ValidToken) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Telegram:BotToken"] = token,
            ["Telegram:AllowedUserIds:0"] = Me.ToString(System.Globalization.CultureInfo.InvariantCulture),
        }).Build();

    private ServiceProvider Build(IConfiguration config, bool fakeTelegram = true)
    {
        var services = new ServiceCollection().AddLogging().AddPocketBot(config);
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
        provider.GetRequiredService<IUpdateHandler>().ShouldBeOfType<BotUpdateHandler>();
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

    private static Task WhenStopped(CancellationToken token)
    {
        var stopped = new TaskCompletionSource();
        token.Register(stopped.SetResult);
        return stopped.Task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
    }
}
