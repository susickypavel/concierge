using Bot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Victoria;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config => config.AddUserSecrets<Program>())
    .ConfigureServices(
        (_, services) =>
        {
            var discordSocketClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildMessageReactions | GatewayIntents.GuildVoiceStates
            });

            var interactionService = new InteractionService(discordSocketClient);

            services.AddSingleton(discordSocketClient);
            services.AddSingleton(interactionService);
            services.AddLavaNode(nodeConfig =>
            {
                nodeConfig.Authorization = "fastasfuckboi";
            });
            services.AddHostedService<DiscordClientService>();
            services.AddSingleton<LavaAudioService>();
        }
    )
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

host.Run();