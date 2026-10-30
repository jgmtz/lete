using Lete.Core.Conditions;
using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// O Caminho (GDD 2.4): se o jogador sabe como sair. A cada evento, confere as chaves que ainda faltam; a primeira
/// forma cuja condição vale dá a chave, que fica para sempre (mesmo que a condição deixe de valer depois), guarda a
/// forma usada e publica <see cref="PathKeyObtained"/> (o Diário escreve a frase). Atravessar não gasta a chave: a
/// passagem fica aberta (decisões do João, 30/out/2026).
/// <para>
/// Uma forma que usa um sistema que ainda não existe (por exemplo, juramentos) é pulada, sem dar a chave; ela
/// aparece em <see cref="WaysWaitingForSystems"/>.
/// </para>
/// </summary>
public sealed class PathKeys : IDisposable
{
    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly Func<IConditionFacts> _facts;
    private readonly HashSet<string> _waiting = new(StringComparer.Ordinal);
    private bool _refreshing;

    public PathKeys(WorldState state, EventBus events, IReadOnlyDictionary<string, PathKey> keys, Func<IConditionFacts> facts)
    {
        _state = state;
        _events = events;
        Keys = keys;
        _facts = facts;
        _events.AnyPublished += OnAnyEvent;
    }

    public IReadOnlyDictionary<string, PathKey> Keys { get; }

    /// <summary>Formas (<c>chave.forma</c>) que dependem de um sistema que ainda não existe.</summary>
    public IReadOnlyCollection<string> WaysWaitingForSystems => _waiting;

    public bool Has(string keyId) => _state.PathKeys.ContainsKey(keyId);

    /// <summary>A forma que deu a chave, ou nula se o jogador ainda não a tem.</summary>
    public string? WayOf(string keyId) => _state.PathKeys.GetValueOrDefault(keyId);

    /// <summary>Confere as chaves que faltam (os eventos já chamam; chame depois de mudar o estado sem evento).</summary>
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
            foreach (var key in Keys.Values.Where(k => !Has(k.Id)).ToList())
            {
                if (key.Ways.FirstOrDefault(w => Passes(key, w, facts)) is { } way)
                {
                    _state.PathKeys[key.Id] = way.Id;
                    _events.Publish(new PathKeyObtained(key.Id, way.Id, key.JournalKey));
                }
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    public void Dispose() => _events.AnyPublished -= OnAnyEvent;

    private void OnAnyEvent(object _) => Refresh();

    private bool Passes(PathKey key, PathWay way, IConditionFacts facts)
    {
        try
        {
            return ConditionEvaluator.Passes(way.Condition, facts);
        }
        catch (NotSupportedException)
        {
            _waiting.Add($"{key.Id}.{way.Id}");
            return false;
        }
    }
}
