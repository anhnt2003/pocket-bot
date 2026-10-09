using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests.Abstractions;

namespace PocketBot.Host.Tests.Telegram;

internal static class TelegramClientTestExtensions
{
    /// <summary>Bot API requests sent through a substitute client, in order (ignores property reads).</summary>
    public static List<IRequest> SentRequests(this ITelegramBotClient client) =>
        client.ReceivedCalls()
            .Select(call => call.GetArguments().FirstOrDefault())
            .OfType<IRequest>()
            .ToList();

    public static List<T> Sent<T>(this ITelegramBotClient client) => client.SentRequests().OfType<T>().ToList();
}
