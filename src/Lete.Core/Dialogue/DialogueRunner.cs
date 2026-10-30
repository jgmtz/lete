using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Dialogue;

/// <summary>Quem aplica os efeitos dos nós (o jogo liga cada um ao sistema certo: Diário, Mosaico, juramentos...).</summary>
public interface IDialogueEffects
{
    void Apply(DialogueEffect effect);
}

/// <summary>De onde vem o rótulo entre colchetes de uma opção (GDD 5.2: <c>[Artesão]</c>, <c>[Memória de Ofício]</c>).</summary>
public enum DialogueTagKind
{
    Origin,
    Memory,
    Mark,
    Offering,
}

/// <summary>Rótulo de uma opção: o tipo e o valor (id da origem, tipo de Lembrança).</summary>
public sealed record DialogueTag(DialogueTagKind Kind, string? Value);

/// <summary>
/// Uma opção como o jogador a vê agora: disponível, ou em cinza com a dica; <see cref="Seen"/> se já foi escolhida
/// numa conversa anterior (aparece esmaecida, mas pode repetir; decisão do João, 30/out/2026).
/// </summary>
public sealed record OptionView(DialogueOption Option, bool Available, bool Seen, DialogueTag? Tag);

/// <summary>
/// Conduz uma conversa: escolhe o nó de entrada, monta as opções, aplica os efeitos dos nós e o comentário do
/// companheiro (uma vez por partida). "Calar" está sempre disponível: o nó que não tiver uma opção de Calar ganha uma
/// que encerra a conversa. A lucidez da sombra vale durante a conversa (condição <c>shade_lucidity</c>); no fim, o
/// despertar da oferenda acaba.
/// </summary>
public sealed class DialogueRunner
{
    /// <summary>Texto da opção de Calar que a conversa ganha quando o nó não tem uma.</summary>
    public const string SilenceKey = "dialogo.calar";

    private readonly DialogueGraph _graph;
    private readonly DialogueState _state;
    private readonly EventBus _events;
    private readonly IConditionFacts _facts;
    private readonly Action<ShadeLucidity?> _setLucidity;
    private readonly IDialogueEffects _effects;
    private readonly Func<string, RecollectionKind?> _kindOf;
    private readonly string? _companion;

    public DialogueRunner(
        DialogueGraph graph, WorldState state, EventBus events, IConditionFacts facts, Action<ShadeLucidity?> setLucidity,
        IDialogueEffects effects, Func<string, RecollectionKind?> kindOf)
    {
        _graph = graph;
        _state = state.Dialogue;
        _events = events;
        _facts = facts;
        _setLucidity = setLucidity;
        _effects = effects;
        _kindOf = kindOf;
        _companion = state.CompanionId;
    }

    public DialogueGraph Graph => _graph;

    public DialogueNode? Current { get; private set; }

    public bool IsOver => Current is null;

    /// <summary>Lucidez da sombra nesta conversa (nula fora do modo sombra).</summary>
    public ShadeLucidity? Lucidity { get; private set; }

    /// <summary>As opções do nó atual, já com as bloqueadas que aparecem em cinza e o Calar garantido.</summary>
    public IReadOnlyList<OptionView> Options { get; private set; } = Array.Empty<OptionView>();

    /// <summary>A fala do companheiro neste nó, se ele ainda não comentou esta cena.</summary>
    public string? PendingCommentKey =>
        Current?.Comment is { } comment && _companion is not null && !_state.CommentsSeen.Contains(comment.Id)
            && comment.LineKeys.TryGetValue(_companion, out var key)
            ? key
            : null;

    public void Start()
    {
        Lucidity = _graph.Lucidity is { } rules ? LucidityOf(rules) : null;
        _setLucidity(Lucidity);
        var entry = _graph.Start.Select(id => _graph.Nodes[id]).FirstOrDefault(n => ConditionEvaluator.Passes(n.When, _facts));
        Go(entry?.Id);
    }

