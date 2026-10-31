using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Events;
using Lete.Core.Recollections;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

/// <summary>
/// Uma pequena partida pelos sistemas de verdade, salva e carregada: depois de carregar, os sistemas refeitos sobre o
/// estado lido respondem igual (M2: "salva e carrega o estado completo").
/// </summary>
public class StateIntegrationTests
{
    private static readonly MarkRules MarkRules = new(MidFrom: 4, HighFrom: 8, FacedToReturnFace: 4, PerjuryMark: 2);

    private static readonly Dictionary<string, Recollection> Recollections = new[]
    {
        new Recollection("agua", RecollectionKind.Craft, RecollectionScope.Common, null, "t", new Dictionary<RecollectionStage, string>(), null,
            new[]
            {
                new RecollectionSource("a", SourceType.Dream, null, null, true, false),
                new RecollectionSource("b", SourceType.Dialogue, null, null, true, false),
            },
            Array.Empty<string>(), 0, 0),
    }.ToDictionary(r => r.Id);

    private static readonly Dictionary<string, Oath> OathDefinitions = new[]
    {
        new Oath("mao_aberta", "n", "p", "b", "h", new[] { Via.Absorb }, null, null, OathEffects.None, OathEffects.None),
        new Oath("do_silencio", "n", "p", "b", "h", Array.Empty<Via>(), null, null, OathEffects.None, OathEffects.None),
    }.ToDictionary(o => o.Id);

    private static readonly Companion Aulo = new("aulo", "n", "postura", SupportEffect.WidenDefense, 0.15f, 0,
        new Dictionary<Via, Approval>(), "r", new[] { "c" });

    /// <summary>Os sistemas sobre um estado, como o GameRoot monta.</summary>
    private sealed class Game
    {
        public Game(WorldState state)
        {
            State = state;
            Mark = new MarkTracker(state, Bus, MarkRules);
            Tide = new TideTracker(state, Bus, new TideRules(1m, 0.1m));
            Mosaic = new Mosaic(state, Bus, Recollections, MosaicTests.Rules);
            Profile = new Profile(state, Bus, ProfileTests.Rules);
            Facts = new WorldFacts(state, MarkRules, Mosaic, Profile);
            Oaths = new Oaths(state, Bus, OathDefinitions, new OathRules(3), () => Facts);
            Offerings = new Offerings(state, Bus, OfferingTests.Rules);
            Companions = new Companions(state, Bus, new Dictionary<string, Companion> { ["aulo"] = Aulo }, CompanionTests.Rules, Tide);
            Shades = new ShadeRecord(state, Bus);
            Regions = new Regions(state, Bus);
        }

        public WorldState State { get; }
        public EventBus Bus { get; } = new();
        public MarkTracker Mark { get; }
        public TideTracker Tide { get; }
        public Mosaic Mosaic { get; }
        public Profile Profile { get; }
        public WorldFacts Facts { get; }
        public Oaths Oaths { get; }
        public Offerings Offerings { get; }
        public Companions Companions { get; }
        public ShadeRecord Shades { get; }
        public Regions Regions { get; }

        /// <summary>O que as condições e as telas leem do jogo.</summary>
        public object Snapshot() => new
        {
            Band = Mark.Band,
            State.MarkStage,
            Tide.Step,
            Recollection = Mosaic.StageOf("agua"),
            TopVia = Profile.TopVia,
            Oath = Oaths.StatusOf("mao_aberta"),
            Silence = Oaths.StatusOf("do_silencio"),
            Companions.Bond,
            Interval = Companions.IntervalTurns,
            Remador = Shades.StateOf("remador"),
            Region = Regions.Current,
            Shared = Facts.WasOfferingShared("padeiro", "mel"),
            Offerings = Offerings.Count("mel"),
        };
    }

    [Fact]
    public void APlaythrough_SavedAndLoaded_AnswersTheSame()
    {
        var game = new Game(new WorldState());
        game.Regions.Enter("asfodelos");
        game.Mosaic.Hear("agua", "a");
        game.Mosaic.Hear("agua", "b");
        game.Oaths.Swear("mao_aberta");
        game.Oaths.Swear("do_silencio");
        game.Companions.Choose("aulo");
        game.Offerings.Pick("mel", 3);
        game.Offerings.Share("padeiro", "mel");
        game.Offerings.Share("aulo", "mel");
        game.Bus.Publish(new ShadeFreed("remador"));
        game.Bus.Publish(new ShadeFreed("tecela"));
        game.Bus.Publish(new ShadeAbsorbed("lavadeira"));
        game.Mark.Add(3);

        var before = game.Snapshot();
        Assert.Equal(OathStatus.Broken, game.Oaths.StatusOf("mao_aberta"));

        var loaded = new Game(SaveGame.Read(SaveGame.Write(game.State)));
        Assert.Equal(before, loaded.Snapshot());

        // Depois de carregar, as regras continuam: o marco não conta de novo e a escolha continua irreversível.
        Assert.False(loaded.Tide.Milestone(Companions.ChosenMilestone));
        Assert.False(loaded.Companions.Choose("aulo"));
        Assert.False(loaded.Oaths.Swear("mao_aberta"));
    }
}
