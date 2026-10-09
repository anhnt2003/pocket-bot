# Pocket Bot

Personal Telegram bot in C# (.NET 10).

## Commands

```bash
dotnet build PocketBot.slnx
dotnet test PocketBot.slnx                          # also enforces the coverage gate
dotnet test --project tests/PocketBot.Core.Tests    # one project
dotnet run --project src/PocketBot.Host             # needs Telegram:BotToken (see README)
```

- Tests run on Microsoft.Testing.Platform (`global.json` → `test.runner`), so use `dotnet test --project <path>`, not a bare path argument.
- Coverage gate lives in `tests/Directory.Build.props`: `X.Tests` measures assembly `X` only; `dotnet test` fails below 90% line or branch. Currently 100%. Don't lower it — add tests.
- Running the host inside a sandbox can hang silently on config file watchers; add `DOTNET_hostBuilder__reloadConfigOnChange=false`.

## Architecture rules

- `PocketBot.Core` must never reference `Telegram.Bot`. Telegram types stop at `src/PocketBot.Host/Telegram/` (mapper → `IncomingUpdate`, `IChatGateway` → Bot API).
- The menu is stateless: navigation state lives in callback data (`nav:|close:|open:|act:<nodeId>`, ≤ 64 bytes). Don't add server-side menu state.
- All user-facing copy (Vietnamese) goes in `src/PocketBot.Core/Texts/BotTexts.cs`. Texts are Telegram HTML; toasts are plain text.
- Any user-supplied text placed into HTML goes through `TelegramHtml.Escape` (not `WebUtility.HtmlEncode`, which mangles Vietnamese/emoji).
- `TreatWarningsAsErrors` + `AnalysisLevel=latest-recommended` are on; use `[LoggerMessage]` for logging.

## Adding a feature

1. Menu entry: add/adjust nodes in `DefaultMenu.Build()` (ids are a contract with callbacks).
2. Behaviour: implement `IMenuAction` with a matching `NodeId` and register it in DI; unregistered actions show "đang phát triển".
3. New slash command: implement `ICommandHandler` and register it; add it to `BotBootstrapper.Commands` so it appears in Telegram's "/" list.

## Testing (TDD is mandatory)

- Red → green → refactor, one behaviour at a time; watch each new test fail before writing code.
- Core tests go through `UpdateRouter` via `BotHarness` (real DI wiring, only `FakeChatGateway` faked).
- Host tests substitute only `ITelegramBotClient` and assert on the Bot API requests sent (`client.Sent<T>()`); use `FakeTimeProvider` / `FakeLogger` instead of real time/log sinks.
- Test names are sentences: `Closing_collapses_the_menu_to_a_single_reopen_button`.
