using PocketBot.Core.Access;
using PocketBot.Core.Commands;
using PocketBot.Core.Menus;
using PocketBot.Core.Messaging;
using PocketBot.Core.Texts;
using PocketBot.Core.Updates;

namespace PocketBot.Core.Routing;

/// <summary>Single entry point: decides who handles an incoming update.</summary>
public sealed class UpdateRouter(
    AccessPolicy access,
    IEnumerable<ICommandHandler> commandHandlers,
    MenuCallbackHandler menuCallbacks,
    IChatGateway chat)
{
    private readonly Dictionary<string, ICommandHandler> _commands =
        commandHandlers.ToDictionary(h => h.Command, StringComparer.Ordinal);

    public Task RouteAsync(IncomingUpdate update, CancellationToken cancellationToken)
    {
        if (!access.IsAllowed(update.From.Id))
        {
            return update is IncomingCallback callback
                ? chat.AnswerCallbackAsync(callback.CallbackId, BotTexts.AccessDeniedToast, cancellationToken)
                : Reply(update, BotTexts.AccessDenied(update.From.Id), cancellationToken);
        }

        return update switch
        {
            IncomingText text => HandleTextAsync(text, cancellationToken),
            IncomingCallback callback => menuCallbacks.HandleAsync(callback, cancellationToken),
            _ => Reply(update, BotTexts.TextOnly, cancellationToken),
        };
    }

    private Task HandleTextAsync(IncomingText message, CancellationToken cancellationToken)
    {
        if (!CommandParser.TryParse(message.Text, out var command))
        {
            return Reply(message, BotTexts.Echo(message.Text), cancellationToken);
        }

        return _commands.TryGetValue(command.Name, out var handler)
            ? handler.HandleAsync(message, command, cancellationToken)
            : Reply(message, BotTexts.UnknownCommand(command.Name), cancellationToken);
    }

    private Task Reply(IncomingUpdate to, string html, CancellationToken cancellationToken) =>
        chat.SendTextAsync(to.ChatId, html, withMenuShortcut: false, cancellationToken);
}