    public void Choose(OptionView view)
    {
        if (Current is not { } node || !Options.Contains(view) || !view.Available)
        {
            throw new InvalidOperationException("Opção indisponível ou fora da conversa.");
        }

        _state.Chosen.Add(SeenKey(view.Option));
        _events.Publish(new DialogueChoiceMade(_graph.Id, node.Id, DialogueData.PostureId(view.Option.Posture)));
        Go(view.Option.Next);
    }

    /// <summary>Nó sem opções: segue para o próximo (ou termina).</summary>
    public void Continue()
    {
        if (Current is not { Options.Count: 0 } node)
        {
            throw new InvalidOperationException("Este nó espera uma escolha.");
        }

        Go(node.Next);
    }

    public void AnswerComment(CommentResponse response)
    {
        if (PendingCommentKey is null)
        {
            throw new InvalidOperationException("Não há comentário do companheiro pendente.");
        }

        var comment = Current!.Comment!;
        _state.CommentsSeen.Add(comment.Id);
        _events.Publish(new CompanionCommented(comment.Id, _companion!, response));
    }

    private ShadeLucidity LucidityOf(LucidityRules rules)
    {
        if (_graph.ShadeId is { } shade && _state.Awakened.Contains(shade))
        {
            return ShadeLucidity.Awake;
        }

        // Com o estado base, a condição shade_lucidity ainda não vale: as condições de subida não podem depender dela.
        _setLucidity(rules.Base);
        return rules.Raises.FirstOrDefault(r => r.To > rules.Base && ConditionEvaluator.Passes(r.When, _facts))?.To ?? rules.Base;
    }

    private void Go(string? nodeId)
    {
        if (nodeId is null)
        {
            End();
            return;
        }

        Current = _graph.Nodes[nodeId];
        foreach (var effect in Current.Effects)
        {
            _effects.Apply(effect);
        }

        Options = Current.Options.Count == 0 ? Array.Empty<OptionView>() : BuildOptions(Current);
    }

    private IReadOnlyList<OptionView> BuildOptions(DialogueNode node)
    {
        var views = new List<OptionView>();
        foreach (var option in node.Options)
        {
            var outcome = option.Requires is null ? ConditionOutcome.Pass : ConditionEvaluator.Evaluate(option.Requires, _facts).Outcome;
            if (outcome == ConditionOutcome.Pass || option.ShowBlocked || outcome == ConditionOutcome.NearMiss)
            {
                views.Add(new OptionView(option, outcome == ConditionOutcome.Pass, _state.Chosen.Contains(SeenKey(option)), TagOf(option.Requires)));
            }
        }

        if (!views.Any(v => v.Option.Posture == Posture.Silence && v.Available))
        {
            views.Add(new OptionView(new DialogueOption(Posture.Silence, SilenceKey, null, null, null, false), true, false, null));
        }

        return views;
    }

    private void End()
    {
        Current = null;
        Options = Array.Empty<OptionView>();
        if (_graph.ShadeId is { } shade)
        {
            _state.Awakened.Remove(shade);
        }

        _setLucidity(null);
        _events.Publish(new DialogueEnded(_graph.Id));
    }

    private string SeenKey(DialogueOption option) => $"{_graph.Id}/{option.TextKey}";

    /// <summary>O primeiro predicado que dá rótulo: origem, Lembrança (pelo tipo), Marca ou oferenda partilhada.</summary>
    private DialogueTag? TagOf(Condition? requires) => requires switch
    {
        null => null,
        OriginIs p => new DialogueTag(DialogueTagKind.Origin, p.Origin),
        RecollectionAt p when _kindOf(p.Id) is { } kind => new DialogueTag(DialogueTagKind.Memory, ConditionParser.SnakeCase(kind)),
        MarkBandIn => new DialogueTag(DialogueTagKind.Mark, null),
        OfferingWasShared p => new DialogueTag(DialogueTagKind.Offering, p.Kind),
        AllOf all => all.Items.Select(TagOf).FirstOrDefault(t => t is not null),
        AnyOf any => any.Items.Select(TagOf).FirstOrDefault(t => t is not null),
        _ => null,
    };
}
