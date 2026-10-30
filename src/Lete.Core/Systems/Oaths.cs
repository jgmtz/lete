using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// Juramentos do Estige (GDD 7.1): no máximo <see cref="OathRules.MaxActive"/> ao mesmo tempo, cada um jurado uma vez
/// só. Perjúrio (quebrar) é irreversível: a bênção some e publica <see cref="OathBroken"/> (a Marca sobe). Cumprido,
/// termina e libera o espaço (<see cref="OathKept"/>). Quebra quando o jogador encerra um encontro por uma via que o
/// juramento proíbe; a cada evento, confere o prazo: <c>kept_when</c> cumpre, <c>expires_when</c> (antes de cumprir) é
/// perjúrio. Uma condição de um sistema que ainda não existe é pulada e aparece em <see cref="ConditionsWaitingForSystems"/>.
/// </summary>
public sealed class Oaths : IDisposable
{
    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly OathRules _rules;
    private readonly Func<IConditionFacts> _facts;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly HashSet<string> _waiting = new(StringComparer.Ordinal);
    private bool _refreshing;

    public Oaths(WorldState state, EventBus events, IReadOnlyDictionary<string, Oath> oaths, OathRules rules, Func<IConditionFacts> facts)
    {
        _state = state;
        _events = events;
        Definitions = oaths;
        _rules = rules;
        _facts = facts;
        _subscriptions.Add(events.Subscribe<ShadeFreed>(_ => Act(Via.Free)));
        _subscriptions.Add(events.Subscribe<ShadeDestroyed>(_ => Act(Via.Fight)));
        _subscriptions.Add(events.Subscribe<ShadeAbsorbed>(_ => Act(Via.Absorb)));
        _subscriptions.Add(events.Subscribe<ShadeWitnessed>(_ => Act(Via.Witness)));
        _subscriptions.Add(events.Subscribe<ShadeSpared>(_ => Act(Via.Spare)));
        _subscriptions.Add(events.Subscribe<ShadeFled>(_ => Act(Via.Flee)));
        _subscriptions.Add(events.Subscribe<ShadeYielded>(_ => Act(Via.Yield)));
        _events.AnyPublished += OnAnyEvent;
    }

    public IReadOnlyDictionary<string, Oath> Definitions { get; }

    /// <summary>Condições (<c>juramento.campo</c>) que dependem de um sistema que ainda não existe.</summary>
    public IReadOnlyCollection<string> ConditionsWaitingForSystems => _waiting;

    public IEnumerable<Oath> Active => _state.Oaths
        .Where(p => p.Value == OathStatus.Active && Definitions.ContainsKey(p.Key))
        .Select(p => Definitions[p.Key]);

    public OathStatus? StatusOf(string id) => _state.Oaths.TryGetValue(id, out var status) ? status : null;

    /// <summary>Bênçãos e calcanhares de todos os juramentos ativos, somados.</summary>
    public OathEffects Effects => Active.Aggregate(OathEffects.None, (sum, o) => sum.Plus(o.Blessing).Plus(o.Heel));

    /// <summary>Os juramentos ativos que esta via quebraria (para avisar na opção, antes do ato).</summary>
    public IReadOnlyList<Oath> WouldBreak(Via via) => Active.Where(o => o.BreaksOn.Contains(via)).ToList();

    /// <summary>Dá para jurar: existe, nunca foi jurado e ainda cabe.</summary>
    public bool CanSwear(string id) =>
        Definitions.ContainsKey(id) && !_state.Oaths.ContainsKey(id) && Active.Count() < _rules.MaxActive;

    public bool Swear(string id)
    {
        if (!CanSwear(id))
        {
            return false;
        }

        _state.Oaths[id] = OathStatus.Active;
        _events.Publish(new OathSworn(id));
        return true;
    }

    /// <summary>Perjúrio por um ato que não é via (a cena chama). Falso se o juramento não estava ativo.</summary>
    public bool Break(string id)
    {
        if (StatusOf(id) != OathStatus.Active)
        {
            return false;
        }

        _state.Oaths[id] = OathStatus.Broken;
        _events.Publish(new OathBroken(id));
        return true;
    }

    /// <summary>Confere os prazos (os eventos já chamam; chame depois de mudar o estado sem evento).</summary>
    public void Refresh()
    {
        if (_refreshing)
        {
            return;
        }

        _refreshing = true;
        try
        {
            var facts = _facts();
            foreach (var oath in Active.ToList())
            {
                if (Passes(oath, "kept_when", oath.KeptWhen, facts))
                {
                    _state.Oaths[oath.Id] = OathStatus.Kept;
                    _events.Publish(new OathKept(oath.Id));
                }
                else if (Passes(oath, "expires_when", oath.ExpiresWhen, facts))
                {
                    Break(oath.Id);
                }
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    public void Dispose()
    {
        _events.AnyPublished -= OnAnyEvent;
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }

    private void OnAnyEvent(object _) => Refresh();

    private void Act(Via via)
    {
        foreach (var oath in WouldBreak(via))
        {
            Break(oath.Id);
        }
    }

    private bool Passes(Oath oath, string field, Condition? condition, IConditionFacts facts)
    {
        if (condition is null)
        {
            return false;
        }

        try
        {
            return ConditionEvaluator.Passes(condition, facts);
        }
        catch (NotSupportedException)
        {
            _waiting.Add($"{oath.Id}.{field}");
            return false;
        }
    }
}
