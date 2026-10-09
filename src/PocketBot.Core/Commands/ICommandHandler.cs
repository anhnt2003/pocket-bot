using PocketBot.Core.Updates;

namespace PocketBot.Core.Commands;

/// <summary>Handles one slash command. Register implementations in DI; the router picks by <see cref="Command"/>.</summary>
public interface ICommandHandler
{
    /// <summary>Lower-case command name without the slash, e.g. "start".</summary>
    string Command { get; }

    Task HandleAsync(IncomingText message, BotCommand command, CancellationToken cancellationToken);
}
