using Bot.Entities;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Victoria.Node;

namespace Bot.Handlers;

// TODO: Filter based on input

public class QueueAutocompleteHandler : AutocompleteHandler
{
    public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context,
        IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
    {
        var lavaNode = services.GetRequiredService<LavaNode<ExtendedLavaPlayer, ExtendedLavaTrack>>();

        if (lavaNode.TryGetPlayer(context.Guild, out var player))
        {
            var autocompletion = player.TrackQueue
                .Select((track, i) => new AutocompleteResult($"{i + 1}. {track.Title}", i));
            
            return AutocompletionResult.FromSuccess(autocompletion.Take(25));
        }

        return AutocompletionResult.FromError(InteractionCommandError.Exception, "Oops.");
    }
}
