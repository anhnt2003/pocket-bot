using PocketBot.Core.Texts;

namespace PocketBot.Core.Tests.Texts;

public class TelegramHtmlTests
{
    [Theory]
    [InlineData("<b>hi</b>", "&lt;b&gt;hi&lt;/b&gt;")]
    [InlineData("Tom & Jerry", "Tom &amp; Jerry")]
    [InlineData("&lt;", "&amp;lt;")]
    public void Escape_neutralises_html_markup(string input, string expected)
    {
        TelegramHtml.Escape(input).ShouldBe(expected);
    }

    [Fact]
    public void Escape_keeps_vietnamese_text_and_emoji_readable()
    {
        TelegramHtml.Escape("🏠 Menu chính › Thu chi").ShouldBe("🏠 Menu chính › Thu chi");
    }
}
