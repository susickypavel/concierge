using Bot.Services;
using Bot.Services.Handlers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Victoria;
using Victoria.WebSocket;

var host = Host.CreateDefaultBuilder(args)
    .UseSystemd()
    .ConfigureAppConfiguration(config => config.AddUserSecrets<Program>())
    .ConfigureServices(
        (_, services) =>
        {
            var discordSocketClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds |
                                 GatewayIntents.GuildMessages |
                                 GatewayIntents.GuildMessageReactions |
                                 GatewayIntents.GuildVoiceStates
            });
            
            services.AddSingleton(discordSocketClient);
            services.AddSingleton<InteractionService>();
            
            services.AddLavaNode(nodeConfig =>
            {
                nodeConfig.Authorization = "fastasfuckboi";
                nodeConfig.SocketConfiguration = new WebSocketConfiguration
                {
                    BufferSize = 4096
                };
            });
            
            services.AddHostedService<DiscordClientService>();
            services.AddHostedService<InteractionHandler>();
            services.AddHostedService<LavaAudioService>();
        }
    )
    .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Information); })
    .Build();

host.Run();
