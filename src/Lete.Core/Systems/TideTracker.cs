using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// Maré do Lete (GDD 2.6): número que só sobe, por progresso e nunca por relógio. Marcos (cada um conta uma vez),
/// derrotas e viagens entre Trívias. Publica <see cref="TideAdvanced"/> a cada subida e <see cref="TideStepReached"/>
/// para cada degrau inteiro cruzado (as tabelas das regiões reagem ao degrau).
/// </summary>
public sealed class TideTracker
{
    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly TideRules _rules;

    public TideTracker(WorldState state, EventBus events, TideRules rules)
    {
        _state = state;
        _events = events;
        _rules = rules;
    }

    /// <summary>O degrau inteiro em que a Maré está (o que as regiões e as condições <c>tide_min</c> leem).</summary>
    public int Step => StepOf(_state.Tide);

    /// <summary>Um marco da história (cruzar o Aqueronte, escolher o companheiro...). Repetido, não conta de novo.</summary>
    public bool Milestone(string milestoneId)
    {
        if (!_state.TideMilestones.Add(milestoneId))
        {
            return false;
        }

        Advance(_rules.MilestoneStep, TideCause.Milestone);
        return true;
    }

    public void Travel() => Advance(_rules.TravelStep, TideCause.Travel);

    /// <summary>Derrota em combate: os degraus vêm do modo de dificuldade (0, 1 ou 2).</summary>
    public void Defeat(int steps)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(steps);
        Advance(steps, TideCause.Defeat);
    }

    private void Advance(decimal amount, TideCause cause)
    {
        if (amount <= 0m)
        {
            return;
        }

        var before = Step;
        _state.Tide += amount;
        _events.Publish(new TideAdvanced(amount, cause));
        for (var step = before + 1; step <= Step; step++)
        {
            _events.Publish(new TideStepReached(step));
        }
    }

    public static int StepOf(decimal tide) => (int)decimal.Floor(tide);
}
