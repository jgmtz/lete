using Lete.Core.Events;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.World;

/// <summary>
/// Uma Trívia de Hécate (<c>content/trivias/&lt;id&gt;.json</c>, GDD 3.6 e 3.7): em que região fica e as até três
/// tochas (as Trívias a que ela liga). A de Asfódelos é <see cref="Hub"/>: liga a todas as já acesas.
/// </summary>
public sealed record Trivia(string Id, string RegionId, string NameKey, IReadOnlyList<string> Torches, bool Hub);

/// <summary>
/// A rede de viagem das Trívias (GDD 3.6): acender uma (chegando a pé) a liga à rede; dela se viaja só para as das
/// tochas que já estão acesas (ou para todas, saindo da grande Trívia de Asfódelos). Cada viagem faz a Maré avançar um
/// pouco (<c>travel_step</c> da Maré). O lugar para onde o jogador vai é a cena que resolve.
/// </summary>
public sealed class Trivias
{
    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly TideTracker _tide;

    public Trivias(WorldState state, EventBus events, IReadOnlyDictionary<string, Trivia> trivias, TideTracker tide)
    {
        _state = state;
        _events = events;
        Definitions = trivias;
        _tide = tide;
    }

    public IReadOnlyDictionary<string, Trivia> Definitions { get; }

    public bool IsLit(string triviaId) => _state.LitTrivias.Contains(triviaId);

    /// <summary>Acende a Trívia (a pé). Falso se já estava acesa.</summary>
    public bool Light(string triviaId)
    {
        Get(triviaId);
        if (!_state.LitTrivias.Add(triviaId))
        {
            return false;
        }

        _events.Publish(new TriviaLit(triviaId));
        return true;
    }

    /// <summary>Para onde dá para ir daqui: as tochas acesas (ou todas as acesas, da grande Trívia).</summary>
    public IReadOnlyList<Trivia> Destinations(string fromId)
    {
        var from = Get(fromId);
        if (!IsLit(fromId))
        {
            return Array.Empty<Trivia>();
        }

        var targets = from.Hub ? Definitions.Keys.Where(id => id != fromId) : from.Torches;
        return targets.Where(IsLit).Select(Get).OrderBy(t => t.Id, StringComparer.Ordinal).ToList();
    }

    /// <summary>Viaja de uma Trívia a outra pela tocha: a Maré sobe um pouco.</summary>
    public void Travel(string fromId, string toId)
    {
        if (Destinations(fromId).All(t => t.Id != toId))
        {
            throw new InvalidOperationException($"Não há tocha acesa de '{fromId}' para '{toId}'.");
        }

        _tide.Travel();
        _events.Publish(new FastTravelUsed(fromId, toId));
    }

    private Trivia Get(string id) => Definitions.TryGetValue(id, out var trivia)
        ? trivia
        : throw new ArgumentException($"Trívia '{id}' não existe.", nameof(id));
}
