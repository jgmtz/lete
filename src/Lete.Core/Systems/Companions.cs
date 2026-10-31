using Lete.Core.Combat;
using Lete.Core.Dialogue;
using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// O companheiro da partida (GDD 4.10, HISTORIA 5): a escolha é uma vez só e irreversível (sobe a Maré: marco
/// <see cref="ChosenMilestone"/>). O vínculo só cresce: partilhar uma oferenda com ele (<see cref="OfferingShared"/> com o
/// id dele), terminar uma luta juntos, conversar na Trívia (uma vez por degrau da Maré) e responder aos comentários dele
/// (discordar aprofunda mais que concordar; ignorar não muda nada). Com vínculo alto, o apoio vem mais seguido.
/// </summary>
public sealed class Companions : IDisposable
{
    /// <summary>Marco da Maré da escolha do companheiro (GDD 3.5, marco 2).</summary>
    public const string ChosenMilestone = "companheiro";

    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly CompanionRules _rules;
    private readonly TideTracker _tide;
    private readonly List<IDisposable> _subscriptions = new();

    public Companions(WorldState state, EventBus events, IReadOnlyDictionary<string, Companion> companions, CompanionRules rules, TideTracker tide)
    {
        _state = state;
        _events = events;
        Definitions = companions;
        _rules = rules;
        _tide = tide;
        _subscriptions.Add(events.Subscribe<OfferingShared>(e => Gain(e.ShadeId == _state.CompanionId, BondSource.OfferingShared)));
        _subscriptions.Add(events.Subscribe<CompanionCommented>(e => Gain(
            e.CompanionId == _state.CompanionId,
            e.Response switch
            {
                CommentResponse.Agree => BondSource.CommentAgreed,
                CommentResponse.Disagree => BondSource.CommentDisagreed,
                _ => null,
            })));
        foreach (var subscription in new[]
        {
            events.Subscribe<ShadeFreed>(_ => Fought()),
            events.Subscribe<ShadeDestroyed>(_ => Fought()),
            events.Subscribe<ShadeAbsorbed>(_ => Fought()),
            events.Subscribe<ShadeWitnessed>(_ => Fought()),
            events.Subscribe<ShadeSpared>(_ => Fought()),
            events.Subscribe<ShadeFled>(_ => Fought()),
            events.Subscribe<ShadeYielded>(_ => Fought()),
        })
        {
            _subscriptions.Add(subscription);
        }
    }

    public IReadOnlyDictionary<string, Companion> Definitions { get; }

    public CompanionRules Rules => _rules;

    /// <summary>O companheiro escolhido; nulo antes da escolha (ou com um id que o conteúdo não tem).</summary>
    public Companion? Current => _state.CompanionId is { } id && Definitions.TryGetValue(id, out var companion) ? companion : null;

    public int Bond => _state.CompanionBond;

    /// <summary>De quantos em quantos turnos o companheiro apoia, pelo vínculo.</summary>
    public int IntervalTurns => Bond >= _rules.FastBond ? _rules.FastIntervalTurns : _rules.IntervalTurns;

    /// <summary>
    /// O apoio na luta do companheiro <paramref name="id"/> (o escolhido, ou outro numa luta de teste), com o intervalo
    /// do vínculo atual.
    /// </summary>
    public CompanionSupport Support(string id)
    {
        var companion = Definitions.TryGetValue(id, out var found) ? found : throw new ArgumentException($"Companheiro '{id}' não existe.", nameof(id));
        return new CompanionSupport(companion.Id, companion.SupportId, companion.Effect, IntervalTurns, companion.Power, _rules.ReluctantFactor);
    }

    public Approval ApprovalOf(string id, Via via) =>
        Definitions.TryGetValue(id, out var companion) ? companion.ApprovalOf(via) : Approval.Tolerate;

    /// <summary>Escolhe o companheiro: uma vez por partida. Falso se já há um ou se o id não existe.</summary>
    public bool Choose(string id)
    {
        if (_state.CompanionId is not null || !Definitions.ContainsKey(id))
        {
            return false;
        }

        _state.CompanionId = id;
        _events.Publish(new CompanionChosen(id));
        _tide.Milestone(ChosenMilestone);
        return true;
    }

    /// <summary>Dá para conversar com ele na Trívia (uma vez por degrau da Maré).</summary>
    public bool CanTalk => Current is not null && (_state.CompanionTalkStep is not { } step || step < _tide.Step);

    /// <summary>Conversa na Trívia: devolve a fala (as conversas seguem em ordem; a última se repete) e aprofunda o vínculo.</summary>
    public string? Talk()
    {
        if (!CanTalk || Current is not { } companion)
        {
            return null;
        }

        var line = companion.TalkKeys[Math.Min(_state.CompanionTalks, companion.TalkKeys.Count - 1)];
        _state.CompanionTalks++;
        _state.CompanionTalkStep = _tide.Step;
        _events.Publish(new CompanionTalked(companion.Id));
        Gain(true, BondSource.TriviaTalk);
        return line;
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }

    private void Fought() => Gain(true, BondSource.Battle);

    private void Gain(bool applies, BondSource? source)
    {
        if (!applies || source is not { } s || _state.CompanionId is not { } id)
        {
            return;
        }

        var bond = Math.Min(_rules.BondMax, _state.CompanionBond + _rules.Gain(s));
        if (bond == _state.CompanionBond)
        {
            return;
        }

        _state.CompanionBond = bond;
        _events.Publish(new CompanionBondChanged(id, bond));
    }
}
