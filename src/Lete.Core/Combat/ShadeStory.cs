namespace Lete.Core.Combat;

/// <summary>Uma fala que a sombra diz quando a Firmeza cai abaixo de <paramref name="BelowFraction"/> (GDD 4.3).</summary>
public sealed record ShadeVoice(float BelowFraction, string TextKey);

/// <summary>
/// Fase nova da sombra (GDD 4.3 e 4.11): ao cair abaixo de <paramref name="AtFraction"/> da Firmeza, o gesto muda
/// (ciclo novo de golpes), uma legenda anuncia, e às vezes algo se revela (a fixação, no Remador).
/// </summary>
public sealed record ShadePhase(float AtFraction, IReadOnlyList<ShadeAttack> Attacks, string CaptionKey, ListenReveal? Reveals);

/// <summary>A oferenda que a sombra reconhece (GDD 5.1): a certa revela nome e fixação; a errada não funciona.</summary>
public sealed record ShadeOffering(string AcceptsKind, string AcceptKey, string RefuseKey);

/// <summary>
/// O que a sombra conta pelas outras vias (GDD 4.5): a história inteira (Presenciar), a pista ao fugir, a cena e a
/// lembrança dela ao deixar-se vencer (com o custo em Lucidez), as pistas de Hécate depois de cada derrota, e a
/// fala de quando é poupada.
/// </summary>
public sealed record ShadeStory(
    IReadOnlyList<ShadeVoice> Voices,
    IReadOnlyList<ShadePhase> Phases,
    IReadOnlyList<string> StoryKeys,
    string FleeClueKey,
    IReadOnlyList<string> YieldKeys,
    float YieldLucidityCost,
    IReadOnlyList<string> DefeatClueKeys,
    string SpareKey,
    ShadeOffering? Offering)
{
    public IEnumerable<string> Validate()
    {
        if (Voices.Any(v => v.BelowFraction is <= 0f or >= 1f) || Phases.Any(p => p.AtFraction is <= 0f or >= 1f))
        {
            yield return "Vozes e fases precisam de um limite de Firmeza entre 0 e 1.";
        }

        if (Phases.Any(p => p.Attacks.Count == 0))
        {
            yield return "Toda fase precisa de pelo menos um golpe.";
        }

        if (StoryKeys.Count == 0 || YieldKeys.Count == 0 || DefeatClueKeys.Count == 0)
        {
            yield return "História (Presenciar), cena de deixar-se vencer e pistas de derrota precisam de pelo menos uma fala.";
        }

        if (YieldLucidityCost < 0f)
        {
            yield return "O custo de deixar-se vencer não pode ser negativo.";
        }
    }
}

/// <summary>
/// Acompanha o que a sombra já disse e em que fase está. Cada voz e cada fase acontece uma vez só, na ordem da
/// Firmeza caindo; as pistas de derrota saem uma por derrota (a última se repete).
/// </summary>
public sealed class ShadeNarration
{
    private readonly ShadeStory _story;
    private readonly HashSet<ShadeVoice> _spoken = new();
    private readonly HashSet<ShadePhase> _reached = new();

    public ShadeNarration(ShadeStory story) => _story = story;

    /// <summary>Falas novas desde a última vez, da mais alta para a mais baixa (pode vir mais de uma num golpe forte).</summary>
    public IReadOnlyList<ShadeVoice> NewVoices(float resolveFraction)
    {
        var lines = _story.Voices
            .Where(v => resolveFraction < v.BelowFraction && !_spoken.Contains(v))
            .OrderByDescending(v => v.BelowFraction)
            .ToList();
        _spoken.UnionWith(lines);
        return lines;
    }

    /// <summary>A fase mais profunda alcançada agora e ainda não anunciada (ou nulo).</summary>
    public ShadePhase? NewPhase(float resolveFraction)
    {
        var reached = _story.Phases
            .Where(p => resolveFraction < p.AtFraction && !_reached.Contains(p))
            .OrderBy(p => p.AtFraction)
            .ToList();
        _reached.UnionWith(reached);
        return reached.FirstOrDefault();
    }

    /// <summary>Pista de Hécate para a derrota número <paramref name="defeats"/> (a partir de 1).</summary>
    public static string DefeatClue(ShadeStory story, int defeats) =>
        story.DefeatClueKeys[Math.Clamp(defeats - 1, 0, story.DefeatClueKeys.Count - 1)];
}
