using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Events;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Dialogue;

/// <summary>
/// As conversas do conteúdo. Escuta as oferendas (dar ou partilhar, na luta ou na conversa): a oferenda certa para a
/// sombra de uma conversa, ou o sangue, a deixa desperta até o fim da próxima conversa com ela. Abre uma conversa com
/// <see cref="Open"/>; dentro dela, <see cref="Offer"/> dá ou partilha.
/// </summary>
public sealed class Dialogues : IDisposable
{
    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly WorldFacts _facts;
    private readonly IDialogueEffects _effects;
    private readonly Func<string, RecollectionKind?> _kindOf;
    private readonly Offerings _offerings;
    private readonly List<IDisposable> _subscriptions = new();

    public Dialogues(
        WorldState state, EventBus events, IReadOnlyDictionary<string, DialogueGraph> graphs, WorldFacts facts,
        IDialogueEffects effects, Func<string, RecollectionKind?> kindOf, Offerings offerings)
    {
        _state = state;
        _events = events;
        Graphs = graphs;
        _facts = facts;
        _effects = effects;
        _kindOf = kindOf;
        _offerings = offerings;
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

    /// <summary>
    /// Dá ou partilha uma oferenda com a sombra da conversa (só no modo sombra). Verdadeiro se ela despertou; partilhar a
    /// oferenda certa também aplica os efeitos de partilhar. A errada se perde (como na luta).
    /// </summary>
    public bool Offer(string dialogueId, string kind, bool share)
    {
        var graph = Graphs[dialogueId];
        if (graph is not { ShadeId: { } shade, Lucidity: { } lucidity })
        {
            throw new InvalidOperationException($"A conversa '{dialogueId}' não é com uma sombra.");
        }

        var spent = share ? _offerings.Share(shade, kind) : _offerings.Give(shade, kind);
        if (!spent || !Awakens(lucidity, kind))
        {
            return false;
        }

        if (share)
        {
            foreach (var effect in lucidity.ShareEffects)
            {
                _effects.Apply(effect);
            }
        }

        return true;
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
        if (Graphs.Values.Any(g => g.ShadeId == shadeId && g.Lucidity is { } l && Awakens(l, kind)))
        {
            _state.Dialogue.Awakened.Add(shadeId);
        }
    }

    private bool Awakens(LucidityRules lucidity, string kind) =>
        lucidity.OfferingKind is null || lucidity.OfferingKind == kind || kind == _offerings.Rules.BloodKind;
}
