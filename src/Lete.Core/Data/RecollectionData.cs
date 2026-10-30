using System.Text.Json;
using Lete.Core.Conditions;
using Lete.Core.Recollections;

namespace Lete.Core.Data;

/// <summary>
/// Lê uma Lembrança (<c>content/recollections/&lt;id&gt;.json</c>, DADOS.md 3) e as regras do Mosaico
/// (<c>content/rules/mosaico.json</c>).
/// </summary>
public static class RecollectionData
{
    private static readonly Dictionary<string, SourceType> SourceTypes = new()
    {
        ["free_shade"] = SourceType.FreeShade,
        ["shared_offering"] = SourceType.SharedOffering,
        ["anchor"] = SourceType.Anchor,
        ["dream"] = SourceType.Dream,
        ["mnemosyne"] = SourceType.Mnemosyne,
        ["dialogue"] = SourceType.Dialogue,
    };

    private static readonly Dictionary<string, RecollectionStage> Stages = new()
    {
        ["fragment"] = RecollectionStage.Fragment,
        ["recalled"] = RecollectionStage.Recalled,
        ["full"] = RecollectionStage.Full,
    };

    public static Recollection FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = String(root, "id");
        var (scope, originId) = String(root, "scope") switch
        {
            "common" => (RecollectionScope.Common, (string?)null),
            "heavy" => (RecollectionScope.Heavy, null),
            var s when s.StartsWith("origin:", StringComparison.Ordinal) && s.Length > "origin:".Length =>
                (RecollectionScope.Origin, s["origin:".Length..]),
            var other => throw new FormatException($"Lembrança '{id}': scope '{other}' desconhecido (common, origin:<id>, heavy)."),
        };

        var texts = new Dictionary<RecollectionStage, string>();
        if (root.TryGetProperty("stages", out var stages))
        {
            foreach (var stage in stages.EnumerateObject())
            {
                texts[Lookup(Stages, stage.Name, "estágio")] = String(stage.Value, "text_key");
            }
        }

        var sources = root.TryGetProperty("sources", out var list)
            ? list.EnumerateArray().Select(s => new RecollectionSource(
                String(s, "id"),
                Lookup(SourceTypes, String(s, "type"), "tipo de fonte"),
                OptionalString(s, "shade"),
                OptionalString(s, "kind"),
                !s.TryGetProperty("reliable", out var reliable) || reliable.GetBoolean(),
                s.TryGetProperty("contradicts", out var contradicts) && contradicts.GetBoolean())).ToList()
            : new List<RecollectionSource>();

        foreach (var source in sources)
        {
            if (source.Type is SourceType.FreeShade or SourceType.SharedOffering && source.Shade is null)
            {
                throw new FormatException($"Lembrança '{id}': a fonte '{source.Id}' precisa de 'shade'.");
            }
        }

        if (sources.Select(s => s.Id).Distinct().Count() != sources.Count)
        {
            throw new FormatException($"Lembrança '{id}': ids de fonte repetidos.");
        }

        var heavy = scope == RecollectionScope.Heavy ? HeavyFromJson(id, root, texts) : null;
        var position = root.TryGetProperty("mosaic_position", out var p) ? p : default;
        return new Recollection(
            id,
            Lookup(MemoryData.Kinds, String(root, "kind"), "tipo de Lembrança"),
            scope,
            originId,
            String(root, "title_key"),
            texts,
            OptionalString(root, "skill"),
            sources,
            root.TryGetProperty("full_by", out var acts) ? acts.EnumerateArray().Select(a => a.GetString()!).ToList() : new List<string>(),
            position.ValueKind == JsonValueKind.Object ? position.GetProperty("x").GetInt32() : 0,
            position.ValueKind == JsonValueKind.Object ? position.GetProperty("y").GetInt32() : 0,
            heavy);
    }

    /// <summary>
    /// A parte da Pesada: <c>order</c>, <c>gate</c> (condição, opcional), <c>warning_key</c>, <c>surface_key</c> e
    /// <c>leave</c> {<c>text_key</c>, <c>lucidity_max</c>, <c>protections</c>}. A verdade é <c>stages.full.text_key</c>
    /// e a Culpa é o <c>skill</c> da raiz; os dois são obrigatórios.
    /// </summary>
    private static HeavyMemory HeavyFromJson(string id, JsonElement root, IReadOnlyDictionary<RecollectionStage, string> texts)
    {
        if (!texts.ContainsKey(RecollectionStage.Full) || OptionalString(root, "skill") is null)
        {
            throw new FormatException($"Pesada '{id}': precisa de 'stages.full.text_key' (a verdade) e 'skill' (a Culpa).");
        }

        var order = root.TryGetProperty("order", out var o) && o.ValueKind == JsonValueKind.Number
            ? o.GetInt32()
            : throw new FormatException($"Pesada '{id}': falta 'order' (lugar na fila fixa).");
        var leave = root.TryGetProperty("leave", out var l) ? l : throw new FormatException($"Pesada '{id}': falta 'leave'.");
        var lightness = new Lightness(TbdNumber.Read(leave, "lucidity_max"), (int)TbdNumber.Read(leave, "protections"));
        if (order < 1 || lightness.LucidityMax < 0f || lightness.Protections < 0)
        {
            throw new FormatException($"Pesada '{id}': 'order' começa em 1 e a Leveza não pode ser negativa.");
        }

        return new HeavyMemory(
            order,
            root.TryGetProperty("gate", out var gate) ? ConditionParser.Parse(gate, $"{id}.gate") : null,
            String(root, "warning_key"),
            String(root, "surface_key"),
            String(leave, "text_key"),
            lightness);
    }

    public static MosaicRules RulesFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var power = doc.RootElement.GetProperty("stage_power");
        var rules = new MosaicRules(
            (int)TbdNumber.Read(doc.RootElement, "key_kinds"),
            Stages.ToDictionary(s => s.Value, s => TbdNumber.Read(power, s.Key)));
        var errors = rules.Validate().ToList();
        return errors.Count == 0 ? rules : throw new FormatException(string.Join(" ", errors));
    }

    /// <summary>Cor da pedra por tipo (hex), de <c>stone_colors</c>; cada uma pode vir como <c>_tbd</c>.</summary>
    public static IReadOnlyDictionary<Combat.RecollectionKind, string> StoneColorsFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var colors = doc.RootElement.GetProperty("stone_colors");
        return MemoryData.Kinds.ToDictionary(
            k => k.Value,
            k => colors.TryGetProperty(k.Key, out var c)
                ? (c.ValueKind == JsonValueKind.Object ? c.GetProperty("value") : c).GetString()!
                : throw new FormatException($"Falta a cor da pedra de '{k.Key}'."));
    }

    private static T Lookup<T>(Dictionary<string, T> table, string value, string what) =>
        table.TryGetValue(value, out var result)
            ? result
            : throw new FormatException($"{what} '{value}' desconhecido (use {string.Join(", ", table.Keys)}).");

    private static string? OptionalString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string String(JsonElement element, string property) =>
        OptionalString(element, property) ?? throw new FormatException($"Campo de texto '{property}' ausente.");
}
