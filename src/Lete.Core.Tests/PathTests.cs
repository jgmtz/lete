using Lete.Core.Conditions;
using Lete.Core.Data;
using Lete.Core.Events;
using Lete.Core.Recollections;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class PathTests
{
    private readonly WorldState _state = new();
    private readonly EventBus _bus = new();
    private readonly List<object> _events = new();
    private readonly Journal _journal;
    private readonly PathKeys _path;

    public PathTests()
    {
        _bus.AnyPublished += e => _events.Add(e);
        _journal = new Journal(_state, _bus);
        var mosaic = new Mosaic(_state, _bus, new Dictionary<string, Recollection>(), MosaicTests.Rules);
        var facts = new WorldFacts(_state, new MarkRules(4, 8, 4), mosaic, new Profile(_state, _bus, ProfileTests.Rules));
        var keys = new[]
        {
            new PathKey("aqueronte", "diario.aqueronte", new[]
            {
                new PathWay("obolo", new FlagSet("pagou_caronte")),
                new PathWay("origem_pescador", new OriginIs("pescador")),
            }),
            new PathKey("cerbero", "diario.cerbero", new[] { new PathWay("trivia", new TriviaIsLit("trivia_do_palacio")) }),
        }.ToDictionary(k => k.Id);
        _path = new PathKeys(_state, _bus, keys, () => facts);
    }

    [Fact]
    public void AWayThatPasses_GivesTheKeyOnTheNextEvent_WithItsJournalLine()
    {
        _state.Flags.Add("pagou_caronte");
        Assert.False(_path.Has("aqueronte"));

        _bus.Publish(new RegionEntered("aqueronte"));

        Assert.Equal("obolo", _path.WayOf("aqueronte"));
        Assert.Contains(new PathKeyObtained("aqueronte", "obolo", "diario.aqueronte"), _events);
        Assert.Contains("diario.aqueronte", _journal.Entries);
    }

    [Fact]
    public void TheKeyStays_EvenIfTheWayStopsPassing()
    {
        _state.Flags.Add("pagou_caronte");
        _path.Refresh();
        _state.Flags.Remove("pagou_caronte");
        _path.Refresh();

        Assert.True(_path.Has("aqueronte"));
        Assert.Single(_events.OfType<PathKeyObtained>());
    }

    [Fact]
    public void TheFirstWayThatPasses_IsTheOneRecorded()
    {
        _state.OriginId = "pescador";
        _state.Flags.Add("pagou_caronte");
        _path.Refresh();

        Assert.Equal("obolo", _path.WayOf("aqueronte"));
    }

    [Fact]
    public void AWayOfAMissingSystem_IsSkippedAndListed()
    {
        _path.Refresh();

        Assert.False(_path.Has("cerbero"));
        Assert.Contains("cerbero.trivia", _path.WaysWaitingForSystems);
    }

    [Fact]
    public void Data_ReadsWaysAndRejectsBrokenOnes()
    {
        var key = PathData.FromJson("""
            { "id": "aqueronte", "journal_key": "d", "ways": [ { "id": "obolo", "condition": { "flag": "pagou" } } ] }
            """);
        Assert.Equal(new FlagSet("pagou"), Assert.Single(key.Ways).Condition);

        Assert.Throws<FormatException>(() => PathData.FromJson("""{ "id": "x", "journal_key": "d", "ways": [] }"""));
        Assert.Throws<FormatException>(() => PathData.FromJson("""{ "id": "x", "journal_key": "d", "ways": [ { "id": "a" } ] }"""));
        Assert.Throws<ConditionException>(() => PathData.FromJson(
            """{ "id": "x", "journal_key": "d", "ways": [ { "id": "a", "condition": { "voar": 1 } } ] }"""));
    }
}
