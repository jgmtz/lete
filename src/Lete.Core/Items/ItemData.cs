using System.Text.Json;
using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Data;
using Lete.Core.Events;

namespace Lete.Core.Items;

/// <summary>
/// Lê um item (<c>content/items/&lt;id&gt;.json</c> com <c>category</c>) e um conjunto (<c>content/item_sets/&lt;id&gt;.json</c>).
/// Item: <c>id</c>, <c>category</c> (instrument | garment | amulet | votive | utensil | relic), <c>name_key</c>,
/// <c>story_key</c>, <c>affinity</c> (tipo de Lembrança), <c>set</c>, <c>unlocks_path</c> (relíquia: a chave do Caminho que abre) e <c>stages</c>
/// [{<c>id</c>: worn | recognized | awakened, <c>when</c> (não no primeiro), <c>story_key</c>, <c>modifiers</c>}].
/// Modificadores: <c>parry_window</c>, <c>dodge_box</c>, <c>breath_max</c>, <c>lucidity_max</c>.
/// </summary>
public static class ItemData
{
    private static readonly Dictionary<string, ItemCategory> Categories = new(StringComparer.Ordinal)
    {
        ["instrument"] = ItemCategory.Instrument,
        ["garment"] = ItemCategory.Garment,
        ["amulet"] = ItemCategory.Amulet,
        ["votive"] = ItemCategory.Votive,
        ["utensil"] = ItemCategory.Utensil,
        ["relic"] = ItemCategory.Relic,
    };

    private static readonly Dictionary<string, ItemStage> Stages = new(StringComparer.Ordinal)
    {
        ["worn"] = ItemStage.Worn,
        ["recognized"] = ItemStage.Recognized,
        ["awakened"] = ItemStage.Awakened,
    };

    private static readonly HashSet<string> ModifierFields = new(StringComparer.Ordinal) { "parry_window", "dodge_box", "breath_max", "lucidity_max" };

    /// <summary>Só os arquivos de <c>content/items</c> com <c>category</c> são itens (a pasta também tem a Bolsa de teste e as provisões).</summary>
    public static bool IsItem(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("category", out _);
    }

    public static Item FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = String(root, "id");
        var category = Lookup(Categories, String(root, "category"), id, "category");
        var stages = root.GetProperty("stages").EnumerateArray().Select((s, i) => new ItemStageDefinition(
            Lookup(Stages, String(s, "id"), id, $"stages[{i}].id"),
            s.TryGetProperty("when", out var when) ? ConditionParser.Parse(when, $"{id}.stages[{i}].when") : null,
            Optional(s, "story_key"),
            Modifiers(id, s))).ToList();

        if (stages.Count == 0 || stages[0].Stage != ItemStage.Worn || stages[0].When is not null
            || stages.Select(s => s.Stage).Zip(stages.Select(s => s.Stage).Skip(1)).Any(p => p.Second <= p.First)
            || stages.Skip(1).Any(s => s.When is null))
        {
            throw new FormatException($"Item '{id}': os estágios começam em 'worn' (sem condição), sobem em ordem e cada seguinte tem 'when'.");
        }

        var pathKey = Optional(root, "unlocks_path");
        if (pathKey is not null && category != ItemCategory.Relic)
        {
            throw new FormatException($"Item '{id}': só relíquia tem 'unlocks_path'.");
        }

        return new Item(id, category, String(root, "name_key"), String(root, "story_key"),
            Lookup(MemoryData.Kinds, String(root, "affinity"), id, "affinity"), Optional(root, "set"), stages, pathKey);
    }

    public static ItemSet SetFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = String(root, "id");
        var pieces = root.GetProperty("pieces").EnumerateArray().Select(p => p.GetString()!).ToList();
        if (pieces.Count < 2)
        {
            throw new FormatException($"Conjunto '{id}': precisa de pelo menos 2 peças.");
        }

        return new ItemSet(id, String(root, "name_key"), String(root, "story_key"), pieces, Modifiers(id, root));
    }

    private static ItemModifiers Modifiers(string id, JsonElement owner)
    {
        if (!owner.TryGetProperty("modifiers", out var m))
        {
            return ItemModifiers.None;
        }

        var unknown = m.EnumerateObject().Select(p => p.Name).FirstOrDefault(n => !ModifierFields.Contains(n) && !n.StartsWith('_'));
        if (unknown is not null)
        {
            throw new FormatException($"Item '{id}': modificador '{unknown}' desconhecido (use {string.Join(", ", ModifierFields)}).");
        }

        float Read(string field) => m.TryGetProperty(field, out _) ? TbdNumber.Read(m, field) : 0f;
        return new ItemModifiers(Read("parry_window"), Read("dodge_box"), Read("breath_max"), Read("lucidity_max"));
    }

    private static T Lookup<T>(IReadOnlyDictionary<string, T> table, string value, string id, string field) =>
        table.TryGetValue(value, out var result)
            ? result
            : throw new FormatException($"Item '{id}': '{field}' = '{value}' (use {string.Join(", ", table.Keys)}).");

    private static string? Optional(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string String(JsonElement element, string property) =>
        Optional(element, property) ?? throw new FormatException($"Campo de texto '{property}' ausente.");
}
