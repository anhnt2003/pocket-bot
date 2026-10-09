using PocketBot.Core.Updates;

namespace PocketBot.Core.Menus;

/// <summary>Behaviour behind a menu action node. Register one per feature; unregistered actions show "coming soon".</summary>
public interface IMenuAction
{
    string NodeId { get; }

    /// <returns>Optional toast shown to the user after the action runs.</returns>
    Task<string?> ExecuteAsync(IncomingCallback callback, CancellationToken cancellationToken);
}
