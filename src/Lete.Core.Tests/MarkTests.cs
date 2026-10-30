using Lete.Core.Events;
using Lete.Core.Presentation;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class MarkTests
{
    private static readonly MarkRules Rules = new(MidFrom: 4, HighFrom: 8, FacedToReturnFace: 4);

    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<int> _stages = new();
    private readonly MarkTracker _mark;

    public MarkTests()
    {
        _bus.Subscribe<MarkStageReached>(e => _stages.Add(e.Stage));
        _mark = new MarkTracker(_state, _bus, Rules);
    }

    [Fact]
    public void Add_NeverGoesDown()
    {
        _mark.Add(2);
        Assert.Equal(2, _state.Mark);
        Assert.Throws<ArgumentOutOfRangeException>(() => _mark.Add(-1));
    }

    [Fact]
    public void FirstAbsorbedShade_ShowsThePomegranate_OnlyOnce()
    {
        _bus.Publish(new ShadeAbsorbed("remador"));
        _bus.Publish(new ShadeAbsorbed("remador"));

        Assert.Equal(1, _state.MarkStage);
        Assert.Equal(new[] { 1 }, _stages);
    }

    [Fact]
    public void Bands_RaiseTheStage_AndTheStageNeverDrops()
    {
        _bus.Publish(new ShadeAbsorbed("remador"));
        _mark.Add(3);
        Assert.Equal(MarkBand.Low, _mark.Band);
        Assert.Equal(1, _state.MarkStage);

        _mark.Add(1);
        Assert.Equal(MarkBand.Mid, _mark.Band);
        Assert.Equal(2, _state.MarkStage);

        _mark.Add(10);
        Assert.Equal(MarkBand.High, _mark.Band);
        Assert.Equal(3, _state.MarkStage);

        // Absorver de novo não volta para a etapa 1.
        _bus.Publish(new ShadeAbsorbed("outra"));
        Assert.Equal(3, _state.MarkStage);
        Assert.Equal(new[] { 1, 2, 3 }, _stages);
    }

    [Fact]
    public void Disposed_StopsListening()
    {
        _mark.Dispose();
        _bus.Publish(new ShadeAbsorbed("remador"));
        Assert.Equal(0, _state.MarkStage);
    }

    [Theory]
    [InlineData(6, 0, FinalFace.Returned)]
    [InlineData(4, 2, FinalFace.Returned)]
    [InlineData(3, 3, FinalFace.Covered)]
    [InlineData(0, 6, FinalFace.Covered)]
    public void RuleC_FacedMajorityReturnsTheFace(int faced, int left, FinalFace expected)
    {
        var heavy = new HeavyMemories(_state, _bus, HeavyMemoryTests.Queue(6));
        for (var i = 0; i < faced + left; i++)
        {
            heavy.Choose(heavy.Next!.Id, i < faced ? HeavyMemoryChoice.Face : HeavyMemoryChoice.Leave);
        }

        Assert.Equal(expected, _mark.SettleFace());
        Assert.Equal(expected, _state.FinalFace);
    }

    [Fact]
    public void HeavyMemory_CannotBeUndone()
    {
        var heavy = new HeavyMemories(_state, _bus, HeavyMemoryTests.Queue(2));
        heavy.Choose("pesada_1", HeavyMemoryChoice.Leave);

        Assert.Throws<InvalidOperationException>(() => heavy.Choose("pesada_1", HeavyMemoryChoice.Face));
        Assert.Equal(HeavyMemoryChoice.Leave, heavy.ChoiceFor("pesada_1"));
        Assert.Null(heavy.ChoiceFor("pesada_2"));
    }

    [Fact]
    public void OriginFragment_CountsOnce()
    {
        var fragments = new OriginFragments(_state, _bus);
        Assert.True(fragments.Gain("pescador_rede"));
        Assert.False(fragments.Gain("pescador_rede"));
        Assert.Equal(new[] { "pescador_rede" }, fragments.Gained);
    }

    [Fact]
    public void Art_FollowsStageThenFace()
    {
        Assert.Equal("f/etapa0.png", MarkArt.ImageFor("f", 0, FinalFace.None));
        Assert.Equal("f/etapa3.png", MarkArt.ImageFor("f", 3, FinalFace.None));
        Assert.Equal("f/rosto_coberto.png", MarkArt.ImageFor("f", 3, FinalFace.Covered));
        Assert.Equal(6, MarkArt.AllImages("f").Count());
    }

    [Fact]
    public void Look_LosesColorByBandAndShadowOnlyWhenHigh()
    {
        var look = new MarkLook(MidDesaturation: 0.3f, HighDesaturation: 0.7f);

        Assert.Equal(0f, look.DesaturationOf(MarkBand.Low));
        Assert.Equal(0.3f, look.DesaturationOf(MarkBand.Mid));
        Assert.Equal(0.7f, look.DesaturationOf(MarkBand.High));
        Assert.True(MarkLook.CastsShadow(MarkBand.Mid));
        Assert.False(MarkLook.CastsShadow(MarkBand.High));
        Assert.NotEmpty(new MarkLook(0.8f, 0.5f).Validate());
    }
}
