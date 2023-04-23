using Discord;
using Discord.Interactions;

namespace Bot.TypeConverters;

public class UrlConverter : TypeConverter<Uri>
{
    public override Task<TypeConverterResult> ReadAsync(IInteractionContext context,
        IApplicationCommandInteractionDataOption option, IServiceProvider services)
    {
        if (Uri.TryCreate(option.Value.ToString(), UriKind.Absolute, out var result))
        {
            return Task.FromResult(TypeConverterResult.FromSuccess(result));
        }

        return Task.FromResult(TypeConverterResult.FromError(InteractionCommandError.ConvertFailed,
            $"Value {option.Value} cannot be converted to {nameof(UriBuilder)}"));
    }

    public override ApplicationCommandOptionType GetDiscordType()
        => ApplicationCommandOptionType.String;
}