using Lete.Core.Data;
using Lete.Core.Dialogue;
using Lete.Core.Events;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

public class OriginFragmentTests
{
    private readonly WorldState _state = new() { OriginId = "pescador" };
    private readonly EventBus _bus = new();
    private readonly OriginFragments _fragments;
    private readonly Journal _journal;

    private static readonly Origin Pescador = new("pescador", "n", "c", "a", "w", "s",
        new[] { "ismena", "calias" }, new[] { "remador" },
        new[]
        {
            new OriginFragment("pescador_ismena", "ismena", "frag.ismena"),
            new OriginFragment("pescador_calias", "calias", "frag.calias"),
            new OriginFragment("pescador_remador", "remador", "frag.remador"),
        });

    private static readonly Origin Artesao = new("artesao", "n", "c", "a", "w", "s",
        new[] { "dimas" }, new[] { "padeiro" },
        new[] { new OriginFragment("artesao_padeiro", "padeiro", "frag.padeiro") });

    public OriginFragmentTests()
    {
        _journal = new Journal(_state, _bus);
        // A conversa "conversa_remador" é com o Remador.
        _fragments = new OriginFragments(_state, _bus, new[] { Pescador, Artesao },
            id => id == "conversa_remador" ? "remador" : id);
    }

    [Fact]
    public void Freeing_TalkingOrTravelling_WithSomeoneLinked_GivesTheirFragmentOnce_IntoTheJournal()
    {
        _bus.Publish(new ShadeFreed("remador"));
        _bus.Publish(new DialogueEnded("conversa_remador"));
        _bus.Publish(new CompanionChosen("ismena"));
        _bus.Publish(new CompanionTalked("ismena"));
        _bus.Publish(new CompanionCommented("c", "calias", CommentResponse.Ignore));

        Assert.Equal(new[] { "pescador_remador", "pescador_ismena", "pescador_calias" }, _fragments.Gained);
        Assert.Equal(new[] { "frag.remador", "frag.ismena", "frag.calias" }, _journal.Entries);
    }

    [Fact]
    public void SomeoneOfAnotherOrigin_GivesNothing()
    {
        _bus.Publish(new ShadeFreed("padeiro"));
        _bus.Publish(new DialogueEnded("padeiro"));
        Assert.Empty(_fragments.Gained);

        _state.OriginId = null;
        _bus.Publish(new ShadeFreed("remador"));
        Assert.Empty(_fragments.Gained);
    }

    [Fact]
    public void GainById_FromADialogueEffect_AlsoWritesTheText()
    {
        Assert.True(_fragments.Gain("artesao_padeiro"));
        Assert.False(_fragments.Gain("artesao_padeiro"));
        Assert.True(_fragments.Gain("sem_texto"));
        Assert.Equal(new[] { "frag.padeiro" }, _journal.Entries);
    }

    [Fact]
    public void Content_EachOriginHasOneFragmentPerLinkedPerson_ThreeCompanionsAndTwoSpirits()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "project.godot")))
        {
            root = root.Parent!;
        }

        var content = Path.Combine(root.FullName, "content");
        var origins = OriginData.FromJson(File.ReadAllText(Path.Combine(content, "characters", "origens.json")));
        var cast = CastData.FromJson(File.ReadAllText(Path.Combine(content, "characters", "elenco.json")));
        foreach (var origin in origins)
        {
            Assert.Equal(3, origin.Companions.Count);
            Assert.Equal(2, origin.Spirits.Count);
            Assert.All(origin.Spirits, s => Assert.Equal(Combat.CastKind.Shade, cast[s].Kind));
            Assert.Equal(origin.Companions.Concat(origin.Spirits).OrderBy(p => p), origin.Fragments.Select(f => f.With).OrderBy(p => p));
        }

        // A mesma quantidade para todas (a origem dá alternativa, nunca vantagem).
        Assert.Single(origins.Select(o => o.Fragments.Count).Distinct());
    }
}
