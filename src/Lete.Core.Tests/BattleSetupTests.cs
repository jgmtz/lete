using System.Numerics;
using Lete.Core.Combat;
using Lete.Core.Data;

namespace Lete.Core.Tests;

public class BattleSetupTests
{
    private const string ValidJson = """
        {
          "layout": { "party": [ { "x": -1, "y": -1 }, { "x": -2, "y": -0.5 } ], "enemy": [ { "x": 1, "y": 0 } ] },
          "party": [ { "id": "aulo", "art": "aulo_c", "height": { "_tbd": true, "value": 1.0 } } ],
          "enemy": [ { "id": "s", "art": "sombra_teste", "height": 1.0, "flip_h": true, "tint": "#8C9BAD" } ],
          "styles": [ { "id": "liso", "folder": "liso", "filter": "linear" }, { "id": "p", "folder": "pixel_128", "filter": "nearest" } ]
        }
        """;

    [Fact]
    public void Load_ReadsLayoutActorsAndStyles()
    {
        var setup = BattleData.FromJson(ValidJson);

        Assert.Equal(new Vector2(-2, -0.5f), setup.Layout.Party[1]);
        Assert.Equal(new BattleActorArt("aulo", "aulo_c", 1f, false, null), setup.Party[0]);
        Assert.True(setup.Enemies[0].FlipH);
        Assert.Equal("#8C9BAD", setup.Enemies[0].Tint);
        Assert.True(setup.Styles[0].Smooth);
        Assert.False(setup.Styles[1].Smooth);
    }

    [Fact]
    public void Layout_PartyMustBeBottomLeft()
    {
        var layout = new BattleLayout(new[] { new Vector2(1, -1) }, new[] { new Vector2(2, 1) });

        Assert.Contains(layout.Validate(), e => e.Contains("party[0]"));
    }

    [Fact]
    public void Layout_EnemyMustBeAboveTheWholeParty()
    {
        var layout = new BattleLayout(new[] { new Vector2(-1, -1), new Vector2(-2, 0.5f) }, new[] { new Vector2(1, 0) });

        Assert.Contains(layout.Validate(), e => e.Contains("enemy[0]"));
    }

    [Fact]
    public void Load_RejectsUnknownFilter()
    {
        var error = Assert.Throws<FormatException>(() => BattleData.FromJson(ValidJson.Replace("\"nearest\"", "\"cubic\"")));

        Assert.Contains("cubic", error.Message);
    }

    [Fact]
    public void ContentFile_IsValid()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
        {
            dir = dir.Parent;
        }

        var cast = CastData.FromJson(File.ReadAllText(Path.Combine(dir!.FullName, "content/characters/elenco.json")));
        var setup = BattleData.FromJson(File.ReadAllText(Path.Combine(dir.FullName, "content/battle/teste_elenco.json")), cast);

        Assert.Equal(2, setup.Party.Count);
        Assert.Single(setup.Enemies);
        Assert.Single(setup.Styles);
        Assert.Equal("lia_34_costas", setup.Party[0].Art);
        Assert.True(cast["ismena"].FrontFacesRight);
        Assert.True(cast["aulo"].FrontFacesRight);
        Assert.False(cast["lia"].FrontFacesRight);
    }
}
