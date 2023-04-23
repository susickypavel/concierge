using System.Reflection;
using Bot.TypeConverters;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bot.Services;

public class DiscordClientService : IHostedService
{
    private DiscordSocketClient Client { get; }
    
    private readonly string? _token;
    private readonly ILogger<DiscordClientService> _logger;
    private readonly IServiceProvider _services;
    private readonly InteractionService _interaction;

    public DiscordClientService(IConfiguration config, ILogger<DiscordClientService> logger, IServiceProvider services)
    {
        _token = config["Discord:Token"];
        _logger = logger;
        _services = services;
        
        Client = new DiscordSocketClient();
        Client.Log += Log;
        Client.Ready += OnReady;
        
        _interaction = new InteractionService(Client);
    }

    private async Task OnReady()
    {
        _interaction.AddTypeConverter<Uri>(new UrlConverter());

        await _interaction.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        
        await _interaction.RegisterCommandsGloballyAsync();
        await _interaction.AddCommandsToGuildAsync(926788615252639774, true);

        Client.InteractionCreated += OnInteractionAsync;
    }

    private async Task OnInteractionAsync(SocketInteraction arg)
    {
        var ctx = new SocketInteractionContext(Client, arg);
        await _interaction.ExecuteCommandAsync(ctx, _services);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Client.LoginAsync(TokenType.Bot, _token);
        await Client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Client.LogoutAsync();
    }
    
    private Task Log(LogMessage arg)
    {
        _logger.Log(ConvertLogSeverityToLevel(arg.Severity), "{ArgMessage}", arg.Message);
        return Task.CompletedTask;
    }

    private static LogLevel ConvertLogSeverityToLevel(LogSeverity severity)
    {
        return severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.None
        };
    }
}