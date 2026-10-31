namespace Lete.Core.Combat;

/// <summary>Em que ponto da rodada a luta está.</summary>
public enum BattlePhase
{
    PlayerTurn,
    CompanionSupport,
    ShadeTurn,

    /// <summary>A luta acabou por uma das vias (ver <see cref="BattleFlow.Outcome"/>), sem derrota.</summary>
    Victory,

    /// <summary>O Fôlego zerou (a derrota involuntária, com custo: GDD 4.9).</summary>
    Defeat,
}

/// <summary>Como a luta terminou sem derrota: as vias de GDD 4.5.</summary>
public enum BattleOutcome
{
    Destroyed,
    Freed,
    Absorbed,

    /// <summary>Presenciar: ficou até o fim do ciclo dela, sem lutar, e viu a história inteira.</summary>
    Witnessed,

    /// <summary>Poupar: deixou-a ir; ela segue vagando, "ainda por resolver".</summary>
    Spared,

    /// <summary>Fugir: escapou (e saiu sabendo algo).</summary>
    Fled,

    /// <summary>Deixar-se vencer: escolheu perder (não é a derrota involuntária).</summary>
    Yielded,
}

/// <summary>
/// Máquina de turnos do combate (GDD 4.1): jogador → apoio do companheiro (quando é a vez dele) → sombra,
/// com as checagens de fim de luta depois de cada passo. Não sabe de animação nem de gesto: quem conduz a cena
/// avisa quando cada passo terminou.
/// </summary>
public sealed class BattleFlow
{
    private IReadOnlyList<ShadeAttack> _attacks;
    private int _shadeTurns;
    private int _cycleStart;
    private BattleOutcome? _pendingOutcome;

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

    /// <summary>Como a luta acabou; nulo enquanto não acabar (ou se acabou em derrota).</summary>
    public BattleOutcome? Outcome { get; private set; }

    /// <summary>Rodada atual, começando em 1.</summary>
    public int Round { get; private set; } = 1;

    /// <summary>Quantos golpes tem o ciclo atual da sombra.</summary>
    public int CycleLength => _attacks.Count;

    /// <summary>Posição do próximo golpe no ciclo da sombra (o ciclo se repete, como o gesto que ela fazia em vida).</summary>
    public int NextShadeAttackIndex => (_shadeTurns - _cycleStart) % _attacks.Count;

    public ShadeAttack NextShadeAttack => _attacks[NextShadeAttackIndex];

    /// <summary>O golpe depois do próximo (o Canto de Cálias o anuncia).</summary>
    public ShadeAttack ShadeAttackAfterNext => _attacks[(NextShadeAttackIndex + 1) % _attacks.Count];

    /// <summary>Quantas rodadas faltam para o companheiro apoiar (0 = apoia nesta rodada); nulo sem companheiro.</summary>
    public int? RoundsUntilSupport => Support is null
        ? null
        : (Support.IntervalTurns - Round % Support.IntervalTurns) % Support.IntervalTurns;

    /// <summary>
    /// O apoio vale do momento em que o companheiro age até o fim do turno da sombra (Postura) ou até o fim da próxima
    /// ação do jogador (os outros apoios).
    /// </summary>
    public bool SupportActive { get; private set; }

    /// <summary>O apoio ativo saiu relutante: o jogador usou uma via que o companheiro reprova.</summary>
    public bool SupportReluctant { get; private set; }

    private bool _reluctancePending;

    /// <summary>A força do apoio <paramref name="effect"/> agora (0 se não é ele que está ativo); relutante, menor.</summary>
    public float SupportPower(Systems.SupportEffect effect) =>
        SupportActive && Support is { } s && s.Effect == effect
            ? s.Power * (SupportReluctant ? s.ReluctantFactor : 1f)
            : 0f;

    /// <summary>A ação deste turno foi por uma via que o companheiro reprova: o próximo apoio sai relutante.</summary>
    public void ReproveAction() => _reluctancePending = true;

    /// <summary>A sombra está enfurecida (Libertar com a fixação errada): o próximo golpe dela é mais forte.</summary>
    public bool Enraged { get; private set; }

    public bool IsOver => Phase is BattlePhase.Victory or BattlePhase.Defeat;

    public void Enrage() => Enraged = true;

    /// <summary>Família: fração do próximo golpe da sombra que não chega (0 = nenhuma proteção).</summary>
    public float GuardFraction { get; private set; }

    /// <summary>Luto: o próximo golpe da sombra fica este tanto mais lento (1 = normal).</summary>
    public float ShadeSlowFactor { get; private set; } = 1f;

    /// <summary>Ofício: quanto as janelas da próxima defesa ficam mais largas.</summary>
    public float FocusWidening { get; private set; }

    /// <summary>Presenciando: quantos golpes dela ainda faltam para fechar o ciclo; nulo se não está presenciando.</summary>
    public int? WitnessTurnsLeft { get; private set; }

    public bool IsWitnessing => WitnessTurnsLeft is not null;

