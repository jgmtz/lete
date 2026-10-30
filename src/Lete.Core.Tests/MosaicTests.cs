using Lete.Core.Combat;
using Lete.Core.Data;
using Lete.Core.Events;
using Lete.Core.Recollections;
using Lete.Core.State;

namespace Lete.Core.Tests;

public class MosaicTests
{
    public static readonly MosaicRules Rules = new(KeyKinds: 3, new Dictionary<RecollectionStage, float>
    {
        [RecollectionStage.Fragment] = 0.5f,
        [RecollectionStage.Recalled] = 0.75f,
        [RecollectionStage.Full] = 1f,
    });

    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<object> _events = new();
    private readonly Mosaic _mosaic;

    public MosaicTests()
    {
        _bus.AnyPublished += e => _events.Add(e);
        var recollections = new[]
        {
            Make("agua_parada", RecollectionKind.Mourning,
                Source("remador", SourceType.FreeShade, shade: "remador", reliable: false),
                Source("sonho", SourceType.Dream, reliable: false),
                Source("pescador", SourceType.Dialogue),
                Source("outra_versao", SourceType.Dialogue, contradicts: true)),
            Make("colo_da_avo", RecollectionKind.Childhood, Source("a", SourceType.Anchor), Source("b", SourceType.Dialogue)),
            Make("mesa_cheia", RecollectionKind.Feast, Source("a", SourceType.Anchor), Source("b", SourceType.Dialogue)),
            Make("maos_no_barro", RecollectionKind.Craft, Source("a", SourceType.Anchor), Source("b", SourceType.Dialogue)),
            Make("o_convite", RecollectionKind.Family, RecollectionScope.Heavy, Source("a", SourceType.Anchor), Source("b", SourceType.Dialogue)),
        }.ToDictionary(r => r.Id);
        _mosaic = new Mosaic(_state, _bus, recollections, Rules);
    }

    private static RecollectionSource Source(string id, SourceType type, string? shade = null, bool reliable = true, bool contradicts = false) =>
        new(id, type, shade, null, reliable, contradicts);

    private static Recollection Make(string id, RecollectionKind kind, params RecollectionSource[] sources) =>
        Make(id, kind, RecollectionScope.Common, sources);

    private static Recollection Make(string id, RecollectionKind kind, RecollectionScope scope, params RecollectionSource[] sources) =>
        new(id, kind, scope, null, $"rec.{id}", new Dictionary<RecollectionStage, string>(), $"mem_{id}",
            sources, new[] { "ato" }, 0, 0);

    [Fact]
    public void FreeingAShade_GivesAFragmentThroughTheEvent()
    {
        _bus.Publish(new ShadeFreed("remador"));

        Assert.Equal(RecollectionStage.Fragment, _mosaic.StageOf("agua_parada"));
        Assert.Contains(new RecollectionGained("agua_parada", RecollectionStage.Fragment), _events);
        Assert.Equal("mem_agua_parada", _mosaic.SkillOf("agua_parada"));
    }

    [Fact]
    public void UnreliableSources_StayUncertainFragmentUntilAReliableOneConfirms()
    {
        _mosaic.Hear("agua_parada", "remador");
        _mosaic.Hear("agua_parada", "sonho");
        Assert.Equal(RecollectionCertainty.Uncertain, _mosaic.CertaintyOf("agua_parada"));
        Assert.Equal(RecollectionStage.Fragment, _mosaic.StageOf("agua_parada"));
        Assert.False(_mosaic.CompleteAct("agua_parada", "ato"));

        _mosaic.Hear("agua_parada", "pescador");
        Assert.Equal(RecollectionCertainty.Certain, _mosaic.CertaintyOf("agua_parada"));
        Assert.Equal(RecollectionStage.Recalled, _mosaic.StageOf("agua_parada"));
        Assert.Contains(new RecollectionAdvanced("agua_parada", RecollectionStage.Recalled), _events);
    }

    [Fact]
    public void ReliableSecondSource_Recalls_ThenTheActMakesItFull()
    {
        _mosaic.Hear("colo_da_avo", "a");
        Assert.Equal(RecollectionStage.Fragment, _mosaic.StageOf("colo_da_avo"));
        Assert.False(_mosaic.Hear("colo_da_avo", "a"));

        _mosaic.Hear("colo_da_avo", "b");
        Assert.True(_mosaic.CompleteAct("colo_da_avo", "ato"));
        Assert.Equal(RecollectionStage.Full, _mosaic.StageOf("colo_da_avo"));
        Assert.False(_mosaic.CompleteAct("colo_da_avo", "ato"));
        Assert.Throws<ArgumentException>(() => _mosaic.CompleteAct("colo_da_avo", "outro_ato"));
    }

