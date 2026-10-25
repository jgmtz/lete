using System.Text.Json;
using Lete.Core.Combat;
using Lete.Core.Data;

namespace Lete.Core.Tests;

/// <summary>
/// Confere o conteúdo inteiro de <c>content/</c>: tudo carrega, e toda arte, gesto, companheiro e chave de
/// texto referenciados existem. Pega erro de dado sem precisar abrir o jogo.
/// </summary>
public class ContentTests
{
    private static readonly string Root = RepoRoot();
    private static string Content(params string[] parts) => Path.Combine(new[] { Root, "content" }.Concat(parts).ToArray());

    public static IEnumerable<object[]> BattleFiles() =>
        Directory.GetFiles(Path.Combine(RepoRoot(), "content", "battle"), "*.json")
            .Where(f => Path.GetFileName(f) != "hud_layouts.json")
            .Select(f => new object[] { Path.GetFileName(f) });

    [Theory]
    [MemberData(nameof(BattleFiles))]
    public void Battle_ArtExistsInEveryStyle(string file)
    {
        var setup = BattleData.FromJson(File.ReadAllText(Content("battle", file)));
        var missing = (
            from style in setup.Styles
            from actor in setup.Party.Concat(setup.Enemies)
            let path = Path.Combine(Root, "art", "sprites", "batalha", style.Folder, actor.Art + ".png")
            where !File.Exists(path)
            select $"{style.Folder}/{actor.Art}.png").ToList();
        missing.AddRange(setup.Party.Concat(setup.Enemies)
            .Where(a => a.Portrait is not null && !File.Exists(FromRes(a.Portrait)))
            .Select(a => a.Portrait!));

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

        var setup = BattleData.FromJson(json);
        Assert.Null(setup.ProtagonistSlotError(CombatData.ProtagonistFromJson(json).Id));
        if (CombatData.CompanionIdFromJson(json) is { } companion)
        {
            var support = CombatData.CompanionFromJson(File.ReadAllText(Content("companions", companion + ".json")));
            Assert.Contains(setup.Party, p => p.Id == support.CompanionId);
        }
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
    public void Locale_HasEveryKeyTheContentUses()
    {
        using var locale = JsonDocument.Parse(File.ReadAllText(Content("locale", "pt_BR.json")));
        var keys = locale.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet();

        var missing = Directory.GetFiles(Content(), "*.json", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(Path.GetDirectoryName(f)) != "locale")
            .SelectMany(f =>
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(f));
                return KeyReferences(doc.RootElement).Select(k => (File: Path.GetFileName(f), Key: k)).ToList();
            })
            .Where(r => !keys.Contains(r.Key))
            .Select(r => $"{r.File}: {r.Key}")
            .ToList();

        Assert.Empty(missing);
    }

    /// <summary>Valores de campos terminados em <c>_key</c> (ou listas <c>_keys</c>), em qualquer profundidade.</summary>
    private static IEnumerable<string> KeyReferences(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray().SelectMany(KeyReferences);
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return Enumerable.Empty<string>();
        }

        return element.EnumerateObject().SelectMany(p =>
            p.Name.EndsWith("_key") && p.Value.ValueKind == JsonValueKind.String ? new[] { p.Value.GetString()! }
            : p.Name.EndsWith("_keys") && p.Value.ValueKind == JsonValueKind.Array
                ? p.Value.EnumerateArray().Select(v => v.GetString()!)
            : KeyReferences(p.Value));
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
