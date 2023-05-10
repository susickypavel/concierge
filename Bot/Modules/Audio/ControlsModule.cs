using Bot.Entities;
using Bot.Enums;
using Bot.Handlers;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Victoria.Node;
using Victoria.Player;
using Victoria.Responses.Search;

namespace Bot.Modules.Audio;

public class ControlsModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack> _lavaNode;
    private readonly ILogger<ControlsModule> _logger;

    public ControlsModule(LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack> lavaNode, ILogger<ControlsModule> logger)
    {
        _logger = logger;
        _lavaNode = lavaNode;
    }

    /// <summary>
    ///     Finds song or whole playlist based on query input and places them in a queue.
    ///     If nothing plays at the moment, it takes the first song from the queue and plays it.
    /// </summary>
    /// <param name="searchQuery">Query used to search for videos</param>
    /// <param name="top">Whether to place song at the top of the queue</param>
    [SlashCommand("play", "Přidá video do fronty")]
    public async Task PlayAsync(
        [Summary(description: "Odkaz, nebo výraz pro vyhledání videí")]
        string searchQuery,
        [Summary(description: "Přidat písničku na začátek fronty")]
        bool top = false)
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
                player.TrackQueue.Enqueue(
                    searchResponse.Tracks.Select(track => new ExtendedLavaTrack(track, Context.User)));
                await FollowupAsync($"`Přidal jsem celý playlist {searchResponse.Tracks.Count} videí.`",
                    ephemeral: true);
                break;
            case SearchStatus.TrackLoaded:
            case SearchStatus.SearchResult:
                var nextTrack = new ExtendedLavaTrack(searchResponse.Tracks.First(), Context.User);
                player.TrackQueue.Enqueue(nextTrack, top);
                await FollowupAsync($"`Přidal jsem '{nextTrack.Title}'.`", ephemeral: true);
                break;
            case SearchStatus.LoadFailed:
                _logger.LogError("Couldn't load '{SearchQuery}', Exception: {ExceptionMessage}", searchQuery,
                    searchResponse.Exception.Message);
                await FollowupAsync($"`Nepodařilo se mi načíst '{searchQuery}'.`", ephemeral: true);
                return;
            case SearchStatus.NoMatches:
                await FollowupAsync($"`Nic takového jsem nenašel :(`", ephemeral: true);
                return;
            default:
                _logger.LogError("Something went wrong, Exception: {ExceptionMessage}",
                    searchResponse.Exception.Message);
                await FollowupAsync("`Něco se mi nepovedlo, zalogováno.`");
                return;
        }

        if (player.PlayerState is PlayerState.Playing or PlayerState.Paused) return;

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

    /// <summary>
    ///     If a specific position is supplied, it tries to remove a song on the specified index from the queue.
    ///     For the default position (-1), it skips the current song.
    /// </summary>
    /// <param name="position">Index of song in queue to skip</param>
    [SlashCommand("skip", "Přeskočí video")]
    public async Task SkipAsync(
        [Summary("pozice", "Číslo pořadí videa pro přeskočení")] [Autocomplete(typeof(QueueAutocompleteHandler))]
        int position = -1)
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

    [SlashCommand("loop", "Nastaví smyčku přehrávání")]
    public async Task Loop(LoopMode mode)
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("`Ty nebo já nejsme připojení do voice.`", ephemeral: true);
            return;
        }

        player.TrackQueue.QueueMode = mode;
        
        await RespondAsync($"`Smyčka nastavena na '{mode}'`", ephemeral: true);
    }
}
