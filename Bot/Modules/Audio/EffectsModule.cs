using Bot.Entities;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Victoria.Node;

namespace Bot.Modules.Audio;

public class EffectsModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack> _lavaNode;
    private readonly ILogger<EffectsModule> _logger;

    public EffectsModule(LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack> lavaNode, ILogger<EffectsModule> logger)
    {
        _logger = logger;
        _lavaNode = lavaNode;
    }

    [SlashCommand("volume", "Nastaví hlasitost bota <0,100>")]
    public async Task VolumeAsync(ushort volume)
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("I'm not connected to a voice channel.", ephemeral: true);
            return;
        }

        if (volume > 100)
        {
            await RespondAsync("`Hlasitost je mimo rozsah <0,100>.`");
            return;
        }

        try
        {
            await player.SetVolumeAsync(volume);
            await RespondAsync($"`Hlasitost změněna na '{volume}%'.`", ephemeral: true);
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message, ephemeral: true);
        }
    }
}
