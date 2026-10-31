using Lete.Core.Events;
using Lete.Core.State;
using Lete.Core.World;

namespace Lete.Core.Systems;

/// <summary>
/// Os encontros do mapa já terminados (no estado) e quais estão esperando o jogador: cada um aparece depois do anterior
/// (<see cref="Encounter.After"/>) e termina uma vez só (<see cref="EncounterFinished"/>).
/// </summary>
public sealed class Encounters
{
    private readonly WorldState _state;
    private readonly EventBus _events;

    public Encounters(WorldState state, EventBus events, IReadOnlyDictionary<string, Encounter> definitions)
    {
        _state = state;
        _events = events;
        Definitions = definitions;
    }

    public IReadOnlyDictionary<string, Encounter> Definitions { get; }

    public bool IsFinished(string id) => _state.Encounters.Contains(id);

    /// <summary>A sombra está no lugar: o encontro ainda não terminou e o anterior (se houver) já.</summary>
    public bool IsAvailable(string id) =>
        Definitions.TryGetValue(id, out var encounter) && !IsFinished(id) && (encounter.After is null || IsFinished(encounter.After));

    /// <summary>Disponível e com o que ele pede na Bolsa (a oferenda): a luta pode começar.</summary>
    public bool CanStart(string id) =>
        IsAvailable(id)
        && (Definitions[id].NeedsOffering is not { } kind || _state.Inventory.Offerings.GetValueOrDefault(kind) > 0);

    /// <summary>Termina o encontro (lição cumprida ou luta acabada); falso se não estava disponível.</summary>
    public bool Finish(string id)
    {
        if (!IsAvailable(id))
        {
            return false;
        }

        _state.Encounters.Add(id);
        _events.Publish(new EncounterFinished(id));
        return true;
    }
}
