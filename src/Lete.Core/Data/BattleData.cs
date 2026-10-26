using System.Numerics;
using System.Text.Json;
using Lete.Core.Combat;

namespace Lete.Core.Data;

/// <summary>
/// Carrega uma configuração de batalha (<c>content/battle/&lt;id&gt;.json</c>). Cada participante cita alguém do elenco
/// (<c>{ "id": "lia", "pose": "back" }</c>, ver <see cref="CastData"/>) ou, em testes, traz a arte por extenso.
/// </summary>
public static class BattleData
{
    public static BattleSetup FromJson(string json, IReadOnlyDictionary<string, CastMember>? cast = null)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var layout = Required(root, "layout");

        var setup = new BattleSetup(
            new BattleLayout(
                ReadPositions(layout, "party"),
                ReadPositions(layout, "enemy"),
                layout.TryGetProperty("depth_scale", out _) ? TbdNumber.Read(layout, "depth_scale") : 0f),
            ReadActors(root, "party", cast),
            ReadActors(root, "enemy", cast),
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

    private static List<BattleActorArt> ReadActors(JsonElement root, string property, IReadOnlyDictionary<string, CastMember>? cast) =>
        Required(root, property).EnumerateArray()
            .Select(a => a.TryGetProperty("pose", out var pose) ? FromCast(a, pose, cast) : new BattleActorArt(
                String(a, "id"),
                String(a, "art"),
                TbdNumber.Read(a, "height"),
                a.TryGetProperty("flip_h", out var flip) && flip.GetBoolean(),
                a.TryGetProperty("tint", out var tint) ? tint.GetString() : null,
                a.TryGetProperty("portrait", out var portrait) ? portrait.GetString() : null,
                a.TryGetProperty("name_key", out var nameKey) ? nameKey.GetString() : null,
                a.TryGetProperty("idle_frames", out var idle) ? ReadIdle(idle) : null))
            .ToList();

    private static BattleActorArt FromCast(JsonElement entry, JsonElement pose, IReadOnlyDictionary<string, CastMember>? cast)
    {
        var id = String(entry, "id");
        if (cast is null || !cast.TryGetValue(id, out var member))
        {
            throw new FormatException($"'{id}' não está no elenco (content/characters/elenco.json).");
        }

        return pose.GetString() switch
        {
            "back" => member.AsActor(back: true),
            "front" => member.AsActor(back: false),
            var other => throw new FormatException($"Pose '{other}' desconhecida (use back ou front)."),
        };
    }

    internal static IdleAnimation ReadIdle(JsonElement idle)
    {
        var animation = new IdleAnimation(
            (int)TbdNumber.Read(idle, "frames"),
            TbdNumber.Read(idle, "fps"),
            Required(idle, "by_style").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString() ?? ""));
        return animation.Frames >= 1 && animation.Fps > 0f
            ? animation
            : throw new FormatException("Animação parada precisa de pelo menos 1 quadro e velocidade positiva.");
    }

    private static JsonElement Required(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value : throw new FormatException($"Campo '{property}' ausente.");

    private static string String(JsonElement element, string property) =>
        Required(element, property).GetString() ?? throw new FormatException($"Campo '{property}' vazio.");
}
