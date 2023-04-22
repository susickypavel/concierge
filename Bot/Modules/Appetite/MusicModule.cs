using Discord.Interactions;

namespace Bot.Modules.Appetite;

public class MusicModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("echo", "Echo an input")]
    public async Task Echo(string input)
    {
        await RespondAsync(input);
    }
}