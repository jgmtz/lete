namespace Lete.Core.Systems;

/// <summary>
/// Quanto a Maré sobe por fonte (<c>content/rules/mare.json</c>). A derrota soma os degraus do modo de dificuldade
/// (<c>defeat_tide_steps</c>), por isso não está aqui.
/// </summary>
public sealed record TideRules(decimal MilestoneStep, decimal TravelStep)
{
    public IEnumerable<string> Validate()
    {
        if (MilestoneStep <= 0m)
        {
            yield return "O degrau de um marco da Maré precisa ser positivo.";
        }

        if (TravelStep < 0m || TravelStep > MilestoneStep)
        {
            yield return "O avanço da Maré por viagem precisa ficar entre 0 e o de um marco.";
        }
    }
}
