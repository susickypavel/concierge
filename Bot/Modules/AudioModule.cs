using System.Text;
using Bot.Entities;
using Bot.Handlers;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Victoria;
using Victoria.Node;
using Victoria.Player;
using Victoria.Responses.Search;

namespace Bot.Modules;

public class AudioModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack> _lavaNode;
    private readonly ILogger<AudioModule> _logger;

    public AudioModule(LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack> lavaNode, ILogger<AudioModule> logger)
    {
        _logger = logger;
        _lavaNode = lavaNode;
    }

    [SlashCommand("join", "Bot se připojí k tobě do voice")]
    public async Task JoinAsync()
    {
        if (_lavaNode.HasPlayer(Context.Guild))
        {
            await RespondAsync("`Já už tu jsem!`", ephemeral: true);
            return;
        }

        var voiceState = Context.User as IVoiceState;
        if (voiceState?.VoiceChannel == null)
        {
            await RespondAsync("`Musíš být v nějakém voice channelu!`", ephemeral: true);
            return;
        }

        try
        {
            await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
            await RespondAsync($"`Už jsem tady zas!`", ephemeral: true);
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message, ephemeral: true);
        }
    }

    [SlashCommand("leave", "Bot odejde z voice")]
    public async Task LeaveAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("`Ty nebo já nejsme připojení do voice.`", ephemeral: true);
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
            await RespondAsync($"`Tak já jdu.`", ephemeral: true);
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
            case SearchStatus.SearchResult:
                var nextTrack = new ExtendedLavaTrack(searchResponse.Tracks.First(), Context.User);
                player.TrackQueue.Enqueue(nextTrack, top);
                await FollowupAsync($"`Přidal jsem '{nextTrack.Title}'.`", ephemeral: true);
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

    [SlashCommand("pause", "Pozastaví přehrávání")]
    public async Task PauseAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("`Ty nebo já nejsme připojení do voice.`", ephemeral: true);
            return;
        }

        if (player.PlayerState == PlayerState.Paused)
        {
            await RespondAsync("`To už je zastavený!`", ephemeral: true);
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync("`Nemůžu zastavit vzduch!`", ephemeral: true);
            return;
        }

        try
        {
            await player.PauseAsync();
            await RespondAsync($"`Pozastaveno: '{player.Track.Title}'`", ephemeral: true);
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message, ephemeral: true);
        }
    }

    [SlashCommand("resume", "Pustí přehrávání")]
    public async Task ResumeAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("`Ty nebo já nejsme připojení do voice.`", ephemeral: true);
            return;
        }

        if (player.PlayerState != PlayerState.Paused)
        {
            await RespondAsync("`Nemůžu pustit vzduch!`", ephemeral: true);
            return;
        }

        try
        {
            await player.ResumeAsync();
            await RespondAsync($"`Pokračujém v: '{player.Track.Title}'`", ephemeral: true);
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message, ephemeral: true);
        }
    }

    [SlashCommand("skip", "Přeskočí video")]
    public async Task SkipAsync([Summary("pozice", "Číslo pořadí videa pro přeskočení")] [Autocomplete(typeof(QueueAutocompleteHandler))] int position = -1)
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("`Ty nebo já nejsme připojení do voice.`", ephemeral: true);
            return;
        }

        if (player.PlayerState != PlayerState.Playing)
        {
            await RespondAsync("`Nemůžu přeskočit video, když nic nehraju.`", ephemeral: true);
            return;
        }

        if (position >= player.TrackQueue.Count())
        {
            await RespondAsync("`Pozice není z rozsahu.`");
            return;
        }

        try
        {
            if (position < 0)
            {
                await player.StopAsync();
            }
            else
            {
                player.TrackQueue.RemoveAt(position);
            }

            await RespondAsync($"`Přeskočeno.`", ephemeral: true);
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message, ephemeral: true);
        }
    }

    [SlashCommand("volume", "Nastaví hlasitost bota <0,100>")]
    public async Task VolumeAsync(ushort volume)
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
            return;
        }

        if (volume > 100)
        {
            await RespondAsync("`Hlasitost je mimo rozsah <0,100>.`");
            return;
        }
        
        try
        {
            await player.SetVolumeAsync(volume);
            await RespondAsync($"`Hlasitost změněna na '{volume}%'.`", ephemeral: true);
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
            .AddField("Na přání", track.QueuedBy.Mention);

        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }

    [SlashCommand("queue", "Zobrazí frontu videí")]
    public async Task ShowQueue()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("`Ty nebo já nejsme připojení do voice.`", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithColor(new Color(255, 0, 0))
            .WithTitle("Fronta videí");

        var descriptionBuilder = new StringBuilder();

        if (player.PlayerState != PlayerState.None && player.Track != null)
        {
            descriptionBuilder.AppendLine($@"**Teď hraje: [{player.Track.Title}]({player.Track.Url})**
");
        }

        if (player.TrackQueue.IsEmpty())
        {
            descriptionBuilder.AppendLine("**Ve frontě nic není, pump it up!**");
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

    [SlashCommand("shuffle", "Zamíchá videa ve frontě.")]
    public async Task Shuffle()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("`Ty nebo já nejsme připojení do voice.`", ephemeral: true);
            return;
        }
        
        player.TrackQueue.Shuffle();

        await RespondAsync("`Fronta zamíchána.`", ephemeral: true);
    }
}
