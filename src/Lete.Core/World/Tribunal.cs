using Lete.Core.Events;
using Lete.Core.Recollections;
using Lete.Core.State;

namespace Lete.Core.World;

/// <summary>Para onde o veredito manda a sombra (GDD 7.2). No conteúdo: asfodelos, elisios, tartaro.</summary>
public enum ShadeDestination
{
    Asphodel,
    Elysium,
    Tartarus,
}

/// <summary>Postura do jogador no julgamento.</summary>
public enum TrialStance
{
    Defend,
    Accuse,
    Silence,
}

/// <summary>Uma prova possível: uma entrada do Diário ou uma Lembrança.</summary>
public enum EvidenceKind
{
    Journal,
    Recollection,
}

public sealed record Evidence(EvidenceKind Kind, string Id);

/// <summary>Uma prova que pesa no julgamento: positiva a favor da sombra, negativa contra.</summary>
public sealed record WeightedEvidence(Evidence Evidence, int Weight);

/// <summary>Um veredito possível: o destino e a nota mínima (o último da lista, sem mínimo, é o que sobra).</summary>
public sealed record Verdict(ShadeDestination Destination, int? Min, string TextKey);

/// <summary>
/// Um julgamento (<c>content/trials/&lt;id&gt;.json</c>, GDD 7.2): o juiz, a sombra, quantas provas o jogador pode
/// apresentar, as provas que pesam, as Lembranças cuja versão incerta vira perjúrio e os vereditos, do melhor ao pior.
/// </summary>
public sealed record Trial(
    string Id,
    string JudgeKey,
    string ShadeId,
    string IntroKey,
    int Slots,
    IReadOnlyList<WeightedEvidence> Weights,
    IReadOnlyList<string> PerjuryIfUncertain,
    IReadOnlyList<Verdict> Verdicts);

/// <summary>Quanto a postura soma à nota (<c>content/rules/tribunal.json</c>).</summary>
public sealed record TribunalRules(int DefendBonus, int AccuseBonus);

/// <summary>O resultado: o veredito, a nota e as Lembranças apresentadas que foram perjúrio.</summary>
public sealed record TrialOutcome(Verdict Verdict, int Score, IReadOnlyList<string> Perjuries);

/// <summary>
/// O Tribunal (GDD 7.2; decisões do João, 30/out/2026). O jogador escolhe até <see cref="Trial.Slots"/> provas entre o
/// que tem no Diário e as Lembranças que tem ou teve (a falsa também: o jogo guarda que ela existiu) e uma postura. A
/// nota é a soma dos pesos das provas apresentadas mais o da postura; o primeiro veredito cujo mínimo ela alcança vale.
/// <b>Calar</b> não apresenta nada (nota 0). Ele depõe sob juramento: apresentar uma Lembrança falsa é sempre perjúrio, e
/// uma incerta, quando o julgamento a marca (a Marca sobe a cada perjúrio). Cada julgamento acontece uma vez.
/// </summary>
public sealed class Tribunal
{
    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly TribunalRules _rules;
    private readonly Mosaic _mosaic;

    public Tribunal(WorldState state, EventBus events, IReadOnlyDictionary<string, Trial> trials, TribunalRules rules, Mosaic mosaic)
    {
        _state = state;
        _events = events;
        Definitions = trials;
        _rules = rules;
        _mosaic = mosaic;
    }

    public IReadOnlyDictionary<string, Trial> Definitions { get; }

    public ShadeDestination? VerdictOf(string trialId) =>
        _state.Trials.TryGetValue(trialId, out var verdict) ? verdict : null;

    /// <summary>O que o jogador pode apresentar: as entradas do Diário e as Lembranças que tem ou teve.</summary>
    public IReadOnlyList<Evidence> AvailableEvidence() =>
        _state.JournalEntries.Select(e => new Evidence(EvidenceKind.Journal, e))
            .Concat(_state.Recollections.Keys.OrderBy(k => k, StringComparer.Ordinal).Select(r => new Evidence(EvidenceKind.Recollection, r)))
            .ToList();

    public TrialOutcome Judge(string trialId, TrialStance stance, IReadOnlyCollection<Evidence> presented)
    {
        var trial = Definitions.TryGetValue(trialId, out var found) ? found : throw new ArgumentException($"Julgamento '{trialId}' não existe.", nameof(trialId));
        if (_state.Trials.ContainsKey(trialId))
        {
            throw new InvalidOperationException($"O julgamento '{trialId}' já aconteceu.");
        }

        var evidence = stance == TrialStance.Silence ? Array.Empty<Evidence>() : presented.Distinct().ToArray();
        if (evidence.Length > trial.Slots || evidence.Except(AvailableEvidence()).Any())
        {
            throw new ArgumentException("Provas demais, ou que o jogador não tem.", nameof(presented));
        }

        var perjuries = new List<string>();
        foreach (var recollection in evidence.Where(e => e.Kind == EvidenceKind.Recollection).Select(e => e.Id))
        {
            _events.Publish(new TestimonyGiven(trialId, recollection));
            var certainty = _mosaic.CertaintyOf(recollection);
            if (certainty == RecollectionCertainty.False
                || (certainty == RecollectionCertainty.Uncertain && trial.PerjuryIfUncertain.Contains(recollection)))
            {
                perjuries.Add(recollection);
                _state.Perjuries++;
                _events.Publish(new TestimonyPerjury(trialId, recollection));
            }
        }

        var score = stance switch { TrialStance.Defend => _rules.DefendBonus, TrialStance.Accuse => _rules.AccuseBonus, _ => 0 }
            + evidence.Sum(e => trial.Weights.FirstOrDefault(w => w.Evidence == e)?.Weight ?? 0);
        var verdict = trial.Verdicts.First(v => v.Min is null || score >= v.Min);
        _state.Trials[trialId] = verdict.Destination;
        _events.Publish(new TrialJudged(trialId, trial.ShadeId, verdict.Destination, stance));
        return new TrialOutcome(verdict, score, perjuries);
    }
}
