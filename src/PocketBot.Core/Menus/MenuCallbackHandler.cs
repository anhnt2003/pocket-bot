using PocketBot.Core.Messaging;
using PocketBot.Core.Texts;
using PocketBot.Core.Updates;

namespace PocketBot.Core.Menus;

/// <summary>Reacts to menu button taps by editing the menu message in place.</summary>
public sealed class MenuCallbackHandler(
    IChatGateway chat,
    MenuCatalog catalog,
    MenuRenderer renderer,
    IEnumerable<IMenuAction> actions)
{
    private readonly Dictionary<string, IMenuAction> _actions = actions.ToDictionary(a => a.NodeId, StringComparer.Ordinal);

    /// <remarks>Always answers the callback exactly once — even on failure — so the button never keeps spinning.</remarks>
    public async Task HandleAsync(IncomingCallback callback, CancellationToken cancellationToken)
    {
        string? toast = null;
        try
        {
            toast = Resolve(callback.Data) is var (action, node)
                ? await ApplyAsync(action, node, callback, cancellationToken)
                : await ResetToRootAsync(callback, cancellationToken);
        }
        finally
        {
            await chat.AnswerCallbackAsync(callback.CallbackId, toast, cancellationToken);
        }
    }

    /// <summary>Valid when the node still exists and the action fits its kind (only actions execute, only pages open).</summary>
    private (CallbackAction Action, MenuNode Node)? Resolve(string data)
    {
        if (!CallbackAction.TryParse(data, out var action) || catalog.Find(action.NodeId) is not { } node)
        {
            return null;
        }

        var expectedKind = action is CallbackAction.Execute ? MenuNodeKind.Action : MenuNodeKind.Submenu;
        return node.Kind == expectedKind ? (action, node) : null;
    }

    private async Task<string?> ApplyAsync(
        CallbackAction action, MenuNode node, IncomingCallback callback, CancellationToken cancellationToken)
    {
        if (action is CallbackAction.Execute)
        {
            return _actions.TryGetValue(node.Id, out var feature)
                ? await feature.ExecuteAsync(callback, cancellationToken)
                : BotTexts.ComingSoon(node.Label);
        }

        var view = action is CallbackAction.Close ? MenuRenderer.RenderCollapsed(node) : renderer.Render(node);
        await chat.EditMenuAsync(callback.ChatId, callback.MessageId, view, cancellationToken);
        return null;
    }

    /// <summary>The button came from an older menu version (or was tampered with): show the current main menu.</summary>
    private async Task<string?> ResetToRootAsync(IncomingCallback callback, CancellationToken cancellationToken)
    {
        await chat.EditMenuAsync(callback.ChatId, callback.MessageId, renderer.Render(catalog.Root), cancellationToken);
        return BotTexts.MenuOutdated;
    }
}
