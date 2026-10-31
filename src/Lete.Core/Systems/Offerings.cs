using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// Oferendas (GDD 5.1): vêm do Altar e das provisões, com um limite pequeno por tipo. <b>Dar</b> desperta a sombra;
/// <b>partilhar</b> (hospitalidade: o protagonista prova também) gasta a mesma oferenda, mas a sombra lembra com mais
/// calor. As duas publicam o evento (o Mosaico, as conversas e a Marca escutam; o sangue sobe a Marca).
/// </summary>
public sealed class Offerings
{
    private readonly InventoryState _inventory;
    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly OfferingRules _rules;

    public Offerings(WorldState state, EventBus events, OfferingRules rules)
    {
        _state = state;
        _inventory = state.Inventory;
        _events = events;
        _rules = rules;
    }

    public OfferingRules Rules => _rules;

    public int Count(string kind) => _inventory.Offerings.GetValueOrDefault(kind);

    /// <summary>Os tipos que o jogador tem agora, na ordem das regras.</summary>
    public IEnumerable<string> Carried => _rules.Kinds.Where(k => Count(k) > 0);

    /// <summary>Recolhe até o limite do tipo; devolve quantas couberam (o resto fica onde estava).</summary>
    public int Pick(string kind, int amount)
    {
        Known(kind);
        var taken = Math.Clamp(_rules.Capacity - Count(kind), 0, amount);
        _inventory.Offerings[kind] = Count(kind) + taken;
        return taken;
    }

    public bool Give(string shadeId, string kind) => Spend(kind, () => _events.Publish(new OfferingGiven(shadeId, kind)));

    public bool Share(string shadeId, string kind) => Spend(kind, () =>
    {
        _state.SharedOfferings.Add($"{shadeId}/{kind}");
        _events.Publish(new OfferingShared(shadeId, kind));
    });

    /// <summary>Gasta uma oferenda sem sombra (acender um Fogo do Lar). Falso se não tinha.</summary>
    public bool Spend(string kind) => Spend(kind, () => { });

    private bool Spend(string kind, Action publish)
    {
        Known(kind);
        if (Count(kind) <= 0)
        {
            return false;
        }

        _inventory.Offerings[kind] = Count(kind) - 1;
        publish();
        return true;
    }

    private void Known(string kind)
    {
        if (!_rules.Kinds.Contains(kind))
        {
            throw new ArgumentException($"Oferenda '{kind}' desconhecida (use {string.Join(", ", _rules.Kinds)}).", nameof(kind));
        }
    }
}

/// <summary>
/// Regras das oferendas (<c>content/rules/oferendas.json</c>): os tipos, o limite por tipo e qual é o sangue (acorda
/// qualquer sombra; a Marca que ele sobe fica em <c>marca.json</c>).
/// </summary>
public sealed record OfferingRules(IReadOnlyList<string> Kinds, int Capacity, string BloodKind)
{
    public IEnumerable<string> Validate()
    {
        if (Kinds.Count == 0 || Kinds.Distinct().Count() != Kinds.Count)
        {
            yield return "Os tipos de oferenda precisam existir e não se repetir.";
        }

        if (Capacity < 1)
        {
            yield return "O limite por tipo precisa ser pelo menos 1.";
        }

        if (!Kinds.Contains(BloodKind))
        {
            yield return $"O sangue ('{BloodKind}') precisa estar entre os tipos.";
        }
    }
}
