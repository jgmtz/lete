using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Dialogue;
using Lete.Core.Events;
using Lete.Core.Recollections;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class DialogueTests
{
    private const string Padeiro = """
        {
          "id": "padeiro", "mode": "shade", "speaker_key": "nome", "shade": "padeiro",
          "lucidity": { "base": "vaga", "raise": [ { "to": "desperta", "when": { "flag": "lembra_do_forno" } } ], "offering": "farinha" },
          "start": [ "desperto", "vago" ],
          "nodes": [
            { "id": "vago", "when": { "shade_lucidity": "vaga" }, "line_key": "l.vago",
              "options": [
                { "posture": "escutar", "text_key": "o.escutar", "next": "fornada" },
                { "posture": "perguntar", "text_key": "o.perguntar", "next": "vago" },
                { "posture": "escutar", "text_key": "o.forno", "requires": { "recollection": { "id": "maos", "min_stage": "fragment" } },
                  "hint_key": "o.forno.dica", "show_blocked": true, "next": "fim" },
                { "posture": "perguntar", "text_key": "o.escondida", "requires": { "flag": "nunca" }, "next": "fim" }
              ] },
            { "id": "fornada", "line_key": "l.fornada", "next": "fim", "effects": [ { "journal_key": "d.fornada" }, { "flag": "ouviu" } ] },
            { "id": "desperto", "when": { "shade_lucidity": "desperta" }, "line_key": "l.desperto",
              "options": [
                { "posture": "escutar", "text_key": "o.maos", "requires": { "origin": "artesao" }, "next": "fim" },
                { "posture": "perguntar", "text_key": "o.mulher", "next": "mulher" }
              ] },
            { "id": "mulher", "line_key": "l.mulher", "next": "fim",
              "comment": { "id": "c.mulher", "lines": [ { "companion": "aulo", "line_key": "c.aulo" } ] } }
          ]
        }
        """;

    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<object> _events = new();
    private readonly List<DialogueEffect> _applied = new();
    private readonly WorldFacts _facts;
    private readonly Dialogues _dialogues;

    private sealed class Effects : IDialogueEffects
    {
        private readonly DialogueTests _test;
        public Effects(DialogueTests test) => _test = test;

        public void Apply(DialogueEffect effect)
        {
            _test._applied.Add(effect);
            if (effect is SetFlag flag)
            {
                _test._state.Flags.Add(flag.Flag);
            }
        }
    }

    public DialogueTests()
    {
        _bus.AnyPublished += e => _events.Add(e);
        var maos = new Recollection("maos", RecollectionKind.Craft, RecollectionScope.Common, null, "t", new Dictionary<RecollectionStage, string>(),
            null, new[] { new RecollectionSource("a", SourceType.Anchor, null, null, true, false) }, Array.Empty<string>(), 0, 0);
        var mosaic = new Mosaic(_state, _bus, new Dictionary<string, Recollection> { ["maos"] = maos }, MosaicTests.Rules);
        _facts = new WorldFacts(_state, new MarkRules(4, 8, 4), mosaic, new Profile(_state, _bus, ProfileTests.Rules));
        var graph = DialogueData.FromJson(Padeiro);
        _dialogues = new Dialogues(_state, _bus, new Dictionary<string, DialogueGraph> { [graph.Id] = graph }, _facts, new Effects(this),
            id => id == "maos" ? RecollectionKind.Craft : null, new Offerings(_state, _bus, OfferingTests.Rules));
        _mosaic = mosaic;
    }

    private readonly Mosaic _mosaic;

    [Fact]
    public void Vague_ShowsTheGrayHint_HidesTheOtherBlocked_AndAlwaysHasSilence()
    {
        var runner = _dialogues.Open("padeiro");

        Assert.Equal(ShadeLucidity.Vague, runner.Lucidity);
        Assert.Equal("vago", runner.Current!.Id);
        Assert.Equal(new[] { "o.escutar", "o.perguntar", "o.forno", DialogueRunner.SilenceKey }, runner.Options.Select(o => o.Option.TextKey));
        var forno = runner.Options.Single(o => o.Option.TextKey == "o.forno");
        Assert.False(forno.Available);
        Assert.Equal(new DialogueTag(DialogueTagKind.Memory, "craft"), forno.Tag);

        _mosaic.Hear("maos", "a");
        runner = _dialogues.Open("padeiro");
        Assert.True(runner.Options.Single(o => o.Option.TextKey == "o.forno").Available);
    }

    [Fact]
    public void Choosing_AppliesTheNodeEffects_MarksSeen_AndEnds()
    {
        var runner = _dialogues.Open("padeiro");
        runner.Choose(runner.Options[0]);

        Assert.Equal("fornada", runner.Current!.Id);
        Assert.Contains(new AddJournalEntry("d.fornada"), _applied);
        Assert.Contains(new DialogueChoiceMade("padeiro", "vago", "escutar"), _events);

        runner.Continue();
        Assert.True(runner.IsOver);
        Assert.Contains(new DialogueEnded("padeiro"), _events);
        Assert.Null(_facts.ShadeLucidity);

        var again = _dialogues.Open("padeiro");
        Assert.True(again.Options[0].Seen);
        Assert.False(again.Options[1].Seen);
    }

    [Fact]
    public void TheRightOffering_AwakensForOneConversation()
    {
        _bus.Publish(new OfferingShared("padeiro", "mel"));
        Assert.Equal(ShadeLucidity.Vague, _dialogues.Open("padeiro").Lucidity);

        _bus.Publish(new OfferingShared("padeiro", "farinha"));
        var runner = _dialogues.Open("padeiro");
        Assert.Equal(ShadeLucidity.Awake, runner.Lucidity);
        Assert.Equal("desperto", runner.Current!.Id);
        Assert.DoesNotContain(runner.Options, o => o.Option.TextKey == "o.maos");

        runner.Choose(runner.Options.Single(o => o.Option.Posture == Posture.Silence));
        Assert.Equal(ShadeLucidity.Vague, _dialogues.Open("padeiro").Lucidity);
    }

    [Fact]
    public void ARaiseCondition_AwakensFromTheFile_AndTheOriginTagShows()
    {
        _state.Flags.Add("lembra_do_forno");
        _state.OriginId = "artesao";
        var runner = _dialogues.Open("padeiro");

        Assert.Equal(ShadeLucidity.Awake, runner.Lucidity);
        Assert.Equal(new DialogueTag(DialogueTagKind.Origin, "artesao"), runner.Options[0].Tag);
    }

    [Fact]
    public void CompanionComment_OnlyWithThatCompanion_AndOnlyOnce()
    {
        _state.Flags.Add("lembra_do_forno");
        var runner = _dialogues.Open("padeiro");
        runner.Choose(runner.Options.Single(o => o.Option.TextKey == "o.mulher"));
        Assert.Null(runner.PendingCommentKey);

        _state.CompanionId = "aulo";
        runner = _dialogues.Open("padeiro");
        runner.Choose(runner.Options.Single(o => o.Option.TextKey == "o.mulher"));
        Assert.Equal("c.aulo", runner.PendingCommentKey);
        runner.AnswerComment(CommentResponse.Disagree);
        Assert.Contains(new CompanionCommented("c.mulher", "aulo", CommentResponse.Disagree), _events);
        Assert.Null(runner.PendingCommentKey);

        runner = _dialogues.Open("padeiro");
        runner.Choose(runner.Options.Single(o => o.Option.TextKey == "o.mulher"));
        Assert.Null(runner.PendingCommentKey);
    }

    [Theory]
    [InlineData("""{ "id": "x", "mode": "fixed", "speaker_key": "s", "start": [ "a" ], "nodes": [ { "id": "a", "line_key": "l", "next": "b" } ] }""")]
    [InlineData("""{ "id": "x", "mode": "shade", "speaker_key": "s", "start": [ "a" ], "nodes": [ { "id": "a", "line_key": "l" } ] }""")]
    [InlineData("""{ "id": "x", "mode": "scene", "speaker_key": "s", "start": [ "a" ], "nodes": [ { "id": "a", "line_key": "l", "options": [ { "posture": "calar", "text_key": "t" } ] } ] }""")]
    [InlineData("""{ "id": "x", "mode": "fixed", "speaker_key": "s", "start": [ "a" ], "nodes": [ { "id": "a", "line_key": "l", "options": [ { "posture": "gritar", "text_key": "t" } ] } ] }""")]
    [InlineData("""{ "id": "x", "mode": "fixed", "speaker_key": "s", "start": [ "a" ], "nodes": [ { "id": "a", "line_key": "l", "options": [ { "posture": "calar", "text_key": "t", "requires": { "flag": "f" }, "show_blocked": true } ] } ] }""")]
    public void Data_RejectsBrokenGraphs(string json) => Assert.Throws<FormatException>(() => DialogueData.FromJson(json));

    [Fact]
    public void ShadeLucidityCondition_OnlyKnownStates() =>
        Assert.Throws<ConditionException>(() => ConditionParser.Parse("""{ "shade_lucidity": "acordada" }"""));
}
