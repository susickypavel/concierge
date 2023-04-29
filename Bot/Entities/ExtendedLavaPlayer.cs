using Discord;
using Victoria.Player;
using Victoria.WebSocket;

namespace Bot.Entities;

public class ExtendedLavaPlayer : LavaPlayer<ExtendedLavaTrack>
{
    public TrackQueue TrackQueue { get; }

    public ExtendedLavaPlayer(WebSocketClient socketClient, IVoiceChannel voiceChannel, ITextChannel textChannel) : base(socketClient, voiceChannel, textChannel)
    {
        TrackQueue = new();
    }
}
