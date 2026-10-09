using PocketBot.Core.Menus;

namespace PocketBot.Core.Messaging;

/// <summary>Outbound port to the chat platform. Texts are Telegram HTML; toasts are plain text.</summary>
public interface IChatGateway
{
    /// <param name="withMenuShortcut">Attach the persistent "📋 Menu" reply-keyboard button.</param>
    Task SendTextAsync(long chatId, string html, bool withMenuShortcut, CancellationToken cancellationToken);

    Task SendMenuAsync(long chatId, MenuView view, CancellationToken cancellationToken);

    /// <summary>Replaces an existing menu message in place.</summary>
    Task EditMenuAsync(long chatId, int messageId, MenuView view, CancellationToken cancellationToken);

    /// <summary>Stops the button's loading spinner, optionally showing a short toast.</summary>
    Task AnswerCallbackAsync(string callbackId, string? toast, CancellationToken cancellationToken);
}
