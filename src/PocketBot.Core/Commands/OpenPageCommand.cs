using PocketBot.Core.Menus;
using PocketBot.Core.Messaging;
using PocketBot.Core.Updates;

namespace PocketBot.Core.Commands;

/// <summary>A command that simply opens a menu page as a new message (/menu → main, /help → help).</summary>
internal sealed class OpenPageCommand(string command, string pageId, IChatGateway chat, MenuCatalog catalog, MenuRenderer renderer)
    : ICommandHandler
{
    public string Command => command;

    public Task HandleAsync(IncomingText message, BotCommand botCommand, CancellationToken cancellationToken) =>
        chat.SendMenuAsync(message.ChatId, renderer.Render(catalog.Find(pageId)!), cancellationToken);
}
