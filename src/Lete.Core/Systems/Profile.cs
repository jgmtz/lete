using Lete.Core.Combat;
using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// O Perfil (GDD 2.5): como o jogador joga. Escuta os desfechos dos encontros e as Memórias usadas em combate
/// (decisões do João, 30/out/2026):
/// <list type="bullet">
/// <item>cada encontro soma 1 na via que o encerrou (Misturar não conta à parte); a derrota encerra sem via;</item>
/// <item>o tipo predominante vem das Memórias usadas em combate;</item>
/// <item>o recente é a janela dos últimos N encontros; os totais desde o começo ficam para o final;</item>
/// <item>empate, ou poucos encontros/usos na janela, é neutro: nenhuma via ou tipo predominante.</item>
/// </list>
/// Marca e Pesadas também fazem parte do Perfil, mas já estão no <see cref="WorldState"/>; quem lê o Perfil as lê de lá.
/// </summary>
public sealed class Profile : IDisposable
{
    private readonly ProfileState _state;
    private readonly ProfileRules _rules;
    private readonly List<RecollectionKind> _pendingKinds = new();
    private readonly List<IDisposable> _subscriptions = new();

    public Profile(WorldState state, EventBus events, ProfileRules rules)
    {
        _state = state.Profile;
        _rules = rules;
        _subscriptions.Add(events.Subscribe<ShadeFreed>(_ => End(Via.Free)));
        _subscriptions.Add(events.Subscribe<ShadeDestroyed>(_ => End(Via.Fight)));
        _subscriptions.Add(events.Subscribe<ShadeAbsorbed>(_ => End(Via.Absorb)));
        _subscriptions.Add(events.Subscribe<ShadeWitnessed>(_ => End(Via.Witness)));
        _subscriptions.Add(events.Subscribe<ShadeSpared>(_ => End(Via.Spare)));
        _subscriptions.Add(events.Subscribe<ShadeFled>(_ => End(Via.Flee)));
        _subscriptions.Add(events.Subscribe<ShadeYielded>(_ => End(Via.Yield)));
        _subscriptions.Add(events.Subscribe<PlayerDefeated>(_ => End(null)));
        _subscriptions.Add(events.Subscribe<MemoryUsed>(e => _pendingKinds.Add(e.Kind)));
    }

    public IReadOnlyDictionary<Via, int> ViaTotals => _state.ViaTotals;

    public IReadOnlyDictionary<RecollectionKind, int> KindTotals => _state.KindTotals;

    /// <summary>A via mais usada nos encontros recentes; nula se empatou ou se ainda são poucos.</summary>
    public Via? TopVia => Top(
        _state.Recent.Where(e => e.Via is not null).Select(e => e.Via!.Value),
        _rules.MinEncounters);

    /// <summary>O tipo das Memórias mais usadas nos encontros recentes; nulo se empatou ou se ainda são poucas.</summary>
    public RecollectionKind? TopKind => Top(_state.Recent.SelectMany(e => e.Kinds), _rules.MinMemoryUses);

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }

    private void End(Via? via)
    {
        if (via is { } counted)
        {
            _state.ViaTotals[counted] = _state.ViaTotals.GetValueOrDefault(counted) + 1;
        }

        foreach (var kind in _pendingKinds)
        {
            _state.KindTotals[kind] = _state.KindTotals.GetValueOrDefault(kind) + 1;
        }

        _state.Recent.Add(new ProfileEncounter(via, _pendingKinds.ToList()));
        _pendingKinds.Clear();
        if (_state.Recent.Count > _rules.RecentWindow)
        {
            _state.Recent.RemoveRange(0, _state.Recent.Count - _rules.RecentWindow);
        }
    }

    private static T? Top<T>(IEnumerable<T> items, int minimum) where T : struct, Enum
    {
        var counts = items.GroupBy(i => i).Select(g => (Item: g.Key, Count: g.Count())).ToList();
        if (counts.Sum(c => c.Count) < minimum || counts.Count == 0)
        {
            return null;
        }

        var most = counts.Max(c => c.Count);
        var leaders = counts.Where(c => c.Count == most).ToList();
        return leaders.Count == 1 ? leaders[0].Item : null;
    }
}

/// <summary>
/// Números do Perfil (<c>content/rules/perfil.json</c>): tamanho da janela recente (em encontros) e o mínimo de
/// encontros com via e de usos de Memória na janela para haver algo predominante.
/// </summary>
public sealed record ProfileRules(int RecentWindow, int MinEncounters, int MinMemoryUses)
{
    public IEnumerable<string> Validate()
    {
        if (RecentWindow < 1)
        {
            yield return "A janela recente do Perfil precisa de pelo menos 1 encontro.";
        }

        if (MinEncounters < 1 || MinEncounters > RecentWindow)
        {
            yield return "O mínimo de encontros precisa ficar entre 1 e o tamanho da janela.";
        }

        if (MinMemoryUses < 1)
        {
            yield return "O mínimo de usos de Memória precisa ser pelo menos 1.";
        }
    }
}
