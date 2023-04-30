using System.Text;
using Bot.Entities;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Victoria;
using Victoria.Node;
using Victoria.Player;
using Victoria.Responses.Search;

namespace Bot.Modules;

// TODO: Seek command
// TODO: Remove from queue command
// TODO: Shuffle command

public class AudioModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack> _lavaNode;
    private readonly ILogger<AudioModule> _logger;

    public AudioModule(LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack> lavaNode, ILogger<AudioModule> logger)
    {
        _logger = logger;
        _lavaNode = lavaNode;
    }

    [SlashCommand("join", "Bot joins the voice channel")]
    public async Task JoinAsync()
    {
        if (_lavaNode.HasPlayer(Context.Guild))
        {
            await RespondAsync("I'm already connected to a voice channel!", ephemeral: true);
            return;
        }

        var voiceState = Context.User as IVoiceState;
        if (voiceState?.VoiceChannel == null)
        {
            await RespondAsync("You must be connected to a voice channel!", ephemeral: true);
            return;
        }

        try
        {
            await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
            await RespondAsync($"Joined {voiceState.VoiceChannel.Name}!", ephemeral: true);
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message, ephemeral: true);
        }
    }

    [SlashCommand("leave", "Bot leaves the voice channel")]
    public async Task LeaveAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to any voice channels!", ephemeral: true);
            return;
        }

        var voiceChannel = (Context.User as IVoiceState)?.VoiceChannel ?? player.VoiceChannel;
        if (voiceChannel == null)
        {
            await RespondAsync("Not sure which voice channel to disconnect from.", ephemeral: true);
            return;
        }

        try
        {
            await _lavaNode.LeaveAsync(voiceChannel);
            await RespondAsync($"I've left {voiceChannel.Name}!", ephemeral: true);
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message, ephemeral: true);
        }
    }

    [SlashCommand("play", "Přidá video do fronty")]
    public async Task PlayAsync(string searchQuery, bool top = false)
    {
        await DeferAsync(ephemeral: true);

        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            var voiceState = Context.User as IVoiceState;

            if (voiceState?.VoiceChannel == null)
            {
                await FollowupAsync("`Musíš být připojený do voice.`", ephemeral: true);
                return;
            }

            try
            {
                player = await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
            }
            catch (Exception exception)
            {
                await FollowupAsync(exception.Message, ephemeral: true);
            }
        }

        var searchResponse = await _lavaNode.SearchAsync(
            Uri.IsWellFormedUriString(searchQuery, UriKind.Absolute) ? SearchType.Direct : SearchType.YouTube,
            searchQuery);

        switch (searchResponse.Status)
        {
            case SearchStatus.PlaylistLoaded:
                player.TrackQueue.Enqueue(searchResponse.Tracks.Select(track => new ExtendedLavaTrack(track, Context.User)));
                await FollowupAsync($"`Přidal jsem celý playlist {searchResponse.Tracks.Count} videí.`", ephemeral: true);
                break;
            case SearchStatus.TrackLoaded:
                var nextTrack = new ExtendedLavaTrack(searchResponse.Tracks.First(), Context.User);
                player.TrackQueue.Enqueue(nextTrack, top);
                await FollowupAsync($"`Přidal jsem {nextTrack.Title}.`", ephemeral: true);
                break;
            case SearchStatus.LoadFailed:
                _logger.LogError("Couldn't load '{SearchQuery}', Exception: {ExceptionMessage}", searchQuery, searchResponse.Exception.Message);
                await FollowupAsync($"`Nepodařilo se mi načíst '{searchQuery}'.`", ephemeral: true);
                return;
            case SearchStatus.NoMatches:
                await FollowupAsync($"`Nic takového jsem nenašel :(`", ephemeral: true);
                return;
            default:
                _logger.LogError("Something went wrong, Exception: {ExceptionMessage}", searchResponse.Exception.Message);
                await FollowupAsync("`Něco se mi nepovedlo, zalogováno.`");
                return;
        }
        
        if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
        {
            return;
        }
        
        if (player.TrackQueue.TryDequeue(out var currentTrack) && currentTrack != null)
        {
            await player.PlayAsync(currentTrack);
        }
    }

    [SlashCommand("pause", "Pauses current song")]
    public async Task PauseAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync("I cannot pause when I'm not playing anything!", ephemeral: true);
            return;
        }

        try
        {
            await player.PauseAsync();
            await RespondAsync($"Paused: {player.Track.Title}", ephemeral: true);
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message, ephemeral: true);
        }
    }

    [SlashCommand("resume", "Resume playing of current song")]
    public async Task ResumeAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
            return;
        }

        if (player.PlayerState != PlayerState.Paused)
        {
            await RespondAsync("I cannot resume when I'm not playing anything!", ephemeral: true);
            return;
        }

        try
        {
            await player.ResumeAsync();
            await RespondAsync($"Resumed: {player.Track.Title}", ephemeral: true);
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message, ephemeral: true);
        }
    }

    [SlashCommand("skip", "Skips current song.")]
    public async Task SkipAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync("Woaaah there, I can't skip when nothing is playing.", ephemeral: true);
            return;
        }

        try
        {
            if (player.Vueue.Count > 0)
            {
                await player.SkipAsync();
            }
            else
            {
                await player.StopAsync();
            }

            await RespondAsync($"Skipped", ephemeral: true);
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message, ephemeral: true);
        }
    }

    [SlashCommand("volume", "sets bot volume")]
    public async Task VolumeAsync(ushort volume)
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
            return;
        }

        try
        {
            await player.SetVolumeAsync(volume);
            await RespondAsync($"I've changed the player volume to {volume}.", ephemeral: true);
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message, ephemeral: true);
        }
    }

    [SlashCommand("now-playing", "Co teď hraje?")]
    public async Task NowPlayingAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("`Ty nebo já nejsme připojení do voice.`", ephemeral: true);
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync("`Nic nehraje, pump it up?`", ephemeral: true);
            return;
        }

        var track = player.Track;
        var artwork = await track.FetchArtworkAsync();

        var embed = new EmbedBuilder()
            .WithAuthor(track.Author, null, track.Url)
            .WithTitle(track.Title)
            .WithUrl(track.Url)
            .WithImageUrl(artwork)
            .WithFooter($"{track.Position:hh\\:mm\\:ss} / {track.Duration}")
            .WithColor(new Color(255, 0, 0))
            .AddField("Requested by", track.QueuedBy.Mention);

        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    [SlashCommand("queue", "Shows current queue")]
    public async Task ShowQueue()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithColor(new Color(255, 0, 0))
            .WithTitle("Queue");

        var descriptionBuilder = new StringBuilder();

        if (player.PlayerState != PlayerState.None && player.Track != null)
        {
            descriptionBuilder.AppendLine($@"**Now playing: [{player.Track.Title}]({player.Track.Url})**
");
        }

        if (player.TrackQueue.IsEmpty())
        {
            descriptionBuilder.AppendLine("**Queue is empty :(**");
        }
        else
        {
            var i = 1;

            foreach (var track in player.TrackQueue)
            {
                descriptionBuilder.Append($"{i}. [{track.Title}]({track.Url})");
                descriptionBuilder.AppendLine($" by {track.QueuedBy.Mention}");
                i++;
            }
        }

        embed.WithDescription(descriptionBuilder.ToString());

        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }
}
