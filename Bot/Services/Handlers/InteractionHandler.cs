using System.Reflection;
using Bot.Enums;
using Bot.TypeConverters;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bot.Services.Handlers;

public class InteractionHandler : IHostedService
{
    private readonly ILogger<InteractionHandler> _logger;
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interaction;
    private readonly IServiceProvider _services;

    public InteractionHandler(DiscordSocketClient client, ILogger<InteractionHandler> logger, IServiceProvider services,
        InteractionService interaction)
    {
        _logger = logger;
        _client = client;
        _interaction = interaction;
        _services = services;

        client.Ready += OnReady;
        client.InteractionCreated += OnInteractionAsync;
    }

    private async Task OnSlashCommandExecuted(SlashCommandInfo arg1, IInteractionContext ctx, IResult result)
    {
        if (!result.IsSuccess)
        {
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    await ctx.Interaction.RespondAsync($"`Unmet Precondition: {result.ErrorReason}`");
                    break;
                case InteractionCommandError.UnknownCommand:
                    await ctx.Interaction.RespondAsync("`Unknown command`");
                    break;
                case InteractionCommandError.BadArgs:
                    await ctx.Interaction.RespondAsync("`Invalid number or arguments`");
                    break;
                case InteractionCommandError.Exception:
                    await ctx.Interaction.RespondAsync($"`Command exception: {result.ErrorReason}`");
                    break;
                case InteractionCommandError.Unsuccessful:
                    await ctx.Interaction.RespondAsync("`Command could not be executed`");
                    break;
                case InteractionCommandError.ConvertFailed:
                    await ctx.Interaction.RespondAsync($"`{result.ErrorReason}`", ephemeral: true);
                    break;
                case InteractionCommandError.ParseFailed:
                    _logger.LogCritical("`SlashCommand parse error`");
                    break;
                case null:
                    _logger.LogCritical("`SlashCommand was unsuccessful but no error was provided`");
                    break;
                default:
                    _logger.LogCritical("`SlashCommand unknown error`");
                    break;
            }
        }
    }

    private async Task OnInteractionAsync(SocketInteraction arg)
    {
        var ctx = new SocketInteractionContext(_client, arg);
        await _interaction.ExecuteCommandAsync(ctx, _services);
    }

    private async Task OnReady()
    {
        _interaction.AddTypeConverter<(MusicPlatform, Uri)>(new MusicPlatformUrl());
        _interaction.AddTypeConverter<LoopMode>(new LoopModeTypeConverter());

        var loadedModules = await _interaction.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        _logger.LogInformation("Successfully registered {Count} modules", loadedModules.Count());

#if !DEBUG
            _logger.LogInformation("Registering commands globally:");
            await _interaction.AddCommandsGloballyAsync(true);
            await _interaction.RegisterCommandsGloballyAsync();
#endif

        _interaction.SlashCommandExecuted += OnSlashCommandExecuted;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
