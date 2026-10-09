using Microsoft.Extensions.Options;
using PocketBot.Core;
using PocketBot.Core.Access;
using PocketBot.Core.Messaging;
using PocketBot.Host.Configuration;
using PocketBot.Host.Telegram;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace PocketBot.Host;

public static class HostServiceCollectionExtensions
{
    /// <summary>Wires Core behind the Telegram adapter and starts long polling as a hosted service.</summary>
    public static IServiceCollection AddPocketBot(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TelegramOptions>()
            .Bind(configuration.GetSection(TelegramOptions.Section))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<TelegramOptions>, TelegramOptionsValidator>();

        services.AddPocketBotCore();
        services.AddSingleton(sp => new AccessPolicy(Telegram(sp).AllowedUserIds));

        services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(Telegram(sp).BotToken));
        services.AddSingleton<IChatGateway, TelegramChatGateway>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<PollingBackoff>();
        services.AddSingleton<IUpdateHandler, BotUpdateHandler>();
        services.AddSingleton<BotBootstrapper>();
        services.AddHostedService<BotPollingService>();
        return services;
    }

    private static TelegramOptions Telegram(IServiceProvider sp) => sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
}
