using Bot.Entities;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Victoria.Node;

namespace Bot.Handlers;

public class QueueAutocompleteHandler : AutocompleteHandler
{
    public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var lavaNode = services.GetRequiredService<LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack>>();

        if (!lavaNode.TryGetPlayer(context.Guild, out var player))
        {
            return Task.FromResult(AutocompletionResult.FromError(InteractionCommandError.Exception, "Oops."));
        }

        var value = autocompleteInteraction.Data.Current.Value.ToString();

        var autocompletion = player.TrackQueue
            .Select((track, i) => new AutocompleteResult($"{i + 1}. {track.Title}", i))
            .Where(autocomplete => value == null || autocomplete.Name.Contains(value));

        return Task.FromResult(AutocompletionResult.FromSuccess(autocompletion.Take(25)));
    }
}
