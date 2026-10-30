using System.Text.Json;
using Lete.Core.Combat;
using Lete.Core.Data;
using Lete.Core.Presentation;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Tests;

/// <summary>
/// Confere o conteúdo inteiro de <c>content/</c>: tudo carrega, e toda arte, gesto, companheiro e chave de
/// texto referenciados existem. Pega erro de dado sem precisar abrir o jogo.
/// </summary>
public class ContentTests
{
    private static readonly string Root = RepoRoot();
    private static readonly IReadOnlyDictionary<string, CastMember> Cast =
        CastData.FromJson(File.ReadAllText(Path.Combine(RepoRoot(), "content", "characters", "elenco.json")));

    private static string Content(params string[] parts) => Path.Combine(new[] { Root, "content" }.Concat(parts).ToArray());

    public static IEnumerable<object[]> BattleFiles() =>
        Directory.GetFiles(Path.Combine(RepoRoot(), "content", "battle"), "*.json")
            .Where(f => Path.GetFileName(f) != "hud_layouts.json")
            .Select(f => new object[] { Path.GetFileName(f) });

    [Theory]
    [MemberData(nameof(BattleFiles))]
    public void Battle_ArtExistsInEveryStyle(string file)
    {
        var setup = BattleData.FromJson(File.ReadAllText(Content("battle", file)), Cast);
        var missing = (
            from style in setup.Styles
            from actor in setup.Party.Concat(setup.Enemies)
            let path = Path.Combine(Root, "art", "sprites", "batalha", style.Folder, actor.Art + ".png")
            where !File.Exists(path)
            select $"{style.Folder}/{actor.Art}.png").ToList();
        missing.AddRange(setup.Party.Concat(setup.Enemies)
            .Where(a => a.Portrait is not null && !File.Exists(FromRes(a.Portrait)))
            .Select(a => a.Portrait!));
        missing.AddRange(
            from idle in setup.Party.Concat(setup.Enemies).Select(a => a.Idle).OfType<IdleAnimation>()
            from style in idle.PrefixByStyle.Keys
            from frame in Enumerable.Range(0, idle.Frames)
            let path = idle.FramePath(style, frame)
            where !File.Exists(FromRes(path))
            select path);

        Assert.Empty(missing);
    }

    [Theory]
    [MemberData(nameof(BattleFiles))]
    public void Battle_ProtagonistAndCompanionAreValid(string file)
    {
        var json = File.ReadAllText(Content("battle", file));
        if (!json.Contains("\"protagonist\""))
        {
            return;
        }

        var setup = BattleData.FromJson(json, Cast);
        Assert.Null(setup.ProtagonistSlotError(CombatData.ProtagonistFromJson(json).Id));
        if (CombatData.CompanionIdFromJson(json) is { } companion)
        {
            var support = CombatData.CompanionFromJson(File.ReadAllText(Content("companions", companion + ".json")));
            Assert.Contains(setup.Party, p => p.Id == support.CompanionId);
        }
    }

    [Fact]
    public void Cast_EveryFileItPointsToExists()
    {
        var missing = new List<string>();
        foreach (var member in Cast.Values)
        {
            foreach (var art in new[] { member.FrontArt, member.BackArt }.OfType<string>())
            {
                var path = Path.Combine(Root, "art", "sprites", "batalha", "pixel_160", art + ".png");
                if (!File.Exists(path))
                {
                    missing.Add($"{member.Id}: {art}.png");
                }
            }

            foreach (var res in new[] { member.Portrait, member.MapFolder, member.Reflection }.OfType<string>())
            {
                if (!File.Exists(FromRes(res)) && !Directory.Exists(FromRes(res)))
                {
                    missing.Add($"{member.Id}: {res}");
                }
            }
        }

        foreach (var member in Cast.Values.Where(m => m.Selectable))
        {
            if (member.InventoryFolder is null)
            {
                missing.Add($"{member.Id}: sem inventory_folder");
                continue;
            }

            missing.AddRange(MarkArt.AllImages(member.InventoryFolder).Where(i => !File.Exists(FromRes(i))).Select(i => $"{member.Id}: {i}"));
        }

        Assert.Empty(missing);
        Assert.Equal(2, Cast.Values.Count(m => m.Selectable));
        Assert.Single(Cast.Values, m => m.Selectable && m.Gender == "f");
    }

    [Fact]
    public void Mark_RulesLoad()
    {
        var json = File.ReadAllText(Content("rules", "marca.json"));
        Assert.Empty(MarkData.FromJson(json).Validate());
        Assert.Empty(MarkData.LookFromJson(json).Validate());
    }

