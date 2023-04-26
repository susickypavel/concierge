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
                .AddField("Added by", Context.User.Mention, true)
                .AddField("Platform", link.platform.ToString(), true)
                .WithColor(GetEmbedColor(link.platform))
                .Build();

        await RespondAsync(link.url.OriginalString);

        var message = await GetOriginalResponseAsync();

        if (Context.Channel.GetChannelType() == ChannelType.Text)
        {
            var textChannel = (Context.Channel as ITextChannel)!;

            var thread = await textChannel.CreateThreadAsync(name: link.url.OriginalString, message: message);

            await thread.SendMessageAsync(embed: embed);
        }

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
