using System.Diagnostics.CodeAnalysis;
using PocketBot.Core.Texts;

namespace PocketBot.Core.Commands;

public sealed record BotCommand(string Name, string Arguments);

/// <summary>Parses "/name[@BotName] [arguments]" — plus the keyboard's menu shortcut, which acts as /menu.</summary>
public static class CommandParser
{
    public static bool TryParse(string text, [NotNullWhen(true)] out BotCommand? command)
    {
        command = null;
        var trimmed = text.Trim();
        if (trimmed == BotTexts.MenuShortcutButton)
        {
            command = new BotCommand(CommandNames.Menu, string.Empty);
            return true;
        }

        if (!trimmed.StartsWith('/'))
        {
            return false;
        }

        var parts = trimmed[1..].Split(' ', 2, StringSplitOptions.TrimEntries);
        var name = parts[0].Split('@')[0];
        if (name.Length == 0)
        {
            return false;
        }

        command = new BotCommand(name.ToLowerInvariant(), parts.Length > 1 ? parts[1] : string.Empty);
        return true;
    }
}