    [Fact]
    public void Recollections_AllLoadWithMosaicRules()
    {
        var recollections = Directory.GetFiles(Content("recollections"), "*.json")
            .Select(f => RecollectionData.FromJson(File.ReadAllText(f)))
            .ToList();
        Assert.NotEmpty(recollections);
        Assert.All(recollections, r => Assert.True(File.Exists(Content("recollections", r.Id + ".json")), $"Id '{r.Id}' difere do arquivo."));

        var skills = Directory.GetFiles(Content("memories"), "*.json")
            .SelectMany(f => MemoryData.FromJson(File.ReadAllText(f)))
            .Select(m => m.Id)
            .ToHashSet();
        Assert.All(recollections.Where(r => r.Skill is not null),
            r => Assert.True(skills.Contains(r.Skill!), $"A Lembrança '{r.Id}' dá a Memória '{r.Skill}', que não existe."));

        // As Pesadas formam uma fila sem buracos (1, 2, 3...).
        Assert.NotEmpty(new HeavyMemories(new WorldState(), new Events.EventBus(), recollections).Queue);

        var rules = File.ReadAllText(Content("rules", "mosaico.json"));
        Assert.Empty(RecollectionData.RulesFromJson(rules).Validate());
        Assert.Equal(5, RecollectionData.StoneColorsFromJson(rules).Count);
    }

    [Fact]
    public void Tide_RulesLoad()
    {
        var rules = TideData.FromJson(File.ReadAllText(Content("rules", "mare.json")));
        Assert.Equal(1m, rules.MilestoneStep);
        Assert.Empty(rules.Validate());
    }

    [Fact]
    public void Path_EveryKeyLoadsWithItsFileName()
    {
        var keys = Directory.GetFiles(Content("path"), "*.json")
            .Select(f => (File: Path.GetFileNameWithoutExtension(f), Key: PathData.FromJson(File.ReadAllText(f))))
            .ToList();
        Assert.NotEmpty(keys);
        Assert.All(keys, k => Assert.Equal(k.File, k.Key.Id));
    }

    [Fact]
    public void Oaths_EveryOathLoadsWithItsFileName_RulesLoad()
    {
        var oaths = Directory.GetFiles(Content("oaths"), "*.json")
            .Select(f => (File: Path.GetFileNameWithoutExtension(f), Oath: OathData.FromJson(File.ReadAllText(f))))
            .ToList();
        Assert.NotEmpty(oaths);
        Assert.All(oaths, o => Assert.Equal(o.File, o.Oath.Id));
        Assert.Equal(3, OathData.RulesFromJson(File.ReadAllText(Content("rules", "juramentos.json"))).MaxActive);
        Assert.Equal(2, MarkData.FromJson(File.ReadAllText(Content("rules", "marca.json"))).PerjuryMark);
    }

    [Fact]
    public void Dialogues_EveryGraphLoadsWithItsFileName()
    {
        var graphs = Directory.GetFiles(Content("dialogue"), "*.json")
            .Select(f => (File: Path.GetFileNameWithoutExtension(f), Graph: Lete.Core.Dialogue.DialogueData.FromJson(File.ReadAllText(f))))
            .ToList();
        Assert.NotEmpty(graphs);
        Assert.All(graphs, g => Assert.Equal(g.File, g.Graph.Id));
    }

