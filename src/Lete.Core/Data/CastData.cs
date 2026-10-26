using System.Text.Json;
using Lete.Core.Combat;

namespace Lete.Core.Data;

/// <summary>Carrega o registro do elenco (<c>content/characters/elenco.json</c>): uma entrada por pessoa.</summary>
public static class CastData
{
    public static IReadOnlyDictionary<string, CastMember> FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("cast", out var list))
        {
            throw new FormatException("Campo 'cast' ausente.");
        }

        var members = list.EnumerateArray().Select(m => new CastMember(
            String(m, "id"),
            String(m, "name_key"),
            String(m, "kind") switch
            {
                "protagonist" => CastKind.Protagonist,
                "companion" => CastKind.Companion,
                "shade" => CastKind.Shade,
                var other => throw new FormatException($"Papel '{other}' desconhecido (use protagonist, companion ou shade)."),
            },
            Optional(m, "gender"),
            TbdNumber.Read(m, "height"),
            Optional(m, "battle_front"),
            Optional(m, "battle_back"),
            Optional(m, "portrait"),
            Optional(m, "map_folder"),
            Optional(m, "tint"),
            m.TryGetProperty("idle_frames", out var idle) ? BattleData.ReadIdle(idle) : null,
            m.TryGetProperty("selectable", out var selectable) && selectable.GetBoolean(),
            Optional(m, "front_faces") switch
            {
                null or "left" => false,
                "right" => true,
                var other => throw new FormatException($"'front_faces' = '{other}' desconhecido (use left ou right)."),
            })).ToList();

        var errors = new List<string>();
        foreach (var group in members.GroupBy(m => m.Id).Where(g => g.Count() > 1))
        {
            errors.Add($"'{group.Key}' aparece mais de uma vez no elenco.");
        }

        foreach (var m in members.Where(m => m.FrontArt is null && m.BackArt is null))
        {
            errors.Add($"'{m.Id}' não tem arte de batalha.");
        }

        foreach (var m in members.Where(m => m.Selectable && m.FrontArt is null))
        {
            errors.Add($"'{m.Id}' aparece na escolha de personagem, mas não tem arte de frente (o busto sai dela).");
        }

        return errors.Count == 0
            ? members.ToDictionary(m => m.Id)
            : throw new FormatException(string.Join(" ", errors));
    }

    /// <summary>Lista de ids para percorrer (campo <c>cast_cycle</c> de um teste de batalha); vazia se não houver.</summary>
    public static IReadOnlyList<string> CycleFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("cast_cycle", out var cycle)
            ? cycle.EnumerateArray().Select(c => c.GetString() ?? "").ToList()
            : Array.Empty<string>();
    }

    private static string? Optional(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string String(JsonElement element, string property) =>
        Optional(element, property) ?? throw new FormatException($"Campo de texto '{property}' ausente.");
}
