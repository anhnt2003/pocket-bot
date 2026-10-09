using Microsoft.Extensions.DependencyInjection;
using PocketBot.Core.Access;
using PocketBot.Core.Menus;
using PocketBot.Core.Messaging;
using PocketBot.Core.Routing;
using PocketBot.Core;
using PocketBot.Core.Tests.Fakes;
using PocketBot.Core.Updates;

namespace PocketBot.Core.Tests.Routing;

/// <summary>The real Core wiring with only the chat platform faked.</summary>
public sealed class BotHarness
{
    public const long ChatId = 1001;
    public const int MenuMessageId = 77;
    public const string CallbackId = "cb-1";

    public static readonly BotUser Me = new(42, "Anh", "anh");
    public static readonly BotUser Stranger = new(666, "Người lạ", null);

    private readonly UpdateRouter _router;

    public BotHarness(params IMenuAction[] actions)
    {
        var services = new ServiceCollection()
            .AddPocketBotCore()
            .AddSingleton(new AccessPolicy([Me.Id]))
            .AddSingleton<IChatGateway>(Chat);
        foreach (var action in actions)
        {
            services.AddSingleton(action);
        }

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        _router = provider.GetRequiredService<UpdateRouter>();
        Menus = provider.GetRequiredService<MenuRenderer>();
        Catalog = provider.GetRequiredService<MenuCatalog>();
    }

    public FakeChatGateway Chat { get; } = new();

    public MenuRenderer Menus { get; }

    public MenuCatalog Catalog { get; }

    public MenuView Page(string id) => Menus.Render(Catalog.Find(id)!);

    public Task SendText(string text, BotUser? from = null) =>
        _router.RouteAsync(new IncomingText(ChatId, from ?? Me, MessageId: 5, text), CancellationToken.None);

    public Task SendUnsupported(BotUser? from = null) =>
        _router.RouteAsync(new IncomingUnsupported(ChatId, from ?? Me), CancellationToken.None);

    public Task Tap(string callbackData, BotUser? from = null) =>
        _router.RouteAsync(
            new IncomingCallback(ChatId, from ?? Me, MenuMessageId, CallbackId, callbackData), CancellationToken.None);
}
