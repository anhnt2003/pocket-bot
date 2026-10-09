using PocketBot.Core.Menus;
using PocketBot.Core.Texts;

namespace PocketBot.Core.Tests.Menus;

public class MenuRendererTests
{
    private static readonly MenuCatalog Catalog = new(
        MenuNode.Submenu("main", "🏠 Menu chính", "Chọn chức năng",
            MenuNode.Submenu("notes", "📝 Ghi chú", "Ghi chú nhanh",
                MenuNode.Action("notes.add", "➕ Thêm")),
            MenuNode.Submenu("finance", "💰 Thu chi", "Ghi thu chi",
                MenuNode.Action("finance.income", "➕ Khoản thu"),
                MenuNode.Action("finance.expense", "➖ Khoản chi"),
                MenuNode.Submenu("finance.report", "📊 Báo cáo", "Xem báo cáo",
                    MenuNode.Action("finance.report.month", "🗓 Theo tháng"))),
            MenuNode.Submenu("help", "❓ Trợ giúp", "Hướng dẫn")));

    private readonly MenuRenderer _renderer = new(Catalog);

    private static string[][] Layout(MenuView view) =>
        view.Rows.Select(row => row.Select(b => $"{b.Label}|{b.CallbackData}").ToArray()).ToArray();

    [Fact]
    public void Root_page_shows_bold_title_and_body_without_breadcrumb()
    {
        var view = _renderer.Render(Catalog.Root);

        view.Html.ShouldBe("<b>🏠 Menu chính</b>\n\nChọn chức năng");
    }

    [Fact]
    public void Root_page_lays_children_out_in_two_columns_followed_by_close()
    {
        var view = _renderer.Render(Catalog.Root);

        Layout(view).ShouldBe(
        [
            ["📝 Ghi chú|nav:notes", "💰 Thu chi|nav:finance"],
            ["❓ Trợ giúp|nav:help"],
            [$"{BotTexts.CloseMenuButton}|close:main"],
        ]);
    }

    [Fact]
    public void Submenu_page_shows_breadcrumb_between_title_and_body()
    {
        var view = _renderer.Render(Catalog.Find("finance")!);

        view.Html.ShouldBe("<b>💰 Thu chi</b>\n<i>🏠 Menu chính › 💰 Thu chi</i>\n\nGhi thu chi");
    }

    [Fact]
    public void First_level_submenu_offers_actions_back_and_close()
    {
        var view = _renderer.Render(Catalog.Find("finance")!);

        Layout(view).ShouldBe(
        [
            ["➕ Khoản thu|act:finance.income", "➖ Khoản chi|act:finance.expense"],
            ["📊 Báo cáo|nav:finance.report"],
            [$"{BotTexts.BackButton}|nav:main"],
            [$"{BotTexts.CloseMenuButton}|close:finance"],
        ]);
    }

    [Fact]
    public void Deeper_submenu_also_offers_a_shortcut_home()
    {
        var view = _renderer.Render(Catalog.Find("finance.report")!);

        Layout(view)[^2].ShouldBe([$"{BotTexts.BackButton}|nav:finance", $"{BotTexts.HomeButton}|nav:main"]);
    }

    [Fact]
    public void Collapsed_view_keeps_a_single_open_button_that_remembers_the_page()
    {
        var view = MenuRenderer.RenderCollapsed(Catalog.Find("finance")!);

        view.Html.ShouldBe(BotTexts.MenuCollapsed);
        Layout(view).ShouldBe([[$"{BotTexts.OpenMenuButton}|open:finance"]]);
    }
}
