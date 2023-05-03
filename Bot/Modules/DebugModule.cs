#if DEBUG
using System.Text;
using Discord.Interactions;

namespace Bot.Modules;

public class DebugModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly InteractionService _interaction;

    public DebugModule(InteractionService interaction)
    {
        _interaction = interaction;
    }

    [SlashCommand("commands-register", "Register commands through Discord's API")]
    public async Task RegisterCommands()
    {
        await _interaction.AddCommandsToGuildAsync(Context.Guild.Id, true);
        var commands = await _interaction.RegisterCommandsToGuildAsync(Context.Guild.Id);

        var builder = new StringBuilder($@"Registered {commands.Count} commands.
");
        
        foreach (var command in commands)
        {
            builder.AppendLine($"- {command.Name} ({command.IsEnabledInDm})");
        }
        
        await RespondAsync(builder.ToString(), ephemeral: true);
    }
}
#endif
