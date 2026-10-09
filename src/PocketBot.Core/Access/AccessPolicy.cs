namespace PocketBot.Core.Access;

/// <summary>Personal bot: only whitelisted Telegram user ids may use it.</summary>
public sealed class AccessPolicy(IEnumerable<long> allowedUserIds)
{
    private readonly HashSet<long> _allowed = [.. allowedUserIds];

    public bool IsAllowed(long userId) => _allowed.Contains(userId);
}
