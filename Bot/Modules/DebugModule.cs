using Discord.Interactions;

namespace Bot.Modules;

public class DebugModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly InteractionService _interaction;

    public DebugModule(InteractionService interaction)
    {
        _interaction = interaction;
    }
    
    [SlashCommand("commands", "add commands")]
    public async Task RegisterCommands()
    {
        await _interaction.AddCommandsToGuildAsync(926788615252639774, true);
        await _interaction.RegisterCommandsToGuildAsync(926788615252639774);
        
        await RespondAsync("Registered.", ephemeral: true);
    }
}
