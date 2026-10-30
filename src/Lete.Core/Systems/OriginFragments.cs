using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// Fragmentos de origem (<c>docs/PROTAGONISTA_SOMBRA.md</c> 4): pedaços da vida passada ganhos ao cruzar com quem
/// é ligado à origem. Só história, nunca poder. Quais eventos dão fragmento ainda está em aberto (PENDÊNCIAS 42).
/// </summary>
public sealed class OriginFragments
{
    private readonly WorldState _state;
    private readonly EventBus _events;

    public OriginFragments(WorldState state, EventBus events)
    {
        _state = state;
        _events = events;
    }

    public IReadOnlyList<string> Gained => _state.OriginFragments;

    /// <returns><c>true</c> se o fragmento era novo.</returns>
    public bool Gain(string fragmentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fragmentId);
        if (_state.OriginFragments.Contains(fragmentId))
        {
            return false;
        }

        _state.OriginFragments.Add(fragmentId);
        _events.Publish(new OriginFragmentGained(fragmentId));
        return true;
    }
}
