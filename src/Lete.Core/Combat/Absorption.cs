namespace Lete.Core.Combat;

/// <summary>
/// Um Eco: o ataque de uma sombra absorvida (GDD 4.6), usado com o gesto dela (<paramref name="Gesture"/>, em
/// <c>content/gestures</c>). Tira Firmeza por resultado e sobe a Marca a cada uso.
/// </summary>
public sealed record EchoDefinition(
    string Id,
    string NameKey,
    string DescriptionKey,
    string Gesture,
    GradeValues ResolveDamage,
    int MarkOnUse);

/// <summary>
/// Regras de Absorver uma sombra (GDD 4.5): só com a Firmeza em até <paramref name="ResolveFractionMax"/>.
/// O gesto é o Segurar; quanto mais fraca a sombra, mais rápido a barra enche (até <paramref name="WeakFillFactor"/>
/// do tempo normal, com a Firmeza zerada). A Marca sobe conforme o resultado (o falho ainda absorve, mas pesa mais).
/// </summary>
public sealed record AbsorbRules(float ResolveFractionMax, float WeakFillFactor, GradeValues MarkGain, EchoDefinition Echo)
{
    public bool CanAbsorb(ShadeResolve resolve) => !resolve.IsBroken && resolve.Fraction <= ResolveFractionMax;

    /// <summary>Fator do tempo de encher a barra para a Firmeza atual: 1 no limite de absorver, <see cref="WeakFillFactor"/> em 0.</summary>
    public float FillFactor(float resolveFraction)
    {
        var weakness = 1f - Math.Clamp(resolveFraction / ResolveFractionMax, 0f, 1f);
        return 1f - (1f - WeakFillFactor) * weakness;
    }

    public IEnumerable<string> Validate()
    {
        if (ResolveFractionMax is <= 0f or > 1f)
        {
            yield return $"Limite de Firmeza para absorver fora de (0, 1] (veio {ResolveFractionMax}).";
        }

        if (WeakFillFactor is <= 0f or > 1f)
        {
            yield return $"Fator de enchimento fora de (0, 1] (veio {WeakFillFactor}).";
        }

        if (Echo.MarkOnUse < 0)
        {
            yield return "A Marca do Eco não pode ser negativa.";
        }
    }
}

/// <summary>
/// Os espaços de Eco do protagonista (GDD 4.6: 3 espaços). Um Eco por sombra; cada um só pode ser usado uma vez
/// por batalha (<see cref="NewBattle"/> libera todos).
/// </summary>
public sealed class EchoSlots
{
    public const int DefaultCapacity = 3;

    private readonly List<EchoDefinition> _echoes = new();
    private readonly HashSet<string> _usedThisBattle = new();

    public EchoSlots(int capacity = DefaultCapacity)
    {
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "É preciso pelo menos um espaço de Eco.");
        }

        Capacity = capacity;
    }

    public int Capacity { get; }
    public IReadOnlyList<EchoDefinition> Echoes => _echoes;
    public bool IsFull => _echoes.Count >= Capacity;

    /// <summary>Guarda o Eco; falso se os espaços estão cheios ou se ele já está guardado.</summary>
    public bool Add(EchoDefinition echo)
    {
        if (IsFull || _echoes.Any(e => e.Id == echo.Id))
        {
            return false;
        }

        _echoes.Add(echo);
        return true;
    }

    public bool CanUse(string id) => _echoes.Any(e => e.Id == id) && !_usedThisBattle.Contains(id);

    public bool HasUsable => _echoes.Any(e => !_usedThisBattle.Contains(e.Id));

    public EchoDefinition Use(string id)
    {
        if (!CanUse(id))
        {
            throw new InvalidOperationException($"Eco '{id}' não está disponível nesta batalha.");
        }

        _usedThisBattle.Add(id);
        return _echoes.First(e => e.Id == id);
    }

    public void NewBattle() => _usedThisBattle.Clear();
}

/// <summary>
/// Marca dos Mortos, só como contador por enquanto (o sistema inteiro é do M2). Nunca aparece como número para o
/// jogador (princípio 1); só no debug.
/// </summary>
public sealed class MarkCounter
{
    public int Value { get; private set; }

    public void Add(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "A Marca não desce (é irreversível).");
        }

        Value += amount;
    }
}
