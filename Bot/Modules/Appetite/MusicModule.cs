using Discord.Interactions;

namespace Bot.Modules.Appetite;

public class MusicModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("echo", "Echo an input")]
    public async Task Echo(Uri input)
    {
        await RespondAsync(input.Host);
    }
}