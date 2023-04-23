using Discord;
using Discord.Interactions;

namespace Bot.Modules.Appetite;

[Group("music", "Music Appetite")]
public class MusicModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("add", "Vloží novou písničku pomocí embedu.")]
    public async Task Add(Uri input)
    {
        var embed =
            new EmbedBuilder()
                .WithUrl(input.OriginalString)
                .WithTitle(input.OriginalString)
                .WithDescription(Context.User.Mention)
                .WithFooter("Footer", Context.User.GetAvatarUrl())
                .Build();

        await RespondAsync(embed: embed);

        var message = await GetOriginalResponseAsync();
        
        await message.AddReactionAsync(new Emoji("🦄"));
    }
}