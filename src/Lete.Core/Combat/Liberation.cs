using Lete.Core.Combat.Gestures;

namespace Lete.Core.Combat;

/// <summary>
/// Uma fixação que o jogador pode acreditar ser a da sombra (as pistas nunca são exatas, GDD 4.3).
/// <paramref name="WrongKey"/>: a fala com que a sombra corrige a pista quando essa escolha está errada
/// (vazia na opção verdadeira).
/// </summary>
public sealed record FixationOption(string Id, string LabelKey, string WrongKey);

/// <summary>
/// Regras de Libertar uma sombra (GDD 4.5): a fixação verdadeira entre as opções, quanta paz ela precisa,
/// quanto cada tentativa certa dá por resultado do Arrastar, o custo em Lucidez, o quanto o golpe seguinte
/// fica mais forte depois de um erro, e as falas de progresso e de partida.
/// </summary>
public sealed record LiberationRules(
    string TrueFixation,
    IReadOnlyList<FixationOption> Options,
    float Peace,
    GradeValues PeaceGain,
    float LucidityCost,
    float EnragedDamageMultiplier,
    string ProgressKey,
    string FreedKey)
{
    public IEnumerable<string> Validate()
    {
        if (Options.Count < 2)
        {
            yield return "Libertar precisa de pelo menos 2 opções de fixação.";
        }

        if (Options.All(o => o.Id != TrueFixation))
        {
            yield return $"A fixação verdadeira '{TrueFixation}' não está entre as opções.";
        }

        if (Options.Any(o => o.Id != TrueFixation && string.IsNullOrEmpty(o.WrongKey)))
        {
            yield return "Toda opção errada precisa da fala com que a sombra corrige a pista (wrong_key).";
        }

        if (Peace <= 0f || LucidityCost < 0f || EnragedDamageMultiplier < 1f)
        {
            yield return "A paz precisa ser positiva, o custo não pode ser negativo e a fúria não pode enfraquecer o golpe.";
        }
    }
}

/// <summary>Resultado de uma tentativa: se a fixação estava certa e a fala que a sombra diz.</summary>
public sealed record LiberationResult(bool Correct, string TextKey);

/// <summary>
/// Libertar em andamento: só abre com a Compreensão completa e Lucidez suficiente. Cada tentativa gasta Lucidez;
/// a fixação certa enche a paz conforme o Arrastar; a errada sai das opções (a sombra corrigiu a pista).
/// </summary>
public sealed class Liberation
{
    private readonly List<FixationOption> _remaining;

    public Liberation(LiberationRules rules)
    {
        Rules = rules;
        _remaining = rules.Options.ToList();
    }

    public LiberationRules Rules { get; }
    public float Peace { get; private set; }
    public float Fraction => Peace / Rules.Peace;
    public bool IsFreed => Peace >= Rules.Peace;
    public IReadOnlyList<FixationOption> RemainingOptions => _remaining;

    public bool CanAttempt(ShadeUnderstanding understanding, Gauge lucidity) =>
        understanding.IsComplete && lucidity.Current >= Rules.LucidityCost && !IsFreed;

    public LiberationResult Attempt(string optionId, GestureGrade grade, Gauge lucidity)
    {
        var option = _remaining.FirstOrDefault(o => o.Id == optionId)
            ?? throw new ArgumentException($"Opção '{optionId}' não está disponível.", nameof(optionId));
        lucidity.Reduce(Rules.LucidityCost);

        if (option.Id != Rules.TrueFixation)
        {
            _remaining.Remove(option);
            return new LiberationResult(false, option.WrongKey);
        }

        Peace = Math.Min(Rules.Peace, Peace + Rules.PeaceGain.For(grade));
        return new LiberationResult(true, IsFreed ? Rules.FreedKey : Rules.ProgressKey);
    }
}
