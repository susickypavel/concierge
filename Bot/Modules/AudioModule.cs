using Bot.Services;
using Discord;
using Discord.Interactions;
using Victoria;
using Victoria.Node;
using Victoria.Player;
using Victoria.Responses.Search;

namespace Bot.Modules;

// TODO: Seek command

public class AudioModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LavaNode _lavaNode;
    private readonly LavaAudioService _lavaAudioService;

    public AudioModule(LavaNode lavaNode, LavaAudioService lavaAudioService)
    {
        _lavaNode = lavaNode;
        _lavaAudioService = lavaAudioService;
    }

    [SlashCommand("join", "Bot joins the voice channel")]
    public async Task JoinAsync()
    {
        if (_lavaNode.HasPlayer(Context.Guild))
        {
            await RespondAsync("I'm already connected to a voice channel!");
            return;
        }

        var voiceState = Context.User as IVoiceState;
        if (voiceState?.VoiceChannel == null)
        {
            await RespondAsync("You must be connected to a voice channel!");
            return;
        }

        try
        {
            await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
            await RespondAsync($"Joined {voiceState.VoiceChannel.Name}!");
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message);
        }
    }

    [SlashCommand("leave", "Bot leaves the voice channel")]
    public async Task LeaveAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to any voice channels!");
            return;
        }

        var voiceChannel = (Context.User as IVoiceState).VoiceChannel ?? player.VoiceChannel;
        if (voiceChannel == null)
        {
            await RespondAsync("Not sure which voice channel to disconnect from.");
            return;
        }

        try
        {
            await _lavaNode.LeaveAsync(voiceChannel);
            await RespondAsync($"I've left {voiceChannel.Name}!");
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message);
        }
    }

    [SlashCommand("play", "Bot plays a song.")]
    public async Task PlayAsync(string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            await RespondAsync("Please provide search terms.");
            return;
        }

        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            var voiceState = Context.User as IVoiceState;
            if (voiceState?.VoiceChannel == null)
            {
                await RespondAsync("You must be connected to a voice channel!");
                return;
            }

            try
            {
                player = await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
                await RespondAsync($"Joined {voiceState.VoiceChannel.Name}!");
            }
            catch (Exception exception)
            {
                await RespondAsync(exception.Message);
            }
        }

        var searchResponse = await _lavaNode.SearchAsync(
            Uri.IsWellFormedUriString(searchQuery, UriKind.Absolute) ? SearchType.Direct : SearchType.YouTube,
            searchQuery);
        if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches)
        {
            await RespondAsync($"I wasn't able to find anything for `{searchQuery}`.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
        {
            player.Vueue.Enqueue(searchResponse.Tracks);
            await RespondAsync($"Enqueued {searchResponse.Tracks.Count} songs.");
        }
        else
        {
            var track = searchResponse.Tracks.FirstOrDefault();
            player.Vueue.Enqueue(track);

            await RespondAsync($"Enqueued {track?.Title}");
        }

        if (player.PlayerState is PlayerState.Playing or PlayerState.Paused)
        {
            return;
        }

        player.Vueue.TryDequeue(out var lavaTrack);
        await player.PlayAsync(lavaTrack);
    }

    [SlashCommand("pause", "Pauses current song")]
    public async Task PauseAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to a voice channel.");
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync("I cannot pause when I'm not playing anything!");
            return;
        }

        try
        {
            await player.PauseAsync();
            await RespondAsync($"Paused: {player.Track.Title}");
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message);
        }
    }

    [SlashCommand("resume", "Resume playing of current song")]
    public async Task ResumeAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to a voice channel.");
            return;
        }

        if (player.PlayerState != PlayerState.Paused)
        {
            await RespondAsync("I cannot resume when I'm not playing anything!");
            return;
        }

        try
        {
            await player.ResumeAsync();
            await RespondAsync($"Resumed: {player.Track.Title}");
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message);
        }
    }

    [SlashCommand("skip", "Skips current song.")]
    public async Task SkipAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to a voice channel.");
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync("Woaaah there, I can't skip when nothing is playing.");
            return;
        }
        
        try
        {
            var (skipped, currenTrack) = await player.SkipAsync();
            await RespondAsync($"Skipped: {skipped.Title}\nNow Playing: {currenTrack.Title}");
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message);
        }
    }

    [SlashCommand("volume", "sets bot volume")]
    public async Task VolumeAsync(ushort volume)
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to a voice channel.");
            return;
        }

        try
        {
            await player.SetVolumeAsync(volume);
            await RespondAsync($"I've changed the player volume to {volume}.");
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message);
        }
    }

    [SlashCommand("np", "Shows current song it plays")]
    public async Task NowPlayingAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to a voice channel.");
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync("Woaaah there, I'm not playing any tracks.");
            return;
        }

        var track = player.Track;
        var artwork = await track.FetchArtworkAsync();

        var embed = new EmbedBuilder()
            .WithAuthor(track.Author, Context.Client.CurrentUser.GetAvatarUrl(), track.Url)
            .WithTitle($"Now Playing: {track.Title}")
            .WithImageUrl(artwork)
            .WithFooter($"{track.Position}/{track.Duration}");

        await RespondAsync(embed: embed.Build());
    }
}