    [Fact]
    public void ContradictingAnUncertainOne_MakesItFalse_GoneWithItsSkill()
    {
        _mosaic.Hear("agua_parada", "remador");
        _mosaic.Hear("agua_parada", "outra_versao");

        Assert.Equal(RecollectionCertainty.False, _mosaic.CertaintyOf("agua_parada"));
        Assert.Null(_mosaic.StageOf("agua_parada"));
        Assert.Null(_mosaic.SkillOf("agua_parada"));
        Assert.Empty(_mosaic.Known);
        Assert.Contains(new RecollectionFalsified("agua_parada"), _events);
        Assert.False(_mosaic.Hear("agua_parada", "pescador"));
    }

    [Fact]
    public void Contradiction_DoesNothingToACertainOneOrAnUnknownOne()
    {
        Assert.False(_mosaic.Hear("agua_parada", "outra_versao"));
        Assert.Null(_mosaic.CertaintyOf("agua_parada"));

        _mosaic.Hear("agua_parada", "pescador");
        Assert.False(_mosaic.Hear("agua_parada", "outra_versao"));
        Assert.Equal(RecollectionCertainty.Certain, _mosaic.CertaintyOf("agua_parada"));
    }

    [Fact]
    public void MemoryKey_ThreeKindsFull_HeavyDoesNotCount()
    {
        foreach (var id in new[] { "colo_da_avo", "mesa_cheia" })
        {
            _mosaic.Hear(id, "a");
            _mosaic.Hear(id, "b");
            _mosaic.CompleteAct(id, "ato");
        }

        _state.HeavyMemories["o_convite"] = HeavyMemoryChoice.Face;
        Assert.Equal(RecollectionStage.Full, _mosaic.StageOf("o_convite"));
        Assert.Throws<ArgumentException>(() => _mosaic.Hear("o_convite", "a"));

        Assert.Equal(2, _mosaic.FullKinds);
        Assert.False(_mosaic.HasMemoryKey);

        _mosaic.Hear("maos_no_barro", "a");
        _mosaic.Hear("maos_no_barro", "b");
        _mosaic.CompleteAct("maos_no_barro", "ato");
        Assert.True(_mosaic.HasMemoryKey);
    }

    [Fact]
    public void Data_ReadsScopeSourcesAndRejectsMissingShade()
    {
        var recollection = RecollectionData.FromJson("""
            {
              "id": "a_agua_parada", "kind": "mourning", "scope": "origin:pescador", "title_key": "rec.t",
              "skill": "mem_1",
              "stages": { "fragment": { "text_key": "rec.s1" } },
              "sources": [
                { "id": "afogado", "type": "free_shade", "shade": "afogado_do_cais", "reliable": false },
                { "id": "outra", "type": "dialogue", "contradicts": true }
              ],
              "full_by": [ "ancora_cais" ],
              "mosaic_position": { "x": 2, "y": 3 }
            }
            """);

        Assert.Equal(RecollectionScope.Origin, recollection.Scope);
        Assert.Equal("mem_1", recollection.Skill);
        Assert.Equal("pescador", recollection.OriginId);
        Assert.False(recollection.Sources[0].Reliable);
        Assert.True(recollection.Sources[1].Contradicts);
        Assert.Equal((2, 3), (recollection.MosaicX, recollection.MosaicY));

        Assert.Throws<FormatException>(() => RecollectionData.FromJson("""
            { "id": "x", "kind": "feast", "scope": "common", "title_key": "k", "sources": [ { "id": "s", "type": "free_shade" } ] }
            """));
    }

    private static MemorySkill Skill(string id) => new(
        id, RecollectionKind.Mourning, id, id, new Combat.Gestures.StrokePattern(new[] { Combat.Gestures.StrokeDirection.Down, Combat.Gestures.StrokeDirection.Up }, 0.5f, 0.2f, 0.5f),
        1f, 2f, MemoryEffectKind.Heal, new GradeValues(20f, 10f, 4f));

    [Fact]
    public void CombatMemories_ComeFromTheMosaicScaledByStage_FalseOnesGone()
    {
        var skills = new[] { "agua_parada", "colo_da_avo", "mesa_cheia", "maos_no_barro", "o_convite" }.Select(id => Skill($"mem_{id}")).ToList();
        Assert.Empty(_mosaic.CombatMemories(skills));

        _mosaic.Hear("colo_da_avo", "a");
        _mosaic.Hear("colo_da_avo", "b");
        _mosaic.CompleteAct("colo_da_avo", "ato");
        _mosaic.Hear("agua_parada", "remador");

        var memories = _mosaic.CombatMemories(skills);
        Assert.Equal(new[] { "agua_parada", "colo_da_avo" }, memories.Select(m => m.RecollectionId).OrderBy(i => i));
        var full = memories.Single(m => m.RecollectionId == "colo_da_avo");
        Assert.Equal((RecollectionStage.Full, 20f), (full.Stage, full.Skill.Power.Perfect));
        var uncertain = memories.Single(m => m.RecollectionId == "agua_parada");
        Assert.Equal((RecollectionStage.Fragment, 10f), (uncertain.Stage, uncertain.Skill.Power.Perfect));

        _mosaic.Hear("agua_parada", "outra_versao");
        Assert.DoesNotContain(_mosaic.CombatMemories(skills), m => m.RecollectionId == "agua_parada");
    }
}
