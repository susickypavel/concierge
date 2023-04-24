using Bot.Services;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Victoria;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config => config.AddUserSecrets<Program>())
    .ConfigureServices(
        (_, services) =>
        {
            var discordSocketClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged
            });

            services.AddSingleton(discordSocketClient);
            services.AddLavaNode(nodeConfig =>
            {
                nodeConfig.Authorization = "fastasfuckboi";
            });
            services.AddSingleton<LavaAudioService>();
            services.AddHostedService<DiscordClientService>();
        }
    )
    .Build();

host.Run();