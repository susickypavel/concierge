using Bot.TypeConverters;
using Discord;
using Discord.Interactions;

namespace Bot.Modules.Appetite;

[Group("music", "Music Appetite")]
public class MusicModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("add", "Vloží novou písničku pomocí embedu.")]
    public async Task Add((MusicPlatform platform, Uri url) link)
    {
        var embed =
            new EmbedBuilder()
                .WithUrl(link.url.OriginalString)
                .WithTitle(link.url.OriginalString)
                .WithFooter(Context.User.Username, Context.User.GetAvatarUrl())
                .AddField("Added by", Context.User.Mention, true)
                .AddField("Platform", link.platform.ToString(), true)
                .WithColor(GetEmbedColor(link.platform))
                .Build();

        await RespondAsync(embed: embed);

        var message = await GetOriginalResponseAsync();

        await message.AddReactionAsync(new Emoji("👍"));
        await message.AddReactionAsync(new Emoji("👎"));
        await message.AddReactionAsync(new Emoji("🦄"));
    }

    private static Color GetEmbedColor(MusicPlatform linkPlatform)
    {
        return linkPlatform switch
        {
            MusicPlatform.YouTube => new Color(255, 0, 0),
            MusicPlatform.Spotify => new Color(30, 215, 96),
            _ => new Color(255, 255, 255)
        };
    }
}