using System.Diagnostics.CodeAnalysis;

namespace PocketBot.Core.Menus;

/// <summary>
/// What a menu button does when tapped. Encoded into Telegram's callback_data as "&lt;prefix&gt;:&lt;nodeId&gt;",
/// so the menu stays stateless: the button itself carries where to go next.
/// </summary>
public abstract record CallbackAction(string NodeId)
{
    public sealed record Navigate(string NodeId) : CallbackAction(NodeId);

    public sealed record Close(string NodeId) : CallbackAction(NodeId);

    public sealed record Open(string NodeId) : CallbackAction(NodeId);

    public sealed record Execute(string NodeId) : CallbackAction(NodeId);

    public string Encode() => $"{Prefix}:{NodeId}";

    public static bool TryParse(string? data, [NotNullWhen(true)] out CallbackAction? action)
    {
        action = null;
        var separator = data?.IndexOf(':') ?? -1;
        if (separator < 0)
        {
            return false;
        }

        var nodeId = data![(separator + 1)..];
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return false;
        }

        action = data[..separator] switch
        {
            "nav" => new Navigate(nodeId),
            "close" => new Close(nodeId),
            "open" => new Open(nodeId),
            "act" => new Execute(nodeId),
            _ => null,
        };
        return action is not null;
    }

    private string Prefix => this switch
    {
        Navigate => "nav",
        Close => "close",
        Open => "open",
        _ => "act",
    };
}
