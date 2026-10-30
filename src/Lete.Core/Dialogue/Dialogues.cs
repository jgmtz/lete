using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Dialogue;

/// <summary>
/// As conversas do conteúdo. Escuta as oferendas (dar ou partilhar): a oferenda certa para a sombra de uma conversa a
/// deixa desperta até o fim da próxima conversa com ela. Abre uma conversa com <see cref="Open"/>.
/// </summary>
public sealed class Dialogues : IDisposable
{
    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly WorldFacts _facts;
    private readonly IDialogueEffects _effects;
    private readonly Func<string, RecollectionKind?> _kindOf;
    private readonly List<IDisposable> _subscriptions = new();

    public Dialogues(
        WorldState state, EventBus events, IReadOnlyDictionary<string, DialogueGraph> graphs, WorldFacts facts,
        IDialogueEffects effects, Func<string, RecollectionKind?> kindOf)
    {
        _state = state;
        _events = events;
        Graphs = graphs;
        _facts = facts;
        _effects = effects;
        _kindOf = kindOf;
        _subscriptions.Add(events.Subscribe<OfferingGiven>(e => Offered(e.ShadeId, e.Kind)));
        _subscriptions.Add(events.Subscribe<OfferingShared>(e => Offered(e.ShadeId, e.Kind)));
    }

    public IReadOnlyDictionary<string, DialogueGraph> Graphs { get; }

    /// <summary>Começa a conversa <paramref name="dialogueId"/> (já no nó de entrada).</summary>
    public DialogueRunner Open(string dialogueId)
    {
        var runner = new DialogueRunner(
            Graphs[dialogueId], _state, _events, _facts,
            lucidity => _facts.ShadeLucidity = lucidity is { } l ? DialogueData.LucidityId(l) : null,
            _effects, _kindOf);
        runner.Start();
        return runner;
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }

    private void Offered(string shadeId, string kind)
    {
        if (Graphs.Values.Any(g => g.ShadeId == shadeId && g.Lucidity is { } l && (l.OfferingKind is null || l.OfferingKind == kind)))
        {
            _state.Dialogue.Awakened.Add(shadeId);
        }
    }
}
