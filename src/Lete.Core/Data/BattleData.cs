using System.Numerics;
using System.Text.Json;
using Lete.Core.Combat;

namespace Lete.Core.Data;

/// <summary>Carrega uma configuração de batalha (<c>content/battle/&lt;id&gt;.json</c>).</summary>
public static class BattleData
{
    public static BattleSetup FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var layout = Required(root, "layout");

        var setup = new BattleSetup(
            new BattleLayout(
                ReadPositions(layout, "party"),
                ReadPositions(layout, "enemy"),
                layout.TryGetProperty("depth_scale", out _) ? TbdNumber.Read(layout, "depth_scale") : 0f),
            ReadActors(root, "party"),
            ReadActors(root, "enemy"),
            Required(root, "styles").EnumerateArray()
                .Select(s => new BattleArtStyle(
                    String(s, "id"),
                    String(s, "folder"),
                    String(s, "filter") switch
                    {
                        "linear" => true,
                        "nearest" => false,
                        var other => throw new FormatException($"Filtro '{other}' desconhecido (use linear ou nearest)."),
                    }))
                .ToList());

        var errors = setup.Layout.Validate().ToList();
        if (setup.Party.Count > setup.Layout.Party.Count || setup.Enemies.Count > setup.Layout.Enemies.Count)
        {
            errors.Add("Há mais participantes do que posições no layout.");
        }

        if (errors.Count > 0)
        {
            throw new FormatException(string.Join(" ", errors));
        }

        return setup;
    }

    private static List<Vector2> ReadPositions(JsonElement layout, string property) =>
        Required(layout, property).EnumerateArray()
            .Select(p => new Vector2(TbdNumber.Read(p, "x"), TbdNumber.Read(p, "y")))
            .ToList();

    private static List<BattleActorArt> ReadActors(JsonElement root, string property) =>
        Required(root, property).EnumerateArray()
            .Select(a => new BattleActorArt(
                String(a, "id"),
                String(a, "art"),
                TbdNumber.Read(a, "height"),
                a.TryGetProperty("flip_h", out var flip) && flip.GetBoolean(),
                a.TryGetProperty("tint", out var tint) ? tint.GetString() : null,
                a.TryGetProperty("portrait", out var portrait) ? portrait.GetString() : null,
                a.TryGetProperty("name_key", out var nameKey) ? nameKey.GetString() : null))
            .ToList();

    private static JsonElement Required(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value : throw new FormatException($"Campo '{property}' ausente.");

    private static string String(JsonElement element, string property) =>
        Required(element, property).GetString() ?? throw new FormatException($"Campo '{property}' vazio.");
}
