using Discord;
using Victoria.Player;

namespace Bot.Entities;

public class ExtendedLavaTrack : LavaTrack
{
    public IUser QueuedBy { get; private set; }

    public ExtendedLavaTrack(LavaTrack track, IUser queuedBy) : base(track)
    {
        QueuedBy = queuedBy;
    }
}
