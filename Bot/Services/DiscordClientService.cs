using System.Reflection;
using Bot.TypeConverters;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Victoria.Node;

namespace Bot.Services;

public class DiscordClientService : IHostedService
{
    private readonly DiscordSocketClient _client;

    private readonly string? _token;
    private readonly ILogger<DiscordClientService> _logger;
    private readonly IServiceProvider _services;
    private readonly InteractionService _interaction;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly LavaNode _lavaNode;

    public DiscordClientService(LavaNode lavaNode, DiscordSocketClient client, IConfiguration config,
        ILogger<DiscordClientService> logger, IServiceProvider services, IHostApplicationLifetime lifetime)
    {
        _token = config["DISCORD_TOKEN"];
        _logger = logger;
        _services = services;
        _lifetime = lifetime;
        _lavaNode = lavaNode;

        _client = client;

        _client.Log += Log;
        _client.Ready += OnReady;
        _client.InteractionCreated += OnInteractionAsync;

        _interaction = new InteractionService(_client);
    }

    private async Task OnReady()
    {
        // TODO: handle thrown errors

        _interaction.AddTypeConverter<(MusicPlatform, Uri)>(new MusicPlatformUrl());

        await _interaction.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

#if !DEBUG
            _logger.LogInformation("Registering commands globally:");
            await _interaction.AddCommandsGloballyAsync(true);
            await _interaction.RegisterCommandsGloballyAsync();
#endif

        _interaction.SlashCommandExecuted += OnSlashCommandExecuted;
    }

    private async Task OnSlashCommandExecuted(SlashCommandInfo arg1, IInteractionContext ctx, IResult result)
    {
        if (!result.IsSuccess)
        {
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    await ctx.Interaction.RespondAsync($"Unmet Precondition: {result.ErrorReason}");
                    break;
                case InteractionCommandError.UnknownCommand:
                    await ctx.Interaction.RespondAsync("Unknown command");
                    break;
                case InteractionCommandError.BadArgs:
                    await ctx.Interaction.RespondAsync("Invalid number or arguments");
                    break;
                case InteractionCommandError.Exception:
                    await ctx.Interaction.RespondAsync($"Command exception: {result.ErrorReason}");
                    break;
                case InteractionCommandError.Unsuccessful:
                    await ctx.Interaction.RespondAsync("Command could not be executed");
                    break;
                case InteractionCommandError.ConvertFailed:
                    await ctx.Interaction.RespondAsync(result.ErrorReason, ephemeral: true);
                    break;
                case InteractionCommandError.ParseFailed:
                    _logger.LogCritical("SlashCommand parse error");
                    break;
                case null:
                    _logger.LogCritical("SlashCommand was unsuccessful but no error was provided");
                    break;
                default:
                    _logger.LogCritical("SlashCommand unknown error");
                    break;
            }
        }
    }

    private async Task OnInteractionAsync(SocketInteraction arg)
    {
        var ctx = new SocketInteractionContext(_client, arg);
        await _interaction.ExecuteCommandAsync(ctx, _services);
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
        foreach (var player in _lavaNode.Players)
        {
            if (_lavaNode.IsConnected)
            {
                await _lavaNode.LeaveAsync(player.VoiceChannel);
            }
        }

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