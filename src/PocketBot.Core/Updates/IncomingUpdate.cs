namespace PocketBot.Core.Updates;

public sealed record BotUser(long Id, string FirstName, string? Username);

/// <summary>A Telegram update translated into the only shapes the bot understands.</summary>
public abstract record IncomingUpdate(long ChatId, BotUser From);

public sealed record IncomingText(long ChatId, BotUser From, int MessageId, string Text) : IncomingUpdate(ChatId, From);

public sealed record IncomingCallback(long ChatId, BotUser From, int MessageId, string CallbackId, string Data)
    : IncomingUpdate(ChatId, From);

/// <summary>A message the bot cannot handle yet (photo, sticker, voice…).</summary>
public sealed record IncomingUnsupported(long ChatId, BotUser From) : IncomingUpdate(ChatId, From);
