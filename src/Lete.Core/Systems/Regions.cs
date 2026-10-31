using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// A região onde o protagonista está e as já visitadas (condição <c>region_visited</c>). Trocar de região publica
/// <see cref="RegionEntered"/>; quem conduz a cena salva o jogo nessa hora (salvar só em Trívias e ao trocar de região).
/// </summary>
public sealed class Regions
{
    private readonly WorldState _state;
    private readonly EventBus _events;

    public Regions(WorldState state, EventBus events)
    {
        _state = state;
        _events = events;
    }

    public string? Current => _state.RegionId;

    public bool WasVisited(string regionId) => _state.VisitedRegions.Contains(regionId);

    /// <summary>Entra na região; verdadeiro se mudou (é a hora de salvar). Voltar a uma visitada também é troca.</summary>
    public bool Enter(string regionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regionId);
        _state.VisitedRegions.Add(regionId);
        if (_state.RegionId == regionId)
        {
            return false;
        }

        _state.RegionId = regionId;
        _events.Publish(new RegionEntered(regionId));
        return true;
    }
}
