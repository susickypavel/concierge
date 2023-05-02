using System.Text;
using Bot.Entities;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Victoria;
using Victoria.Node;
using Victoria.Player;

namespace Bot.Modules.Audio;

public class InfoModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack> _lavaNode;
    private readonly ILogger<InfoModule> _logger;

    public InfoModule(LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack> lavaNode, ILogger<InfoModule> logger)
    {
        _logger = logger;
        _lavaNode = lavaNode;
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
}