    /// <summary>
    /// Efeito de uma Memória que vale até o fim do próximo turno da sombra (Família, Luto, Ofício). Usar a mesma
    /// de novo não soma: vale a mais forte.
    /// </summary>
    public void ApplyUntilShadeTurnEnds(MemoryEffectKind effect, float power)
    {
        switch (effect)
        {
            case MemoryEffectKind.Guard:
                GuardFraction = Math.Max(GuardFraction, Math.Clamp(power, 0f, 1f));
                break;
            case MemoryEffectKind.Slow:
                ShadeSlowFactor = Math.Max(ShadeSlowFactor, Math.Max(1f, power));
                break;
            case MemoryEffectKind.Focus:
                FocusWidening = Math.Max(FocusWidening, Math.Max(0f, power));
                break;
            default:
                throw new ArgumentException($"{effect} não dura até o turno da sombra.", nameof(effect));
        }
    }

    /// <summary>A sombra foi absorvida (a ação do jogador terminou com ela dentro dele).</summary>
    public void AbsorbShade() => EndBattle(BattleOutcome.Absorbed);

    /// <summary>
    /// A ação do jogador encerra a luta por uma via imediata (Absorver, Poupar, Fugir, Deixar-se vencer); vale quando
    /// a ação terminar (<see cref="EndPlayerAction"/>).
    /// </summary>
    public void EndBattle(BattleOutcome outcome)
    {
        if (outcome is BattleOutcome.Destroyed or BattleOutcome.Freed or BattleOutcome.Witnessed)
        {
            throw new ArgumentException($"{outcome} acontece pela regra, não por escolha direta.", nameof(outcome));
        }

        _pendingOutcome = outcome;
    }

    /// <summary>A ação do jogador já decidiu o fim da luta (Absorver, Poupar, Fugir, Deixar-se vencer).</summary>
    public bool IsEnding => _pendingOutcome is not null;

    /// <summary>Presenciar: a partir de agora só se defende, até ela completar um ciclo inteiro de golpes.</summary>
    public void StartWitnessing() => WitnessTurnsLeft = CycleLength;

    /// <summary>Desistir de presenciar no meio (volta a lutar normalmente).</summary>
    public void StopWitnessing() => WitnessTurnsLeft = null;

    /// <summary>A sombra muda de gesto (fase nova): o ciclo novo começa do primeiro golpe.</summary>
    public void ChangeCycle(IReadOnlyList<ShadeAttack> attacks)
    {
        if (attacks.Count == 0)
        {
            throw new ArgumentException("O ciclo novo precisa de pelo menos um golpe.", nameof(attacks));
        }

        _attacks = attacks;
        _cycleStart = _shadeTurns;
        if (IsWitnessing)
        {
            // Presenciar acompanha o ciclo inteiro: com o gesto novo, conta de novo.
            WitnessTurnsLeft = CycleLength;
        }
    }

    /// <summary>A ação do jogador terminou (já com o efeito aplicado na Firmeza, Lucidez etc.).</summary>
    public void EndPlayerAction()
    {
        Expect(BattlePhase.PlayerTurn);
        if (SupportActive && Support is { LastsUntilShadeTurnEnds: false })
        {
            // O apoio para a ação do jogador já foi usado nela.
            SupportActive = false;
        }

        if (!Check())
        {
            Phase = RoundsUntilSupport == 0 ? BattlePhase.CompanionSupport : BattlePhase.ShadeTurn;
            if (Phase == BattlePhase.CompanionSupport)
            {
                // Já na vez dele se sabe se vem relutante (ele age dizendo).
                SupportReluctant = _reluctancePending;
                _reluctancePending = false;
            }
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
        if (Support is { LastsUntilShadeTurnEnds: true })
        {
            SupportActive = false;
        }

        Enraged = false;
        GuardFraction = 0f;
        ShadeSlowFactor = 1f;
        FocusWidening = 0f;
        if (WitnessTurnsLeft is { } left)
        {
            WitnessTurnsLeft = left - 1;
        }

        if (!Check())
        {
            Round++;
            Phase = BattlePhase.PlayerTurn;
        }
    }

    private bool Check()
    {
        if (_pendingOutcome is { } chosen)
        {
            End(chosen);
        }
        else if (Liberation is { IsFreed: true })
        {
            End(BattleOutcome.Freed);
        }
        else if (Resolve.IsBroken)
        {
            End(BattleOutcome.Destroyed);
        }
        else if (Breath.IsEmpty)
        {
            Phase = BattlePhase.Defeat;
        }
        else if (WitnessTurnsLeft == 0)
        {
            // Aguentou o ciclo inteiro sem lutar: ela conta a história toda.
            End(BattleOutcome.Witnessed);
        }

        return IsOver;
    }

    private void End(BattleOutcome outcome)
    {
        Phase = BattlePhase.Victory;
        Outcome = outcome;
        WitnessTurnsLeft = null;
    }

    private void Expect(BattlePhase phase)
    {
        if (Phase != phase)
        {
            throw new InvalidOperationException($"Esperava {phase}, mas a luta está em {Phase}.");
        }
    }
}
