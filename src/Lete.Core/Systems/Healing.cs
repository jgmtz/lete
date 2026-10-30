using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// Cura fora da luta (GDD 4.8; nunca por derrotar sombras). A Trívia devolve tudo (Fôlego e Lucidez); o Fogo do Lar
/// (aceso com uma oferenda) e a hospitalidade de uma sombra libertada devolvem parte do Fôlego, cada fonte uma vez por
/// degrau da Maré (decisão do João, 30/out/2026). As provisões curam na luta, pela Bolsa.
/// </summary>
public sealed class Healing
{
    private readonly VitalsState _vitals;
    private readonly EventBus _events;
    private readonly HealingRules _rules;
    private readonly Offerings _offerings;
    private readonly Func<int> _tideStep;

    public Healing(WorldState state, EventBus events, HealingRules rules, Offerings offerings, Func<int> tideStep)
    {
        _vitals = state.Vitals;
        _events = events;
        _rules = rules;
        _offerings = offerings;
        _tideStep = tideStep;
    }

    /// <summary>Descansar numa Trívia: Fôlego e Lucidez cheios.</summary>
    public void Rest()
    {
        _vitals.BreathSpent = 0f;
        _vitals.LuciditySpent = 0f;
        _events.Publish(new Rested());
    }

    /// <summary>A fonte ainda cura neste degrau da Maré.</summary>
    public bool CanUse(string sourceId) =>
        !_vitals.HealUses.TryGetValue(sourceId, out var step) || step < _tideStep();

    /// <summary>
    /// Acende o Fogo do Lar <paramref name="fireId"/> com a primeira oferenda que o jogador tiver. Falso se já curou
    /// neste degrau ou se não há oferenda.
    /// </summary>
    public bool LightHearth(string fireId)
    {
        if (!CanUse(fireId) || _offerings.Carried.FirstOrDefault() is not { } kind)
        {
            return false;
        }

        _offerings.Spend(kind);
        Heal(fireId, _rules.HearthBreath);
        return true;
    }

    /// <summary>Hospitalidade da sombra <paramref name="shadeId"/> (pão ou leito). Falso se já curou neste degrau.</summary>
    public bool Hospitality(string shadeId)
    {
        var source = $"hospitalidade:{shadeId}";
        if (!CanUse(source))
        {
            return false;
        }

        Heal(source, _rules.HospitalityBreath);
        return true;
    }

    private void Heal(string source, float breath)
    {
        _vitals.HealUses[source] = _tideStep();
        _vitals.BreathSpent = Math.Max(0f, _vitals.BreathSpent - breath);
        _events.Publish(new Healed(source, breath));
    }
}

/// <summary>Quanto Fôlego cada fonte limitada devolve (<c>content/rules/cura.json</c>).</summary>
public sealed record HealingRules(float HearthBreath, float HospitalityBreath)
{
    public IEnumerable<string> Validate()
    {
        if (HearthBreath <= 0f || HospitalityBreath <= 0f)
        {
            yield return "As curas precisam devolver algum Fôlego.";
        }
    }
}
