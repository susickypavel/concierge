using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bot.Services;

public class DiscordClientService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly string? _token;
    private readonly ILogger<DiscordClientService> _logger;
    
    public DiscordClientService(IConfiguration config, ILogger<DiscordClientService> logger)
    {
        _token = config["Discord:Token"];
        _client = new DiscordSocketClient();
        _client.Log += Log;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _client.LoginAsync(TokenType.Bot, _token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.LogoutAsync();
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