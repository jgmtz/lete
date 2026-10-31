using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// Fragmentos de origem (<c>docs/PROTAGONISTA_SOMBRA.md</c> 4): pedaços da vida passada, só história e nunca poder.
/// Decisão do João (30/out/2026): um por pessoa ligada à origem (3 companheiros e 2 espíritos nomeados), dado na
/// primeira vez de qualquer gatilho: conversar (uma conversa com a sombra, um comentário respondido, a conversa da
/// Trívia), libertar a sombra ou viajar junto (escolher o companheiro). O texto vai para o Diário.
/// </summary>
public sealed class OriginFragments : IDisposable
{
    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly IReadOnlyDictionary<string, Origin> _origins;
    private readonly Func<string, string?> _dialoguePerson;
    private readonly List<IDisposable> _subscriptions = new();

    /// <param name="dialoguePerson">Com quem é a conversa de um id de diálogo (a sombra do arquivo), ou nulo.</param>
    public OriginFragments(WorldState state, EventBus events, IEnumerable<Origin> origins, Func<string, string?> dialoguePerson)
    {
        _state = state;
        _events = events;
        _origins = origins.ToDictionary(o => o.Id, StringComparer.Ordinal);
        _dialoguePerson = dialoguePerson;
        _subscriptions.Add(events.Subscribe<DialogueEnded>(e => Met(_dialoguePerson(e.DialogueId))));
        _subscriptions.Add(events.Subscribe<CompanionCommented>(e => Met(e.CompanionId)));
        _subscriptions.Add(events.Subscribe<CompanionTalked>(e => Met(e.CompanionId)));
        _subscriptions.Add(events.Subscribe<CompanionChosen>(e => Met(e.CompanionId)));
        _subscriptions.Add(events.Subscribe<ShadeFreed>(e => Met(e.ShadeId)));
    }

    public IReadOnlyList<string> Gained => _state.OriginFragments;

    /// <summary>O fragmento da origem escolhida ligado a <paramref name="personId"/>, se houver.</summary>
    public OriginFragment? FragmentWith(string personId) =>
        _state.OriginId is { } origin && _origins.TryGetValue(origin, out var o)
            ? o.Fragments.FirstOrDefault(f => f.With == personId)
            : null;

    /// <returns><c>true</c> se o fragmento era novo.</returns>
    public bool Gain(string fragmentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fragmentId);
        if (_state.OriginFragments.Contains(fragmentId))
        {
            return false;
        }

        _state.OriginFragments.Add(fragmentId);
        var textKey = _origins.Values.SelectMany(o => o.Fragments).FirstOrDefault(f => f.Id == fragmentId)?.TextKey;
        _events.Publish(new OriginFragmentGained(fragmentId, textKey));
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

    private void Met(string? personId)
    {
        if (personId is not null && FragmentWith(personId) is { } fragment)
        {
            Gain(fragment.Id);
        }
    }
}
