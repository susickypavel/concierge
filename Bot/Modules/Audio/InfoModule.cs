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
    public async Task ShowQueue([Summary(description: "Stránka fronty")] byte page = 1)
    {
        var (embed, component) = await CreateQueueEmbed(page);

        if (component != null)
        {
            await RespondAsync(embed: embed, components: component, ephemeral: true);
        }
        else
        {
            await RespondAsync(embed: embed, ephemeral: true);
        }
    }

    [ComponentInteraction("queue-page-select")]
    public async Task HandlePageSelect(string pageValue)
    {
        var page = Convert.ToByte(pageValue);

        var interaction = Context.Interaction as IComponentInteraction;

        async void UpdateEmbed(MessageProperties properties)
        {
            var (embed, _) = await CreateQueueEmbed(page);
            properties.Embed = embed;
        }

        await interaction!.UpdateAsync(UpdateEmbed);
    }

    private async Task<(Embed, MessageComponent?)> CreateQueueEmbed(byte page)
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("`Ty nebo já nejsme připojení do voice.`", ephemeral: true);
        }

        var pagesCount = Math.Ceiling(player.TrackQueue.Count() / (double) Constants.TracksPerQueuePage);
        var pageOffset = (page - 1) * Constants.TracksPerQueuePage;
        
        var embed = new EmbedBuilder()
            .WithColor(new Color(255, 0, 0))
            .WithTitle($"Fronta videí ({page}/{pagesCount})")
            .WithFooter(
                $"{Constants.LoopModeFlags[player.TrackQueue.QueueMode]} Smyčka: {player.TrackQueue.QueueMode}");

        var descriptionBuilder = new StringBuilder();

        if (player.PlayerState != PlayerState.None && player.Track != null)
        {
            descriptionBuilder.AppendLine($@"**Teď hraje: [{player.Track.Title}]({player.Track.Url}) od {player.Track.QueuedBy.Mention}**
");
        }

        if (player.TrackQueue.IsEmpty())
        {
            descriptionBuilder.AppendLine("**Ve frontě nic není, pump it up!**");
        }
        else
        {
            var tracks = player.TrackQueue
                .Skip(pageOffset)
                .Take(Constants.TracksPerQueuePage);

            var i = pageOffset;

            foreach (var track in tracks)
            {
                var title = track.Title
                    .Trim()
                    [..Math.Min(track.Title.Length, Constants.TrackTitleMaxLength)]
                    .PadRight(Constants.TrackTitleMaxLength);

                if (track.Title.Length > Constants.TrackTitleMaxLength)
                {
                    title = title[..(Constants.TrackTitleMaxLength - 3)]
                        .PadRight(Constants.TrackTitleMaxLength, '.');
                }

                descriptionBuilder.AppendLine($"`[{(i + 1).ToString().PadLeft(2, '0')}] {title}` [`🌐`]({track.Url})");
                i++;
            }
        }

        embed.WithDescription(descriptionBuilder.ToString());

        if (pagesCount <= 1) return (embed.Build(), null);

        var menu = new SelectMenuBuilder()
            .WithPlaceholder("Vyber stránku")
            .WithCustomId("queue-page-select")
            .WithMinValues(1)
            .WithMaxValues(1);

        for (var i = 0; i < pagesCount; i++)
        {
            menu.AddOption($"{i + 1}", $"{i + 1}");
        }

        var component = new ComponentBuilder()
            .WithSelectMenu(menu);

        return (embed.Build(), component.Build());
    }
}
