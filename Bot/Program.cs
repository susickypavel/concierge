using Bot.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config => config.AddUserSecrets<Program>())
    .ConfigureServices(
        (_, services) =>
        {
            services.AddHostedService<DiscordClientService>();
        }
    )
    .Build();

host.Run();