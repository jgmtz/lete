using System.Text.Json;
using Lete.Core.Systems;

namespace Lete.Core.Data;

/// <summary>Carrega os números da Maré (<c>content/rules/mare.json</c>).</summary>
public static class TideData
{
    public static TideRules FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var rules = new TideRules(Read(root, "milestone_step"), Read(root, "travel_step"));

        var errors = rules.Validate().ToList();
        return errors.Count == 0 ? rules : throw new FormatException(string.Join(" ", errors));
    }

    /// <summary>Frações em decimal (0,1 somado dez vezes dá 1 exato, e o degrau inteiro não escorrega).</summary>
    private static decimal Read(JsonElement root, string property) => Math.Round((decimal)TbdNumber.Read(root, property), 4);
}
