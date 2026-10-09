using PocketBot.Core.Updates;
using PocketBot.Host.Telegram;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PocketBot.Host.Tests.Telegram;

public class TelegramUpdateMapperTests
{
    private static readonly User Anh = new() { Id = 42, FirstName = "Anh", Username = "anh" };
    private static readonly Chat PrivateChat = new() { Id = 1001, Type = ChatType.Private };
    private static readonly BotUser ExpectedAnh = new(42, "Anh", "anh");

    [Fact]
    public void Text_message_becomes_incoming_text()
    {
        var update = new Update { Message = new Message { Id = 5, From = Anh, Chat = PrivateChat, Text = "/start" } };

        TelegramUpdateMapper.Map(update).ShouldBe(new IncomingText(1001, ExpectedAnh, 5, "/start"));
    }

    [Fact]
    public void Message_without_text_becomes_unsupported()
    {
        var update = new Update { Message = new Message { Id = 6, From = Anh, Chat = PrivateChat, Sticker = new Sticker() } };

        TelegramUpdateMapper.Map(update).ShouldBe(new IncomingUnsupported(1001, ExpectedAnh));
    }

    [Fact]
    public void Button_tap_becomes_incoming_callback_on_the_menu_message()
    {
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cb-9",
                From = Anh,
                Data = "nav:finance",
                Message = new Message { Id = 77, Chat = PrivateChat },
            },
        };

        TelegramUpdateMapper.Map(update).ShouldBe(new IncomingCallback(1001, ExpectedAnh, 77, "cb-9", "nav:finance"));
    }

    [Fact]
    public void Button_tap_without_payload_is_treated_as_an_empty_callback()
    {
        var update = new Update
        {
            CallbackQuery = new CallbackQuery { Id = "cb-9", From = Anh, Message = new Message { Id = 77, Chat = PrivateChat } },
        };

        TelegramUpdateMapper.Map(update).ShouldBe(new IncomingCallback(1001, ExpectedAnh, 77, "cb-9", ""));
    }

    public static TheoryData<Update> IgnoredUpdates => new()
    {
        // Message posted on behalf of a channel/group: no human sender
        new Update { Message = new Message { Id = 1, Chat = PrivateChat, Text = "hi" } },
        // Tap on an inline-mode message: there is no chat message to edit
        new Update { CallbackQuery = new CallbackQuery { Id = "cb", From = Anh, Data = "nav:main", InlineMessageId = "x" } },
        // Update kinds the bot does not subscribe to
        new Update { EditedMessage = new Message { Id = 1, From = Anh, Chat = PrivateChat, Text = "edited" } },
    };

    [Theory]
    [MemberData(nameof(IgnoredUpdates))]
    public void Updates_the_bot_cannot_act_on_are_ignored(Update update)
    {
        TelegramUpdateMapper.Map(update).ShouldBeNull();
    }
}
