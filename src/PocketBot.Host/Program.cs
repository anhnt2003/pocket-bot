using PocketBot.Host;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddPocketBot(builder.Configuration);
builder.Build().Run();
