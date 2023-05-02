using Bot.Entities;
using Discord;
using Discord.Interactions;
using Victoria.Node;

namespace Bot.Modules.Audio;

public class MiscModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack> _lavaNode;

    public MiscModule(LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack> lavaNode)
    {
        _lavaNode = lavaNode;
    }

    [SlashCommand("join", "Bot se připojí k tobě do voice")]
    public async Task JoinAsync()
    {
        if (_lavaNode.HasPlayer(Context.Guild))
        {
            await RespondAsync("`Já už tu jsem!`", ephemeral: true);
            return;
        }

        var voiceState = Context.User as IVoiceState;
        if (voiceState?.VoiceChannel == null)
        {
            await RespondAsync("`Musíš být v nějakém voice channelu!`", ephemeral: true);
            return;
        }

        try
        {
            await _lavaNode.JoinAsync(voiceState.VoiceChannel, Context.Channel as ITextChannel);
            await RespondAsync($"`Už jsem tady zas!`", ephemeral: true);
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message, ephemeral: true);
        }
    }

    [SlashCommand("leave", "Bot odejde z voice")]
    public async Task LeaveAsync()
    {
        if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
        {
            await RespondAsync("`Ty nebo já nejsme připojení do voice.`", ephemeral: true);
            return;
        }

        var voiceChannel = (Context.User as IVoiceState)?.VoiceChannel ?? player.VoiceChannel;
        if (voiceChannel == null)
        {
            await RespondAsync("Not sure which voice channel to disconnect from.", ephemeral: true);
            return;
        }

        try
        {
            await _lavaNode.LeaveAsync(voiceChannel);
            await RespondAsync($"`Tak já jdu.`", ephemeral: true);
        }
        catch (Exception exception)
        {
            await RespondAsync(exception.Message, ephemeral: true);
        }
    }
}
