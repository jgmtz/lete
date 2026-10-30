using Lete.Core.Events;
using Lete.Core.State;

namespace Lete.Core.Systems;

/// <summary>
/// Marca dos Mortos (GDD 2.3, <c>docs/PROTAGONISTA_SOMBRA.md</c> 6 e 7). Guarda o contador e a etapa no
/// <see cref="WorldState"/>; os dois só sobem. A etapa 1 vem do primeiro Eco absorvido (escuta
/// <see cref="ShadeAbsorbed"/>); as etapas 2 e 3 vêm das faixas média e alta. O perjúrio (<see cref="OathBroken"/>)
/// sobe <see cref="MarkRules.PerjuryMark"/>. O rosto do fim sai da regra C.
/// </summary>
public sealed class MarkTracker : IDisposable
{
    public const int AbsorbedStage = 1;
    public const int MidStage = 2;
    public const int HighStage = 3;

    private readonly WorldState _state;
    private readonly EventBus _events;
    private readonly MarkRules _rules;
    private readonly IDisposable _absorbed;
    private readonly IDisposable _perjury;

    public MarkTracker(WorldState state, EventBus events, MarkRules rules)
    {
        _state = state;
        _events = events;
        _rules = rules;
        _absorbed = events.Subscribe<ShadeAbsorbed>(_ => RaiseStage(AbsorbedStage));
        _perjury = events.Subscribe<OathBroken>(_ => Add(_rules.PerjuryMark));
    }

    public MarkBand Band => _rules.BandOf(_state.Mark);

    public void Add(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "A Marca não desce (é irreversível).");
        }

        if (amount == 0)
        {
            return;
        }

        _state.Mark += amount;
        _events.Publish(new MarkIncreased(amount, Band));
        RaiseStage(Band switch
        {
            MarkBand.High => HighStage,
            MarkBand.Mid => MidStage,
            _ => 0,
        });
    }

    /// <summary>
    /// Regra C: com pelo menos <see cref="MarkRules.FacedToReturnFace"/> Pesadas encaradas o rosto volta; senão, a
    /// romã o cobre. Provisório: o meio-termo ainda não existe (PENDÊNCIAS 44). Chamado no fim do jogo.
    /// </summary>
    public FinalFace SettleFace()
    {
        var faced = _state.HeavyMemories.Values.Count(c => c == HeavyMemoryChoice.Face);
        _state.FinalFace = faced >= _rules.FacedToReturnFace ? FinalFace.Returned : FinalFace.Covered;
        _events.Publish(new FinalFaceSettled(_state.FinalFace));
        return _state.FinalFace;
    }

    public void Dispose()
    {
        _absorbed.Dispose();
        _perjury.Dispose();
    }

    private void RaiseStage(int stage)
    {
        if (stage <= _state.MarkStage)
        {
            return;
        }

        _state.MarkStage = stage;
        _events.Publish(new MarkStageReached(stage));
    }
}
