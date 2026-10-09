using Microsoft.Extensions.Options;
using PocketBot.Core.Commands;
using PocketBot.Core.Texts;
using PocketBot.Host.Configuration;
using Telegram.Bot;
using TelegramCommand = Telegram.Bot.Types.BotCommand;
using User = Telegram.Bot.Types.User;

namespace PocketBot.Host.Telegram;

/// <summary>One-time startup handshake with Telegram before polling begins.</summary>
internal sealed partial class BotBootstrapper(
    ITelegramBotClient client,
    IOptions<TelegramOptions> options,
    ILogger<BotBootstrapper> logger)
{
    private static readonly TelegramCommand[] Commands =
    [
        new() { Command = CommandNames.Start, Description = BotTexts.StartCommandDescription },
        new() { Command = CommandNames.Menu, Description = BotTexts.MenuCommandDescription },
        new() { Command = CommandNames.Help, Description = BotTexts.HelpCommandDescription },
    ];

    /// <summary>Verifies the token, clears any webhook (polling and webhooks are exclusive) and publishes the "/" command list.</summary>
    /// <exception cref="global::Telegram.Bot.Exceptions.ApiRequestException">The token is rejected; startup must stop.</exception>
    public async Task<User> InitializeAsync(CancellationToken cancellationToken)
    {
        var me = await client.GetMe(cancellationToken);
        LogConnected(me.Username);

        await client.DeleteWebhook(options.Value.DropPendingUpdates, cancellationToken);
        await client.SetMyCommands(Commands, cancellationToken: cancellationToken);

        if (options.Value.AllowedUserIds.Count == 0)
        {
            LogEmptyWhitelist();
        }

        return me;
    }

    [LoggerMessage(LogLevel.Information, "Connected to Telegram as @{Username}")]
    private partial void LogConnected(string? username);

    [LoggerMessage(LogLevel.Warning,
        "Telegram:AllowedUserIds is empty, so everyone is refused. Message the bot to see your user id, then add it to AllowedUserIds")]
    private partial void LogEmptyWhitelist();
}
