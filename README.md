# 🤖 Pocket Bot

Bot Telegram cá nhân viết bằng C# (.NET 10). Bot nhận tin nhắn, phản hồi và thực hiện action khi bạn tương tác. Có menu trực quan, đóng/mở được.

## Tính năng hiện có

- **Kết nối Telegram** bằng long polling, không cần domain hay HTTPS. Khi mất mạng bot tự chờ lâu dần (1s → 30s) rồi hồi phục.
- **Menu inline** được edit tại chỗ (không spam chat), có breadcrumb, `⬅️ Quay lại` và `🏠 Menu chính`.
- **Đóng/mở menu**: `✖️ Đóng menu` thu gọn menu thành `📂 Mở menu`, mở lại sẽ về đúng trang đang xem. Nút `📋 Menu` luôn nằm ở bàn phím.
- **Menu chính**: 📝 Ghi chú · ⏰ Nhắc việc · 💰 Thu chi · ⚙️ Cài đặt · ❓ Trợ giúp. Các chức năng bên trong hiện báo "🚧 đang phát triển".
- **Phản hồi tin nhắn**: `/start`, `/menu`, `/help`; text thường được xác nhận đã nhận; lệnh lạ và tin nhắn không phải chữ có thông báo riêng.
- **Bot cá nhân**: chỉ user ID trong whitelist mới dùng được.

## Cài đặt & chạy

Yêu cầu: [.NET SDK 10](https://dotnet.microsoft.com/download).

1. **Tạo bot**: nhắn [@BotFather](https://t.me/BotFather) lệnh `/newbot`, rồi copy token.
2. **Lưu token** (token không nằm trong repo):
   ```bash
   dotnet user-secrets set "Telegram:BotToken" "<token>" --project src/PocketBot.Host
   ```
3. **Chạy bot**:
   ```bash
   dotnet run --project src/PocketBot.Host
   ```
   Log hiện `Connected to Telegram as @<tên bot>` là đã kết nối thành công.
4. **Thêm bạn vào whitelist**: nhắn bot bất kỳ tin gì. Bot trả lời ⛔ kèm **User ID** của bạn. Thêm ID đó vào config rồi chạy lại:
   ```bash
   dotnet user-secrets set "Telegram:AllowedUserIds:0" "<user id>" --project src/PocketBot.Host
   ```
5. Gõ `/start` để bắt đầu.

| Cấu hình | Ý nghĩa | Mặc định |
|----------|---------|----------|
| `Telegram:BotToken` | Token từ @BotFather (bắt buộc) | — |
| `Telegram:AllowedUserIds` | Danh sách user ID được dùng bot | rỗng (từ chối tất cả) |
| `Telegram:DropPendingUpdates` | Bỏ qua tin nhắn gửi đến lúc bot đang tắt | `false` |

Ngoài user-secrets, bạn có thể đặt cấu hình qua biến môi trường, ví dụ `Telegram__BotToken=...`. Chỉ chạy **một** instance cùng lúc, vì Telegram không cho hai tiến trình cùng poll.

## Phát triển

```bash
dotnet build PocketBot.slnx
dotnet test PocketBot.slnx
```

Dự án làm theo **TDD**. `dotnet test` sẽ **fail nếu coverage dưới 90%** (line hoặc branch). Hiện cả hai project đều đạt 100%. File báo cáo cobertura nằm trong `tests/*/bin/Debug/net10.0/TestResults/`. Muốn xem dạng HTML thì chạy:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"tests/**/TestResults/*.cobertura*.xml" -targetdir:coverage
```

### Cấu trúc

```
src/
  PocketBot.Core/     # logic bot: router, menu, lệnh, text — không phụ thuộc Telegram.Bot
  PocketBot.Host/     # Worker Service: adapter Telegram, polling, cấu hình, DI
tests/
  PocketBot.Core.Tests/
  PocketBot.Host.Tests/
docs/agentic-workflow/tech-designs/   # thiết kế kỹ thuật
```

### Thêm chức năng mới

1. Khai báo node trong `src/PocketBot.Core/Menus/DefaultMenu.cs`.
2. Viết class implement `IMenuAction` có `NodeId` trùng với node đó, rồi đăng ký vào DI. Bấm nút là action chạy.
3. Nếu cần lệnh `/xyz` mới, implement `ICommandHandler`.

Thiết kế chi tiết: [docs/agentic-workflow/tech-designs/2026-10-09-telegram-foundation.md](docs/agentic-workflow/tech-designs/2026-10-09-telegram-foundation.md).
