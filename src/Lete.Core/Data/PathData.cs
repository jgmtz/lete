using System.Text.Json;
using Lete.Core.Conditions;
using Lete.Core.Systems;

namespace Lete.Core.Data;

/// <summary>Lê uma chave do Caminho (<c>content/path/&lt;id&gt;.json</c>): <c>id</c>, <c>journal_key</c> e <c>ways</c> [{<c>id</c>, <c>condition</c>}].</summary>
public static class PathData
{
    public static PathKey FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = String(root, "id");
        var ways = root.TryGetProperty("ways", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Select((w, i) => new PathWay(
                String(w, "id"),
                w.TryGetProperty("condition", out var condition)
                    ? ConditionParser.Parse(condition, $"{id}.ways[{i}].condition")
                    : throw new FormatException($"Chave '{id}': a forma {i} não tem 'condition'."))).ToList()
            : throw new FormatException($"Chave '{id}': falta a lista 'ways'.");

        if (ways.Count == 0 || ways.Select(w => w.Id).Distinct().Count() != ways.Count)
        {
            throw new FormatException($"Chave '{id}': precisa de pelo menos uma forma, sem ids repetidos.");
        }

        return new PathKey(id, String(root, "journal_key"), ways);
    }

    private static string String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new FormatException($"Campo de texto '{property}' ausente.");
}
