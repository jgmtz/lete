using Lete.Core.Conditions;
using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Items;

/// <summary>
/// Itens únicos (GDD 6): adquirir, equipar (3 espaços + 2 ex-votos; troca a qualquer hora fora da luta), evoluir e
/// reunir conjuntos. A cada evento, confere a evolução: o próximo estágio de um item que o jogador tem chega quando a
/// condição dele vale (a linha de história vai para o Diário). Condição de um sistema que ainda não existe (missões) é
/// pulada e aparece em <see cref="ConditionsWaitingForSystems"/>. Os modificadores valem para o equipado, os conjuntos
/// completos e as relíquias do alforje.
/// </summary>
public sealed class ItemTracker : IDisposable
{
    private readonly ItemsState _state;
    private readonly EventBus _events;
    private readonly Func<IConditionFacts> _facts;
    private readonly HashSet<string> _waiting = new(StringComparer.Ordinal);
    private bool _refreshing;

    public ItemTracker(WorldState state, EventBus events, IReadOnlyDictionary<string, Item> items, IReadOnlyDictionary<string, ItemSet> sets,
        Func<IConditionFacts> facts)
    {
        _state = state.Items;
        _events = events;
        Definitions = items;
        Sets = sets;
        _facts = facts;
        _events.AnyPublished += OnAnyEvent;
    }

    public IReadOnlyDictionary<string, Item> Definitions { get; }

    public IReadOnlyDictionary<string, ItemSet> Sets { get; }

    public IReadOnlyCollection<string> ConditionsWaitingForSystems => _waiting;

    public IEnumerable<Item> Owned => _state.Owned.Keys.Where(Definitions.ContainsKey).Select(id => Definitions[id]);

    public bool Owns(string itemId) => _state.Owned.ContainsKey(itemId);

    public ItemStage? StageOf(string itemId) =>
        _state.Owned.TryGetValue(itemId, out var index) ? Definitions[itemId].Stages[index].Stage : null;

    public string? EquippedIn(EquipSlot slot) => _state.Equipped.GetValueOrDefault(slot);

    public EquipSlot? SlotOf(string itemId) =>
        _state.Equipped.FirstOrDefault(p => p.Value == itemId) is { Value: not null } pair ? pair.Key : null;

    public bool IsSetComplete(string setId) => _state.CompletedSets.Contains(setId);

    /// <summary>Soma do equipado (no estágio atual), dos conjuntos completos e das relíquias no alforje.</summary>
    public ItemModifiers Modifiers =>
        _state.Equipped.Values.Concat(Owned.Where(i => i.Category == ItemCategory.Relic).Select(i => i.Id))
            .Select(id => Definitions[id].Stages[_state.Owned[id]].Modifiers)
            .Concat(_state.CompletedSets.Where(Sets.ContainsKey).Select(id => Sets[id].Modifiers))
            .Aggregate(ItemModifiers.None, (sum, m) => sum.Plus(m));

    /// <summary>O jogador ganha o item (no estágio Gasto). Falso se já tinha.</summary>
    public bool Acquire(string itemId)
    {
        var item = Get(itemId);
        if (!_state.Owned.TryAdd(itemId, 0))
        {
            return false;
        }

        _events.Publish(new ItemAcquired(itemId));
        if (item.SetId is { } setId && Sets.TryGetValue(setId, out var set) && !IsSetComplete(setId) && set.Pieces.All(Owns))
        {
            _state.CompletedSets.Add(setId);
            _events.Publish(new ItemSetCompleted(setId, set.StoryKey));
        }

        return true;
    }

    /// <summary>
    /// Equipa no espaço da categoria (ex-voto: o espaço pedido, ou o primeiro livre, ou o 1). O que estava ali sai.
    /// Relíquias e utensílios não se equipam.
    /// </summary>
    public void Equip(string itemId, EquipSlot? votiveSlot = null)
    {
        var item = Get(itemId);
        if (!Owns(itemId))
        {
            throw new InvalidOperationException($"O jogador não tem o item '{itemId}'.");
        }

        var slot = item.Category switch
        {
            ItemCategory.Instrument => EquipSlot.Instrument,
            ItemCategory.Garment => EquipSlot.Garment,
            ItemCategory.Amulet => EquipSlot.Amulet,
            ItemCategory.Votive => votiveSlot is EquipSlot.Votive1 or EquipSlot.Votive2 ? votiveSlot.Value
                : EquippedIn(EquipSlot.Votive1) is null ? EquipSlot.Votive1
                : EquippedIn(EquipSlot.Votive2) is null ? EquipSlot.Votive2
                : EquipSlot.Votive1,
            _ => throw new InvalidOperationException($"'{itemId}' ({item.Category}) não se equipa."),
        };

        Unequip(itemId);
        _state.Equipped[slot] = itemId;
    }

    public void Unequip(string itemId)
    {
        if (SlotOf(itemId) is { } slot)
        {
            _state.Equipped.Remove(slot);
        }
    }

    /// <summary>Confere a evolução (os eventos já chamam; chame depois de mudar o estado sem evento).</summary>
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
            foreach (var item in Owned.ToList())
            {
                var index = _state.Owned[item.Id];
                while (index + 1 < item.Stages.Count && Passes(item, item.Stages[index + 1], facts))
                {
                    index++;
                    _state.Owned[item.Id] = index;
                    var stage = item.Stages[index];
                    _events.Publish(new ItemEvolved(item.Id, stage.Stage, stage.StoryKey));
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

    private bool Passes(Item item, ItemStageDefinition stage, IConditionFacts facts)
    {
        try
        {
            return ConditionEvaluator.Passes(stage.When, facts);
        }
        catch (NotSupportedException)
        {
            _waiting.Add($"{item.Id}.{stage.Stage}");
            return false;
        }
    }

    private Item Get(string itemId) => Definitions.TryGetValue(itemId, out var item)
        ? item
        : throw new ArgumentException($"Item '{itemId}' não existe.", nameof(itemId));
}
