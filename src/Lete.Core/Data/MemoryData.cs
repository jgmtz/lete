using System.Text.Json;
using Lete.Core.Combat;
using Lete.Core.Combat.Gestures;

namespace Lete.Core.Data;

/// <summary>Carrega as Memórias (habilidades de combate) de <c>content/memories/&lt;id&gt;.json</c>.</summary>
public static class MemoryData
{
    private static readonly Dictionary<string, StrokeDirection> Directions = new()
    {
        ["up"] = StrokeDirection.Up,
        ["up_right"] = StrokeDirection.UpRight,
        ["right"] = StrokeDirection.Right,
        ["down_right"] = StrokeDirection.DownRight,
        ["down"] = StrokeDirection.Down,
        ["down_left"] = StrokeDirection.DownLeft,
        ["left"] = StrokeDirection.Left,
        ["up_left"] = StrokeDirection.UpLeft,
    };

    private static readonly Dictionary<string, RecollectionKind> Kinds = new()
    {
        ["childhood"] = RecollectionKind.Childhood,
        ["feast"] = RecollectionKind.Feast,
        ["family"] = RecollectionKind.Family,
        ["mourning"] = RecollectionKind.Mourning,
        ["craft"] = RecollectionKind.Craft,
    };

    private static readonly Dictionary<string, MemoryEffectKind> Effects = new()
    {
        ["heal"] = MemoryEffectKind.Heal,
        ["strike"] = MemoryEffectKind.Strike,
        ["guard"] = MemoryEffectKind.Guard,
        ["slow"] = MemoryEffectKind.Slow,
        ["focus"] = MemoryEffectKind.Focus,
    };

    /// <summary>Campo <c>memories</c>: lista de Memórias com Traço, custo e efeito.</summary>
    public static IReadOnlyList<MemorySkill> FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("memories", out var list))
        {
            throw new FormatException("Campo 'memories' ausente.");
        }

        var skills = list.EnumerateArray().Select(m =>
        {
            var stroke = Required(m, "stroke");
            var power = Required(m, "power");
            return new MemorySkill(
                String(m, "id"),
                Lookup(Kinds, String(m, "kind"), "tipo de Lembrança"),
                String(m, "name_key"),
                String(m, "description_key"),
                new StrokePattern(
                    Required(stroke, "steps").EnumerateArray()
                        .Select(s => Lookup(Directions, s.GetString() ?? "", "direção"))
                        .ToList(),
                    TbdNumber.Read(stroke, "beat_seconds"),
                    TbdNumber.Read(stroke, "zero_score_offset_seconds"),
                    TbdNumber.Read(stroke, "lead_in_seconds")),
                TbdNumber.Read(m, "lucidity_cost"),
                TbdNumber.Read(m, "failed_cost_multiplier"),
                Lookup(Effects, String(m, "effect"), "efeito"),
                new GradeValues(TbdNumber.Read(power, "perfect"), TbdNumber.Read(power, "good"), TbdNumber.Read(power, "failed")));
        }).ToList();

        var errors = skills.SelectMany(s => s.Validate()).ToList();
        if (skills.GroupBy(s => s.Id).Any(g => g.Count() > 1))
        {
            errors.Add("Duas Memórias com o mesmo id.");
        }

        return errors.Count == 0 ? skills : throw new FormatException(string.Join(" ", errors));
    }

    private static T Lookup<T>(Dictionary<string, T> table, string value, string what) =>
        table.TryGetValue(value, out var result)
            ? result
            : throw new FormatException($"{what} '{value}' desconhecido (use {string.Join(", ", table.Keys)}).");

    private static JsonElement Required(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value : throw new FormatException($"Campo '{property}' ausente.");

    private static string String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new FormatException($"Campo de texto '{property}' ausente.");
}
