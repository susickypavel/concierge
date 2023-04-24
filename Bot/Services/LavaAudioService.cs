using Victoria.Node;
using Victoria.Node.EventArgs;
using Victoria.Player;

namespace Bot.Services;

public class LavaAudioService
{
    private readonly LavaNode _lavaNode;

    public LavaAudioService(LavaNode lavaNode)
    {
        _lavaNode = lavaNode;

        _lavaNode.ConnectAsync();
        
        _lavaNode.OnTrackEnd += OnTrackEndAsync;
        _lavaNode.OnTrackStart += OnTrackStartAsync;
        _lavaNode.OnStatsReceived += OnStatsReceivedAsync;
        _lavaNode.OnUpdateReceived += OnUpdateReceivedAsync;
        _lavaNode.OnWebSocketClosed += OnWebSocketClosedAsync;
        _lavaNode.OnTrackStuck += OnTrackStuckAsync;
        _lavaNode.OnTrackException += OnTrackExceptionAsync;
    }
    
    private static Task OnTrackExceptionAsync(TrackExceptionEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg) {
        arg.Player.Vueue.Enqueue(arg.Track);
        return arg.Player.TextChannel.SendMessageAsync($"{arg.Track} has been requeued because it threw an exception.");
    }

    private static Task OnTrackStuckAsync(TrackStuckEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg) {
        arg.Player.Vueue.Enqueue(arg.Track);
        return arg.Player.TextChannel.SendMessageAsync($"{arg.Track} has been requeued because it got stuck.");
    }

    private Task OnWebSocketClosedAsync(WebSocketClosedEventArg arg) {
        return Task.CompletedTask;
    }

    private Task OnStatsReceivedAsync(StatsEventArg arg) {
        return Task.CompletedTask;
    }

    private static Task OnUpdateReceivedAsync(UpdateEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg) {
        // return arg.Player.TextChannel.SendMessageAsync(
        //     $"Player update received: {arg.Position}/{arg.Track?.Duration}");
        
        return Task.CompletedTask;
    }

    private static Task OnTrackStartAsync(TrackStartEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg) {
        return arg.Player.TextChannel.SendMessageAsync($"Started playing {arg.Track}.");
    }

    private static Task OnTrackEndAsync(TrackEndEventArg<LavaPlayer<LavaTrack>, LavaTrack> arg) {
        return arg.Player.TextChannel.SendMessageAsync($"Finished playing {arg.Track}.");
    }
}