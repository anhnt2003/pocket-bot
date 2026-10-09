# 🤖 Pocket Bot

A personal Telegram bot written in C# (.NET 10). It receives messages, replies, and runs actions as you interact with it, through a visual menu you can collapse and reopen.

## Features

- **Telegram connection** via long polling, so no domain or HTTPS is needed. When Telegram is unreachable, the bot waits progressively longer between retries (1s → 30s) and recovers on its own.
- **Inline menu** edited in place (no chat spam), with a breadcrumb, `⬅️ Quay lại` (Back) and `🏠 Menu chính` (Main menu).
- **Collapsible menu**: `✖️ Đóng menu` (Close) folds the menu into `📂 Mở menu` (Open), which reopens it on the page you were viewing. The `📋 Menu` button always stays on the keyboard.
- **Main menu**: 📝 Ghi chú (Notes) · ⏰ Nhắc việc (Reminders) · 💰 Thu chi (Finance) · ⚙️ Cài đặt (Settings) · ❓ Trợ giúp (Help). The features inside currently reply "🚧 đang phát triển" (in development).
- **Message handling**: `/start`, `/menu`, `/help`; plain text is acknowledged; unknown commands and non-text messages get their own replies.
- **Personal bot**: only user IDs on the whitelist can use it.

The bot's UI copy is Vietnamese; labels above are quoted as they appear in Telegram.

## Setup & run

Requires [.NET SDK 10](https://dotnet.microsoft.com/download).

1. **Create a bot**: send `/newbot` to [@BotFather](https://t.me/BotFather), then copy the token.
2. **Store the token** (it never goes in the repo):
   ```bash
   dotnet user-secrets set "Telegram:BotToken" "<token>" --project src/PocketBot.Host
   ```
3. **Run the bot**:
   ```bash
   dotnet run --project src/PocketBot.Host
   ```
   The log line `Connected to Telegram as @<bot name>` means it is connected.
4. **Whitelist yourself**: send the bot any message. It replies ⛔ with your **User ID**. Add that ID to the config and run again:
   ```bash
   dotnet user-secrets set "Telegram:AllowedUserIds:0" "<user id>" --project src/PocketBot.Host
   ```
5. Send `/start` to begin.

| Setting | Meaning | Default |
|---------|---------|---------|
| `Telegram:BotToken` | Token from @BotFather (required) | — |
| `Telegram:AllowedUserIds` | User IDs allowed to use the bot | empty (everyone is refused) |
| `Telegram:DropPendingUpdates` | Skip messages sent while the bot was offline | `false` |

Besides user-secrets, you can set configuration through environment variables, e.g. `Telegram__BotToken=...`. Run only **one** instance at a time, because Telegram doesn't allow two processes to poll the same bot.

## Development

```bash
dotnet build PocketBot.slnx
dotnet test PocketBot.slnx
```

The project follows **TDD**. `dotnet test` **fails if coverage drops below 90%** (line or branch). Both projects are currently at 100%. Cobertura reports are written to `tests/*/bin/Debug/net10.0/TestResults/`. For an HTML view, run:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"tests/**/TestResults/*.cobertura*.xml" -targetdir:coverage
```

### Layout

```
src/
  PocketBot.Core/     # bot logic: router, menu, commands, texts — no Telegram.Bot dependency
  PocketBot.Host/     # Worker Service: Telegram adapter, polling, configuration, DI
tests/
  PocketBot.Core.Tests/
  PocketBot.Host.Tests/
```

### Adding a feature

1. Declare the node in `src/PocketBot.Core/Menus/DefaultMenu.cs`.
2. Implement `IMenuAction` with a `NodeId` matching that node, then register it in DI. Tapping the button runs the action.
3. For a new `/xyz` command, implement `ICommandHandler`.