    [Fact]
    public void Items_LoadWithTheirSetsAndRelicPaths()
    {
        var items = Directory.GetFiles(Content("items"), "*.json").Select(File.ReadAllText)
            .Where(Lete.Core.Items.ItemData.IsItem).Select(Lete.Core.Items.ItemData.FromJson).ToList();
        var sets = Directory.GetFiles(Content("item_sets"), "*.json")
            .Select(f => Lete.Core.Items.ItemData.SetFromJson(File.ReadAllText(f))).ToList();
        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.True(File.Exists(Content("items", i.Id + ".json")), $"Item '{i.Id}' difere do arquivo."));
        Assert.All(sets, s => Assert.All(s.Pieces, p => Assert.Contains(items, i => i.Id == p && i.SetId == s.Id)));
        Assert.All(items.Where(i => i.PathKey is not null), i => Assert.True(File.Exists(Content("path", i.PathKey + ".json"))));
    }

    [Fact]
    public void Profile_RulesLoad() => ProfileData.FromJson(File.ReadAllText(Content("rules", "perfil.json")));

    [Fact]
    public void Origins_FourCardsWithSymbolsAndOnePairPerCompanion()
    {
        var origins = OriginData.FromJson(File.ReadAllText(Content("characters", "origens.json")));
        var companions = Cast.Values.Where(m => m.Kind == CastKind.Companion).Select(m => m.Id).ToList();

        Assert.Equal(4, origins.Count);
        Assert.Equal(6, companions.Count);
        Assert.Empty(OriginData.LinkErrors(origins, companions));
        Assert.All(origins, o => Assert.Equal(3, o.Companions.Count));
        Assert.All(origins, o => Assert.True(File.Exists(FromRes(o.Symbol)), o.Symbol));
    }

    [Fact]
    public void Origins_LinkErrorsCatchRepeatedPairAndUnknownCompanion()
    {
        var origins = new[]
        {
            new Origin("a", "", "", "", "", "", new[] { "x", "y" }),
            new Origin("b", "", "", "", "", "", new[] { "x", "y", "z" }),
        };

        var errors = OriginData.LinkErrors(origins, new[] { "x", "y" });

        Assert.Contains(errors, e => e.Contains("mesmo par"));
        Assert.Contains(errors, e => e.Contains("'z'"));
    }

    [Fact]
    public void Shades_AttackGesturesExistAndLoad()
    {
        foreach (var file in Directory.GetFiles(Content("shades"), "*.json"))
        {
            var json = File.ReadAllText(file);
            CombatData.ShadeResolveFromJson(json);
            foreach (var attack in CombatData.ShadeAttacksFromJson(json))
            {
                var gesture = File.ReadAllText(Content("gestures", attack.Gesture + ".json"));
                if (attack.Kind == ShadeAttackKind.Pattern)
                {
                    GestureData.DodgeFromJson(gesture);
                }
                else
                {
                    GestureData.RingFromJson(gesture);
                }
            }

            // Sombras nomeadas trazem escuta e libertação; as comuns podem não ter.
            if (json.Contains("\"listen\""))
            {
                ShadeData.ListeningFromJson(json);
            }

            if (json.Contains("\"liberation\""))
            {
                ShadeData.LiberationFromJson(json);
            }

            if (json.Contains("\"absorb\""))
            {
                var echo = ShadeData.AbsorbFromJson(json).Echo;
                GestureData.RingFromJson(File.ReadAllText(Content("gestures", echo.Gesture + ".json")));
            }
        }
    }

    [Fact]
    public void Difficulty_BagAndEscapeLoad()
    {
        foreach (var file in Directory.GetFiles(Content("difficulty"), "*.json"))
        {
            var json = File.ReadAllText(file);
            BagData.AssistFromDifficulty(json);
            Assert.InRange(BagData.DefeatTideSteps(json), 0, 2);
        }

        // A bolsa de teste só cita provisões que existem, e as regras de oferendas e de cura carregam.
        var inventory = new InventoryState();
        BagData.FillFromJson(File.ReadAllText(Content("items", "bolsa_teste.json")), inventory);
        var bag = new BattleBag(inventory, BagData.ProvisionsFromJson(File.ReadAllText(Content("items", "provisoes.json"))));
        Assert.True(bag.HasAnyOffering);
        Assert.True(bag.HasAnyProvision);
        Assert.All(inventory.Provisions.Keys, id => Assert.Contains(bag.Provisions, p => p.Id == id));
        var offerings = BagData.OfferingRulesFromJson(File.ReadAllText(Content("rules", "oferendas.json")));
        Assert.All(inventory.Offerings.Keys, k => Assert.Contains(k, offerings.Kinds));
        BagData.HealingRulesFromJson(File.ReadAllText(Content("rules", "cura.json")));
        Assert.True(MarkData.FromJson(File.ReadAllText(Content("rules", "marca.json"))).MarkOfOffering(offerings.BloodKind) > 0);
        BagData.EscapeFromJson(File.ReadAllText(Content("gestures", "fuga.json")));
    }

    [Fact]
    public void Shades_StoryLoadsAndPhaseGesturesExist()
    {
        foreach (var file in Directory.GetFiles(Content("shades"), "*.json"))
        {
            var json = File.ReadAllText(file);
            if (!json.Contains("\"story_keys\""))
            {
                continue;
            }

            var story = ShadeData.StoryFromJson(json);
            foreach (var attack in story.Phases.SelectMany(p => p.Attacks))
            {
                Assert.True(File.Exists(Content("gestures", attack.Gesture + ".json")), $"Gesto '{attack.Gesture}' não existe.");
            }
        }
    }

    [Fact]
    public void Gestures_AllLoadByKind()
    {
        foreach (var file in Directory.GetFiles(Content("gestures"), "*.json"))
        {
            var json = File.ReadAllText(file);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            switch (doc.RootElement.GetProperty("gesture").GetString())
            {
                case "ring": GestureData.RingFromJson(json); break;
                case "dodge": GestureData.DodgeFromJson(json); break;
                case "hold": GestureData.HoldFromJson(json); break;
                case "drag": GestureData.DragFromJson(json); break;
                case var other: Assert.Fail($"{Path.GetFileName(file)}: gesto '{other}' desconhecido."); break;
            }
        }
    }

    [Theory]
    [MemberData(nameof(BattleFiles))]
    public void Battle_ShadeFileExists(string file)
    {
        if (CombatData.ShadeIdFromJson(File.ReadAllText(Content("battle", file))) is { } shade)
        {
            Assert.True(File.Exists(Content("shades", shade + ".json")), $"Sombra '{shade}' não existe.");
        }
    }

    [Fact]
    public void Content_PassesLoadValidation()
    {
        // A mesma validação que o jogo faz ao abrir (DADOS.md 9): chaves de texto, condições, ids citados.
        using var locale = JsonDocument.Parse(File.ReadAllText(Content("locale", "pt_BR.json")));
        var keys = locale.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();
        var files = Directory.GetFiles(Content(), "*.json", SearchOption.AllDirectories)
            .ToDictionary(f => Path.GetRelativePath(Content(), f).Replace('\\', '/'), File.ReadAllText);

        var report = ContentValidator.Validate(files, keys, release: false);

        Assert.True(report.Ok, string.Join("\n", report.Errors));
    }

    private static string FromRes(string path) => Path.Combine(Root, path["res://".Length..]);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "project.godot")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("project.godot não encontrado.");
    }
}
