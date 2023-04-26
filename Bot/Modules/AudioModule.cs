using System.Text;
using Bot.Entities;
using Discord;
using Discord.Interactions;
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
    private readonly LavaNode _lavaNode;

    public AudioModule(LavaNode lavaNode)
    {
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

    [SlashCommand("play", "Bot plays a song.")]
    public async Task PlayAsync(string searchQuery)
    {
        await DeferAsync(ephemeral: true);

        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            var voiceState = Context.User as IVoiceState;

            if (voiceState?.VoiceChannel == null)
            {
                await FollowupAsync("You must be connected to a voice channel!", ephemeral: true);
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

        if (searchResponse.Status is SearchStatus.LoadFailed or SearchStatus.NoMatches)
        {
            await FollowupAsync($"I wasn't able to find anything for `{searchQuery}`.", ephemeral: true);
            return;
        }

        if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
        {
            player.Vueue.Enqueue(searchResponse.Tracks.Select(track => new ExtendedLavaTrack(track, Context.User)));
            await FollowupAsync($"Enqueued {searchResponse.Tracks.Count} songs.", ephemeral: true);
        }
        else
        {
            var track = searchResponse.Tracks.FirstOrDefault();

            if (track != null)
            {
                player.Vueue.Enqueue(new ExtendedLavaTrack(track, Context.User));
                await FollowupAsync($"Enqueued {track?.Title}", ephemeral: true);
            }
            else
            {
                await FollowupAsync("This should not happen, ever. lol");
            }
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

    [SlashCommand("np", "Shows current song it plays")]
    public async Task NowPlayingAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync("Woaaah there, I'm not playing any tracks.", ephemeral: true);
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

        if (player.Vueue.Count <= 0)
        {
            descriptionBuilder.AppendLine("**Queue is empty :(**");
        }
        else
        {
            var i = 1;

            foreach (var lavaTrack in player.Vueue)
            {
                descriptionBuilder.Append($"{i}. [{lavaTrack.Title}]({lavaTrack.Url})");

                if (lavaTrack is ExtendedLavaTrack extendedLavaTrack)
                {
                    descriptionBuilder.Append($" by {extendedLavaTrack.QueuedBy.Mention}");
                }

                descriptionBuilder.AppendLine();
                i++;
            }
        }

        embed.WithDescription(descriptionBuilder.ToString());

        await RespondAsync(embed: embed.Build());
    }
}
