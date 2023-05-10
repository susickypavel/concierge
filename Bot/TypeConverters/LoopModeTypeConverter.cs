using Bot.Enums;
using Discord;
using Discord.Interactions;

namespace Bot.TypeConverters;

public class LoopModeTypeConverter : TypeConverter<LoopMode>
{
    public override ApplicationCommandOptionType GetDiscordType() => ApplicationCommandOptionType.String;

    public override Task<TypeConverterResult> ReadAsync(IInteractionContext context,
        IApplicationCommandInteractionDataOption option, IServiceProvider services)
    {
        if (Enum.TryParse(option.Value.ToString(), out LoopMode mode))
        {
            return Task.FromResult(TypeConverterResult.FromSuccess(mode));
        }

        return Task.FromResult(TypeConverterResult.FromError(InteractionCommandError.ConvertFailed,
            $"Parameter '{option.Value}' není validní cyklus."));
    }

    public override void Write(ApplicationCommandOptionProperties properties, IParameterInfo parameter)
    {
        var names = Enum.GetNames(typeof(LoopMode));

        var choices = names
            .Select(name => new ApplicationCommandOptionChoiceProperties {Name = name, Value = name});

        properties.Choices = choices.ToList();
    }
}
