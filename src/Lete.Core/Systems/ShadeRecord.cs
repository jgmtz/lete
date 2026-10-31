using Lete.Core.Conditions;
using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// Como terminou o último encontro com cada sombra (ARQUITETURA 4.1: resolvida, poupada, destruída, vista...), para a
/// condição <c>shade_state</c>. Fica o desfecho mais recente: uma sombra poupada ou de quem se fugiu pode ser libertada
/// depois.
/// </summary>
public sealed class ShadeRecord : IDisposable
{
    private readonly WorldState _state;
    private readonly List<IDisposable> _subscriptions = new();

    public ShadeRecord(WorldState state, EventBus events)
    {
        _state = state;
        _subscriptions.Add(events.Subscribe<ShadeFreed>(e => Set(e.ShadeId, ShadeState.Freed)));
        _subscriptions.Add(events.Subscribe<ShadeDestroyed>(e => Set(e.ShadeId, ShadeState.Destroyed)));
        _subscriptions.Add(events.Subscribe<ShadeAbsorbed>(e => Set(e.ShadeId, ShadeState.Absorbed)));
        _subscriptions.Add(events.Subscribe<ShadeWitnessed>(e => Set(e.ShadeId, ShadeState.Seen)));
        _subscriptions.Add(events.Subscribe<ShadeSpared>(e => Set(e.ShadeId, ShadeState.Spared)));
        _subscriptions.Add(events.Subscribe<ShadeFled>(e => Set(e.ShadeId, ShadeState.Fled)));
        _subscriptions.Add(events.Subscribe<ShadeYielded>(e => Set(e.ShadeId, ShadeState.Yielded)));
    }

    public ShadeState? StateOf(string shadeId) => _state.Shades.TryGetValue(shadeId, out var s) ? s : null;

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }

    private void Set(string shadeId, ShadeState state) => _state.Shades[shadeId] = state;
}
