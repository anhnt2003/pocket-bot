namespace PocketBot.Host.Configuration;

public sealed class TelegramOptions
{
    public const string Section = "Telegram";

    /// <summary>From @BotFather. Keep it in user-secrets or the environment, never in appsettings.json.</summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>Telegram user ids allowed to use the bot. Message the bot to see yours.</summary>
    public IReadOnlyList<long> AllowedUserIds { get; set; } = [];

    /// <summary>Skip messages that arrived while the bot was offline.</summary>
    public bool DropPendingUpdates { get; set; }
}
