using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;

namespace Bot.TypeConverters;

public enum MusicPlatform
{
    YouTube,
    Spotify,
    Other
}

public class MusicPlatformUrl : TypeConverter<(MusicPlatform, Uri)>
{
    private readonly Dictionary<Regex, MusicPlatform> _platforms = new()
    {
        {new Regex(@"(^|\.)youtube\.com$"), MusicPlatform.YouTube},
        {new Regex(@"(^|\.)spotify\.com$"), MusicPlatform.Spotify},
    };

    private readonly Dictionary<MusicPlatform, IEnumerable<string>> _trackingParameters = new()
    {
        {MusicPlatform.Spotify, new[] {"si"}}
    };

    private MusicPlatform GetPlatform(string url)
    {
        return _platforms.Keys
            .Where(key => key.IsMatch(url))
            .Select(key => _platforms[key])
            .FirstOrDefault(MusicPlatform.Other);
    }

    private string StripTrackers(Uri url, MusicPlatform platform)
    {
        if (!_trackingParameters.ContainsKey(platform))
        {
            return url.Query;
        }
    
        var updatedQueryParams = url.Query
            .TrimStart('?')
            .Split('&')
            .Where(p => !_trackingParameters[platform].Contains(p.Split('=')[0]));

        return string.Join("&", updatedQueryParams);
    }

    public override Task<TypeConverterResult> ReadAsync(IInteractionContext context,
        IApplicationCommandInteractionDataOption option, IServiceProvider services)
    {
        if (!Uri.TryCreate(option.Value.ToString(), UriKind.Absolute, out var result))
        {
            return Task.FromResult(TypeConverterResult.FromError(InteractionCommandError.ConvertFailed,
                $"Parametr `{option.Value}` není validní URL"));
        }

        var platform = GetPlatform(result.Host);

        var uriBuilder = new UriBuilder(result)
        {
            Query = StripTrackers(result, platform)
        };

        return Task.FromResult(TypeConverterResult.FromSuccess((platform, uriBuilder.Uri)));
    }

    public override ApplicationCommandOptionType GetDiscordType()
        => ApplicationCommandOptionType.String;
}