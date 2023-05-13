using Bot.Enums;
using Discord;

namespace Bot;

public static class Constants
{
    public static readonly Dictionary<LoopMode, Emoji> LoopModeFlags = new()
    {
        {LoopMode.Single, Emoji.Parse("\uD83D\uDD02")},
        {LoopMode.All, Emoji.Parse(	"\uD83D\uDD01")},
        {LoopMode.Off, Emoji.Parse("\u274C")}
    };

    public const byte TrackTitleMaxLength = 60;
    public const byte TracksPerQueuePage = 10;
}
