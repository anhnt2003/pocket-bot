using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace PocketBot.Host.Configuration;

/// <summary>Fails fast at startup with an actionable message — and never echoes the secret back.</summary>
internal sealed partial class TelegramOptionsValidator : IValidateOptions<TelegramOptions>
{
    public ValidateOptionsResult Validate(string? name, TelegramOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BotToken))
        {
            return ValidateOptionsResult.Fail(
                "Telegram:BotToken is missing. Set it with: " +
                "dotnet user-secrets set \"Telegram:BotToken\" \"<token from @BotFather>\" --project src/PocketBot.Host");
        }

        return TokenFormat().IsMatch(options.BotToken)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Telegram:BotToken is malformed (expected \"<bot id>:<secret>\" as issued by @BotFather).");
    }

    [GeneratedRegex(@"^\d+:[A-Za-z0-9_-]{30,}$")]
    private static partial Regex TokenFormat();
}
