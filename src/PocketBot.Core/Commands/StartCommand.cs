using PocketBot.Core.Menus;
using PocketBot.Core.Messaging;
using PocketBot.Core.Texts;
using PocketBot.Core.Updates;

namespace PocketBot.Core.Commands;

internal sealed class StartCommand(IChatGateway chat, MenuCatalog catalog, MenuRenderer renderer) : ICommandHandler
{
    public string Command => CommandNames.Start;

    public async Task HandleAsync(IncomingText message, BotCommand command, CancellationToken cancellationToken)
    {
        await chat.SendTextAsync(message.ChatId, BotTexts.Welcome(message.From.FirstName), withMenuShortcut: true, cancellationToken);
        await chat.SendMenuAsync(message.ChatId, renderer.Render(catalog.Root), cancellationToken);
    }
}
