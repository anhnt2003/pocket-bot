using PocketBot.Core.Commands;
using PocketBot.Core.Texts;

namespace PocketBot.Core.Tests.Commands;

public class CommandParserTests
{
    [Theory]
    [InlineData("/start", "start", "")]
    [InlineData("  /menu  ", "menu", "")]
    [InlineData("/START@PocketBot  hello world", "start", "hello world")]
    [InlineData("/help@pocket_bot", "help", "")]
    public void TryParse_reads_command_name_and_arguments(string text, string name, string arguments)
    {
        CommandParser.TryParse(text, out var command).ShouldBeTrue();
        command.ShouldBe(new BotCommand(name, arguments));
    }

    [Fact]
    public void TryParse_treats_the_keyboard_menu_shortcut_as_the_menu_command()
    {
        CommandParser.TryParse(BotTexts.MenuShortcutButton, out var command).ShouldBeTrue();
        command.ShouldBe(new BotCommand("menu", ""));
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("/")]
    [InlineData("/ start")]
    [InlineData("what is /start")]
    public void TryParse_rejects_plain_text(string text)
    {
        CommandParser.TryParse(text, out var command).ShouldBeFalse();
        command.ShouldBeNull();
    }
}
