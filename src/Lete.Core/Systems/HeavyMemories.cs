using Lete.Core.Conditions;
using Lete.Core.Events;
using Lete.Core.Recollections;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// Memórias Pesadas (GDD 2.2, HISTORIA 1.2): seguem fila fixa (a seguinte só aflora depois da anterior) e a escolha,
/// Encarar ou Deixar, é uma vez só (irreversível, princípio 2). Encarar dá a Culpa (a Memória da Pesada, no Mosaico);
/// Deixar dá a <see cref="Lightness"/> dela. O aviso que vem antes de aflorar pode ser adiado ("agora não"): nada se
/// guarda, e o aviso volta na próxima vez que o jogador chegar ao lugar (decisão do João, 30/out/2026).
/// </summary>
public sealed class HeavyMemories
{
    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly IReadOnlyDictionary<string, Recollection> _byId;

    public HeavyMemories(WorldState state, EventBus events, IEnumerable<Recollection> recollections)
    {
        _state = state;
        _events = events;
        Queue = recollections.Where(r => r.Heavy is not null).OrderBy(r => r.Heavy!.Order).ToList();
        var orders = Queue.Select(r => r.Heavy!.Order).ToList();
        if (!orders.SequenceEqual(Enumerable.Range(1, orders.Count)))
        {
            throw new FormatException($"As Pesadas precisam da ordem 1, 2, 3... sem repetir nem pular (veio {string.Join(", ", orders)}).");
        }

        _byId = Queue.ToDictionary(r => r.Id, StringComparer.Ordinal);
    }

    /// <summary>As Pesadas na ordem da fila.</summary>
    public IReadOnlyList<Recollection> Queue { get; }

    /// <summary>A próxima a aflorar (a primeira ainda pendente), ou nula se todas foram decididas.</summary>
    public Recollection? Next => Queue.FirstOrDefault(r => !_state.HeavyMemories.ContainsKey(r.Id));

    public HeavyMemoryChoice? ChoiceFor(string memoryId) =>
        _state.HeavyMemories.TryGetValue(memoryId, out var choice) ? choice : null;

    /// <summary>A Pesada aflora aqui: é a próxima da fila e a condição dela vale agora.</summary>
    public bool CanSurface(string memoryId, IConditionFacts facts) =>
        Next is { } next && next.Id == memoryId && ConditionEvaluator.Passes(next.Heavy!.Gate, facts);

    /// <summary>Leveza somada das Pesadas deixadas.</summary>
    public Lightness Lightness => Queue
        .Where(r => ChoiceFor(r.Id) == HeavyMemoryChoice.Leave)
        .Aggregate(Lightness.None, (sum, r) => sum.Plus(r.Heavy!.Lightness));

    public void Choose(string memoryId, HeavyMemoryChoice choice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memoryId);
        if (_state.HeavyMemories.ContainsKey(memoryId))
        {
            throw new InvalidOperationException($"A Memória Pesada '{memoryId}' já foi decidida (não tem volta).");
        }

        if (!_byId.ContainsKey(memoryId))
        {
            throw new ArgumentException($"Memória Pesada '{memoryId}' não existe.", nameof(memoryId));
        }

        if (Next?.Id != memoryId)
        {
            throw new InvalidOperationException($"A Memória Pesada '{memoryId}' não é a próxima da fila ('{Next?.Id}').");
        }

        _state.HeavyMemories[memoryId] = choice;
        _events.Publish(new HeavyMemoryChosen(memoryId, choice));
    }
}
