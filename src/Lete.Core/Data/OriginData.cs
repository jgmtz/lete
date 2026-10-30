using System.Text.Json;
using Lete.Core.State;

namespace Lete.Core.Data;

/// <summary>Carrega as origens (<c>content/characters/origens.json</c>), na ordem das cartas da escolha.</summary>
public static class OriginData
{
    public static IReadOnlyList<Origin> FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("origins", out var list))
        {
            throw new FormatException("Campo 'origins' ausente.");
        }

        var origins = list.EnumerateArray().Select(o => new Origin(
            String(o, "id"),
            String(o, "name_key"),
            String(o, "craft_key"),
            String(o, "ambition_key"),
            String(o, "who_key"),
            String(o, "symbol"),
            o.TryGetProperty("companions", out var companions)
                ? companions.EnumerateArray().Select(c => c.GetString() ?? "").ToList()
                : new List<string>())).ToList();

        var repeated = origins.GroupBy(o => o.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        return repeated.Count == 0
            ? origins
            : throw new FormatException($"Origem repetida: {string.Join(", ", repeated)}.");
    }

    /// <summary>
    /// Confere a regra das ligações (HISTORIA 5): cada companheiro ligado a exatamente duas origens, um par diferente
    /// para cada um. Devolve os erros (vazio se estiver certo).
    /// </summary>
    public static IReadOnlyList<string> LinkErrors(IReadOnlyList<Origin> origins, IEnumerable<string> companionIds)
    {
        var errors = new List<string>();
        var pairs = new Dictionary<string, string>();
        foreach (var id in companionIds)
        {
            var linked = origins.Where(o => o.Companions.Contains(id)).Select(o => o.Id).OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (linked.Count != 2)
            {
                errors.Add($"'{id}' está ligado a {linked.Count} origens (devem ser 2).");
                continue;
            }

            var pair = string.Join("+", linked);
            if (pairs.TryGetValue(pair, out var other))
            {
                errors.Add($"'{id}' e '{other}' têm o mesmo par de origens ({pair}).");
            }

            pairs[pair] = id;
        }

        var known = companionIds.ToHashSet();
        errors.AddRange(origins.SelectMany(o => o.Companions.Where(c => !known.Contains(c)).Select(c => $"'{o.Id}' cita o companheiro desconhecido '{c}'.")));
        return errors;
    }

    private static string String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new FormatException($"Campo de texto '{property}' ausente.");
}
