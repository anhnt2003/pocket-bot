using Microsoft.Extensions.Options;
using PocketBot.Host.Configuration;

namespace PocketBot.Host.Tests.Configuration;

public class TelegramOptionsValidatorTests
{
    private const string ValidToken = "123456789:AAHdqTcvCH1vGWJxfSeofSAs0K5PALDsaw";

    private static ValidateOptionsResult Validate(string token) =>
        new TelegramOptionsValidator().Validate(null, new TelegramOptions { BotToken = token });

    [Fact]
    public void Well_formed_token_is_accepted()
    {
        Validate(ValidToken).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_token_explains_how_to_set_it(string token)
    {
        var result = Validate(token);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Telegram:BotToken");
        result.FailureMessage.ShouldContain("user-secrets");
    }

    [Theory]
    [InlineData("not-a-token")]
    [InlineData("123456789:short")]
    [InlineData("abc:AAHdqTcvCH1vGWJxfSeofSAs0K5PALDsaw")]
    public void Malformed_token_is_rejected_without_echoing_it(string token)
    {
        var result = Validate(token);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Telegram:BotToken");
        result.FailureMessage.ShouldNotContain(token);
    }
}
