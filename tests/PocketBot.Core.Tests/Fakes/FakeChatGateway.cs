using PocketBot.Core.Menus;
using PocketBot.Core.Messaging;

namespace PocketBot.Core.Tests.Fakes;

public abstract record ChatCall;

public sealed record SentText(long ChatId, string Html, bool WithMenuShortcut) : ChatCall;

public sealed record SentMenu(long ChatId, MenuView View) : ChatCall;

public sealed record EditedMenu(long ChatId, int MessageId, MenuView View) : ChatCall;

public sealed record AnsweredCallback(string CallbackId, string? Toast) : ChatCall;

/// <summary>Records everything the bot says, in order.</summary>
public sealed class FakeChatGateway : IChatGateway
{
    public List<ChatCall> Calls { get; } = [];

    public Exception? FailEditsWith { get; set; }

    public Task SendTextAsync(long chatId, string html, bool withMenuShortcut, CancellationToken cancellationToken)
    {
        Calls.Add(new SentText(chatId, html, withMenuShortcut));
        return Task.CompletedTask;
    }

    public Task SendMenuAsync(long chatId, MenuView view, CancellationToken cancellationToken)
    {
        Calls.Add(new SentMenu(chatId, view));
        return Task.CompletedTask;
    }

    public Task EditMenuAsync(long chatId, int messageId, MenuView view, CancellationToken cancellationToken)
    {
        if (FailEditsWith is not null)
        {
            throw FailEditsWith;
        }

        Calls.Add(new EditedMenu(chatId, messageId, view));
        return Task.CompletedTask;
    }

    public Task AnswerCallbackAsync(string callbackId, string? toast, CancellationToken cancellationToken)
    {
        Calls.Add(new AnsweredCallback(callbackId, toast));
        return Task.CompletedTask;
    }
}
