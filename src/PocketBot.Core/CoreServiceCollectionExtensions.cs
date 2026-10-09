using Microsoft.Extensions.DependencyInjection;
using PocketBot.Core.Commands;
using PocketBot.Core.Menus;
using PocketBot.Core.Messaging;
using PocketBot.Core.Routing;

namespace PocketBot.Core;

public static class CoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the bot's menu, commands and router. The host must also register
    /// an <see cref="Access.AccessPolicy"/> and an <see cref="Messaging.IChatGateway"/>.
    /// </summary>
    public static IServiceCollection AddPocketBotCore(this IServiceCollection services)
    {
        services.AddSingleton(new MenuCatalog(DefaultMenu.Build()));
        services.AddSingleton<MenuRenderer>();
        services.AddSingleton<ICommandHandler, StartCommand>();
        services.AddSingleton<ICommandHandler>(sp => OpenPage(sp, CommandNames.Menu, DefaultMenu.RootId));
        services.AddSingleton<ICommandHandler>(sp => OpenPage(sp, CommandNames.Help, DefaultMenu.HelpId));
        services.AddSingleton<MenuCallbackHandler>();
        services.AddSingleton<UpdateRouter>();
        return services;
    }

    private static OpenPageCommand OpenPage(IServiceProvider sp, string command, string pageId) =>
        new(command, pageId,
            sp.GetRequiredService<IChatGateway>(),
            sp.GetRequiredService<MenuCatalog>(),
            sp.GetRequiredService<MenuRenderer>());
}
