using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bot.Services;

public class DiscordClientService : IHostedService
{
    private readonly string? _token;

    private readonly DiscordSocketClient _client;
    private readonly ILogger<DiscordClientService> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public DiscordClientService(DiscordSocketClient client, IConfiguration config,
        ILogger<DiscordClientService> logger, IHostApplicationLifetime lifetime)
    {
        _token = config["DISCORD_TOKEN"];
        _logger = logger;
        _lifetime = lifetime;
        _client = client;

        _client.Log += Log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            TokenUtils.ValidateToken(TokenType.Bot, _token);

            await _client.LoginAsync(TokenType.Bot, _token, false);
            await _client.StartAsync();
        }
        catch
        {
            _logger.LogError("Supplied token was invalid. Stopping application");
            _lifetime.StopApplication();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.LogoutAsync();
    }

    private Task Log(LogMessage arg)
    {
        _logger.Log(ConvertLogSeverityToLevel(arg.Severity), "{ArgMessage}", arg.Message ?? arg.Exception.Message);

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
