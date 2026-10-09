using PocketBot.Core.Menus;

namespace PocketBot.Core.Tests.Menus;

public class CallbackActionTests
{
    public static TheoryData<CallbackAction, string> EncodedActions => new()
    {
        { new CallbackAction.Navigate("finance"), "nav:finance" },
        { new CallbackAction.Close("finance"), "close:finance" },
        { new CallbackAction.Open("settings"), "open:settings" },
        { new CallbackAction.Execute("finance.income"), "act:finance.income" },
    };

    [Theory]
    [MemberData(nameof(EncodedActions))]
    public void Encode_produces_prefixed_callback_data(CallbackAction action, string expected)
    {
        action.Encode().ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(EncodedActions))]
    public void TryParse_restores_the_encoded_action(CallbackAction expected, string data)
    {
        CallbackAction.TryParse(data, out var parsed).ShouldBeTrue();
        parsed.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("finance")]
    [InlineData("xyz:finance")]
    [InlineData("nav:")]
    [InlineData("nav:   ")]
    public void TryParse_rejects_malformed_or_unknown_data(string? data)
    {
        CallbackAction.TryParse(data, out var parsed).ShouldBeFalse();
        parsed.ShouldBeNull();
    }
}
