using Lete.Core.Combat.Gestures;

namespace Lete.Core.Combat;

/// <summary>Os cinco tipos de Lembrança (GDD 2.1); cada um com seu Traço e seu tipo de efeito.</summary>
public enum RecollectionKind
{
    Childhood,
    Feast,
    Family,
    Mourning,
    Craft,
}

/// <summary>
/// O que uma Memória faz em combate (GDD 2.1, "Efeito das habilidades"): Infância cura, Festa golpeia, Família
/// protege do próximo golpe, Luto deixa a sombra mais lenta, Ofício alarga a próxima janela de defesa.
/// </summary>
public enum MemoryEffectKind
{
    Heal,
    Strike,
    Guard,
    Slow,
    Focus,
}

/// <summary>
/// Uma Memória (habilidade de uma Lembrança): o Traço que a ativa, o custo em Lucidez (o falho custa
/// <paramref name="FailedCostMultiplier"/> vezes mais: "forçou a lembrança", GDD 4.4) e a força do efeito por resultado.
/// </summary>
public sealed record MemorySkill(
    string Id,
    RecollectionKind Kind,
    string NameKey,
    string DescriptionKey,
    StrokePattern Stroke,
    float LucidityCost,
    float FailedCostMultiplier,
    MemoryEffectKind Effect,
    GradeValues Power)
{
    public float CostFor(GestureGrade grade) =>
        grade == GestureGrade.Failed ? LucidityCost * FailedCostMultiplier : LucidityCost;

    public IEnumerable<string> Validate()
    {
        foreach (var error in Stroke.Validate())
        {
            yield return $"{Id}: {error}";
        }

        if (LucidityCost < 0f || FailedCostMultiplier < 1f)
        {
            yield return $"{Id}: custo negativo ou falho mais barato que o normal.";
        }
    }
}

/// <summary>Resultado de usar uma Memória: o efeito, a força e quanto de Lucidez foi gasto.</summary>
public sealed record MemoryUse(MemorySkill Skill, GestureGrade Grade, float Power, float Cost);

/// <summary>
/// As Memórias do protagonista em combate. Contam quantas vezes cada uma foi usada; quando a Lucidez zera (por
/// qualquer gasto), a mais usada fica <b>esquecida</b> (indisponível) até a próxima Trívia (GDD 4.2). A Leveza das
/// Pesadas deixadas dá proteções: cada uma segura um esquecimento, e todas voltam na Trívia (GDD 2.2).
/// </summary>
public sealed class MemoryBook
{
    private readonly Dictionary<string, int> _uses = new();
    private readonly List<string> _useOrder = new();
    private readonly HashSet<string> _forgotten = new();

    private readonly int _protections;

    public MemoryBook(IReadOnlyList<MemorySkill> skills, int protections = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(protections);
        Skills = skills;
        _protections = protections;
        ProtectionsLeft = protections;
    }

    /// <summary>Proteções da Leveza que ainda restam até a próxima Trívia.</summary>
    public int ProtectionsLeft { get; private set; }

    public IReadOnlyList<MemorySkill> Skills { get; }
    public IReadOnlyCollection<string> Forgotten => _forgotten;

    public bool IsForgotten(string id) => _forgotten.Contains(id);

    /// <summary>Dá para usar: não esquecida e com Lucidez para o custo normal.</summary>
    public bool CanUse(string id, Gauge lucidity) =>
        Skills.FirstOrDefault(s => s.Id == id) is { } skill && !IsForgotten(id) && lucidity.Current >= skill.LucidityCost;

    public MemoryUse Use(string id, GestureGrade grade, Gauge lucidity)
    {
        var skill = Skills.FirstOrDefault(s => s.Id == id) ?? throw new ArgumentException($"Memória '{id}' não existe.", nameof(id));
        if (IsForgotten(id))
        {
            throw new InvalidOperationException($"Memória '{id}' está esquecida.");
        }

        var cost = Math.Min(lucidity.Current, skill.CostFor(grade));
        lucidity.Reduce(cost);
        _uses[id] = _uses.GetValueOrDefault(id) + 1;
        _useOrder.Remove(id);
        _useOrder.Add(id);
        return new MemoryUse(skill, grade, skill.Power.For(grade), cost);
    }

    /// <summary>
    /// Chamar depois de qualquer gasto de Lucidez: zerada, esquece a Memória mais usada (no empate, a usada por
    /// último), a não ser que reste uma proteção da Leveza, que é gasta no lugar (<see cref="Forgetting.Resisted"/>).
    /// Devolve o que aconteceu agora, ou nulo.
    /// </summary>
    public Forgetting? CheckForgetting(Gauge lucidity)
    {
        if (!lucidity.IsEmpty || _uses.Count == 0)
        {
            return null;
        }

        var candidates = _uses.Where(u => !_forgotten.Contains(u.Key)).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        var most = candidates.Max(u => u.Value);
        var id = _useOrder.Last(i => candidates.Any(c => c.Key == i && c.Value == most));
        if (ProtectionsLeft > 0)
        {
            ProtectionsLeft--;
            return new Forgetting(Skills.First(s => s.Id == id), Resisted: true);
        }

        _forgotten.Add(id);
        return new Forgetting(Skills.First(s => s.Id == id), Resisted: false);
    }

    /// <summary>Descanso numa Trívia: nada fica esquecido.</summary>
    public void Rest()
    {
        _forgotten.Clear();
        _uses.Clear();
        _useOrder.Clear();
        ProtectionsLeft = _protections;
    }
}

/// <summary>A Lucidez zerou: <see cref="Skill"/> foi esquecida, ou quase (a Leveza a segurou).</summary>
public sealed record Forgetting(MemorySkill Skill, bool Resisted);
