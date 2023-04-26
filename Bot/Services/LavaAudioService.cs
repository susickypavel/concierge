using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Victoria;
using Victoria.Node;
using Victoria.Node.EventArgs;
using Victoria.Player;

namespace Bot.Services;

public class LavaAudioService : IHostedService
{
    private readonly LavaNode _lavaNode;
    private readonly ILogger<LavaAudioService> _logger;
    public LavaAudioService(ILogger<LavaAudioService> logger, LavaNode lavaNode, DiscordSocketClient client)
    {
        _lavaNode = lavaNode;
        _logger = logger;
        
        client.Ready += Connect;

        _lavaNode.OnTrackEnd += OnTrackEndAsync;
        _lavaNode.OnTrackStart += OnTrackStartAsync;
        _lavaNode.OnStatsReceived += OnStatsReceivedAsync;
        _lavaNode.OnUpdateReceived += OnUpdateReceivedAsync;
        _lavaNode.OnWebSocketClosed += OnWebSocketClosedAsync;
        _lavaNode.OnTrackStuck += OnTrackStuckAsync;
        _lavaNode.OnTrackException += OnTrackExceptionAsync;
    }

    private Task OnTrackExceptionAsync(TrackExceptionEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
    {
        _logger.LogWarning("Track {TrackTitle} thrown an exception", arg.Track.Title);

        arg.Player.Vueue.Enqueue(arg.Track);

        return arg.Player.TextChannel.SendMessageAsync($"{arg.Track} has been requeued because it threw an exception.");
    }

    private Task OnTrackStuckAsync(TrackStuckEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
    {
        _logger.LogWarning("Track {TrackTitle} is stuck", arg.Track.Title);

        arg.Player.Vueue.Enqueue(arg.Track);

        return arg.Player.TextChannel.SendMessageAsync($"{arg.Track} has been requeued because it got stuck.");
    }

    private static Task OnWebSocketClosedAsync(WebSocketClosedEventArg arg)
    {
        return Task.CompletedTask;
    }

    private static Task OnStatsReceivedAsync(StatsEventArg arg)
    {
        return Task.CompletedTask;
    }

    private static Task OnUpdateReceivedAsync(UpdateEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
    {
        return Task.CompletedTask;
    }

    private async Task OnTrackStartAsync(TrackStartEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
    {
        _logger.LogDebug("Track '{TrackTitle}' started", arg.Track.Title);

        var embed = new EmbedBuilder()
            .WithColor(new Color(255, 0, 0))
            .AddField("Duration", arg.Track.Duration, true)
            .WithTitle(arg.Track.Title)
            .WithAuthor(arg.Track.Author)
            .WithUrl(arg.Track.Url);

        var artwork = await arg.Track.FetchArtworkAsync();

        if (artwork != null)
        {
            embed.WithImageUrl(artwork);
        }

        await arg.Player.TextChannel.SendMessageAsync(embed: embed.Build());
    }

    private async Task OnTrackEndAsync(TrackEndEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg)
    {
        _logger.LogDebug("Track '{TrackTitle}' ended", arg.Track.Title);

        if (arg.Player.Vueue.TryDequeue(out var nextTrack))
        {
            await arg.Player.PlayAsync(nextTrack);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task Connect()
    {
        await _lavaNode.ConnectAsync();
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
    }
}
