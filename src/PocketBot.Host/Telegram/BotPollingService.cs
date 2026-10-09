using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace PocketBot.Host.Telegram;

/// <summary>Connects to Telegram and long-polls for updates until the host stops.</summary>
internal sealed partial class BotPollingService(
    BotBootstrapper bootstrapper,
    ITelegramBotClient client,
    IUpdateHandler updateHandler,
    ILogger<BotPollingService> logger) : BackgroundService
{
    private static readonly ReceiverOptions Receiver = new()
    {
        AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await bootstrapper.InitializeAsync(stoppingToken);
        }
        catch (ApiRequestException ex)
        {
            LogStartupRejected(ex, ex.ErrorCode);
            throw;
        }

        await client.ReceiveAsync(updateHandler, Receiver, stoppingToken);
    }

    [LoggerMessage(LogLevel.Critical,
        "Telegram rejected the bot at startup (HTTP {ErrorCode}). Check Telegram:BotToken from @BotFather")]
    private partial void LogStartupRejected(Exception exception, int errorCode);
}
