namespace Lete.Core.Combat;

/// <summary>Em que ponto da rodada a luta está.</summary>
public enum BattlePhase
{
    PlayerTurn,
    CompanionSupport,
    ShadeTurn,
    Victory,
    Defeat,
}

/// <summary>Como a luta terminou a favor do jogador (as vias de GDD 4.5 que já existem).</summary>
public enum BattleOutcome
{
    Destroyed,
    Freed,
    Absorbed,
}

/// <summary>
/// Máquina de turnos do combate (GDD 4.1): jogador → apoio do companheiro (quando é a vez dele) → sombra,
/// com as checagens de vitória e derrota depois de cada passo. Não sabe de animação nem de gesto: quem
/// conduz a cena avisa quando cada passo terminou.
/// </summary>
public sealed class BattleFlow
{
    private readonly IReadOnlyList<ShadeAttack> _attacks;
    private int _shadeTurns;

    public BattleFlow(
        Gauge breath,
        Gauge lucidity,
        ShadeResolve resolve,
        IReadOnlyList<ShadeAttack> attacks,
        CompanionSupport? support,
        Liberation? liberation = null)
    {
        if (attacks.Count == 0)
        {
            throw new ArgumentException("A sombra precisa de pelo menos um golpe no ciclo.", nameof(attacks));
        }

        Breath = breath;
        Lucidity = lucidity;
        Resolve = resolve;
        _attacks = attacks;
        Support = support;
        Liberation = liberation;
    }

    public Gauge Breath { get; }
    public Gauge Lucidity { get; }
    public ShadeResolve Resolve { get; }
    public CompanionSupport? Support { get; }
    public Liberation? Liberation { get; }

    public BattlePhase Phase { get; private set; } = BattlePhase.PlayerTurn;

    /// <summary>Como a luta foi vencida; nulo enquanto não houver vitória.</summary>
    public BattleOutcome? Outcome { get; private set; }

    /// <summary>Rodada atual, começando em 1.</summary>
    public int Round { get; private set; } = 1;

    /// <summary>Posição do próximo golpe no ciclo da sombra (o ciclo se repete, como o gesto que ela fazia em vida).</summary>
    public int NextShadeAttackIndex => _shadeTurns % _attacks.Count;

    public ShadeAttack NextShadeAttack => _attacks[NextShadeAttackIndex];

    /// <summary>Quantas rodadas faltam para o companheiro apoiar (0 = apoia nesta rodada); nulo sem companheiro.</summary>
    public int? RoundsUntilSupport => Support is null
        ? null
        : (Support.IntervalTurns - Round % Support.IntervalTurns) % Support.IntervalTurns;

    /// <summary>O apoio vale do momento em que o companheiro age até o fim do turno da sombra.</summary>
    public bool SupportActive { get; private set; }

    /// <summary>A sombra está enfurecida (Libertar com a fixação errada): o próximo golpe dela é mais forte.</summary>
    public bool Enraged { get; private set; }

    public bool IsOver => Phase is BattlePhase.Victory or BattlePhase.Defeat;

    public void Enrage() => Enraged = true;

    /// <summary>A sombra foi absorvida (a ação do jogador terminou com ela dentro dele).</summary>
    public bool ShadeAbsorbed { get; private set; }

    public void AbsorbShade() => ShadeAbsorbed = true;

    /// <summary>A ação do jogador terminou (já com o efeito aplicado na Firmeza, Lucidez etc.).</summary>
    public void EndPlayerAction()
    {
        Expect(BattlePhase.PlayerTurn);
        if (!Check())
        {
            Phase = RoundsUntilSupport == 0 ? BattlePhase.CompanionSupport : BattlePhase.ShadeTurn;
        }
    }

    public void EndSupport()
    {
        Expect(BattlePhase.CompanionSupport);
        SupportActive = true;
        if (!Check())
        {
            Phase = BattlePhase.ShadeTurn;
        }
    }

    /// <summary>O golpe da sombra terminou (já com o dano aplicado no Fôlego).</summary>
    public void EndShadeTurn()
    {
        Expect(BattlePhase.ShadeTurn);
        _shadeTurns++;
        SupportActive = false;
        Enraged = false;
        if (!Check())
        {
            Round++;
            Phase = BattlePhase.PlayerTurn;
        }
    }

    private bool Check()
    {
        if (ShadeAbsorbed)
        {
            Phase = BattlePhase.Victory;
            Outcome = BattleOutcome.Absorbed;
        }
        else if (Liberation is { IsFreed: true })
        {
            Phase = BattlePhase.Victory;
            Outcome = BattleOutcome.Freed;
        }
        else if (Resolve.IsBroken)
        {
            Phase = BattlePhase.Victory;
            Outcome = BattleOutcome.Destroyed;
        }
        else if (Breath.IsEmpty)
        {
            Phase = BattlePhase.Defeat;
        }

        return IsOver;
    }

    private void Expect(BattlePhase phase)
    {
        if (Phase != phase)
        {
            throw new InvalidOperationException($"Esperava {phase}, mas a luta está em {Phase}.");
        }
    }
}
