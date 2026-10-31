using System.Text.Json;
using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Systems;

namespace Lete.Core.Data;

/// <summary>
/// Lê um companheiro (<c>content/companions/&lt;id&gt;.json</c>): <c>id</c>, <c>name_key</c>, <c>support</c> {<c>id</c>,
/// <c>effect</c> (snake_case de <see cref="SupportEffect"/>), <c>power</c>, <c>found_obols</c> (opcional)},
/// <c>approval</c> (via → approve | tolerate | reprove; o que falta é tolerado), <c>reluctant_key</c> e
/// <c>talk_keys</c>; e as regras do vínculo de <c>content/rules/companheiro.json</c>.
/// </summary>
public static class CompanionData
{
    public static Companion FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = String(root, "id");
        if (!root.TryGetProperty("support", out var support))
        {
            throw new FormatException($"Companheiro '{id}': campo 'support' ausente.");
        }

        var approvals = new Dictionary<Via, Approval>();
        if (root.TryGetProperty("approval", out var approval))
        {
            foreach (var entry in approval.EnumerateObject().Where(p => !p.Name.StartsWith('_')))
            {
                approvals[EnumOf<Via>(id, entry.Name)] = EnumOf<Approval>(id, entry.Value.GetString() ?? "");
            }
        }

        var companion = new Companion(
            id,
            String(root, "name_key"),
            String(support, "id"),
            EnumOf<SupportEffect>(id, String(support, "effect")),
            TbdNumber.Read(support, "power"),
            support.TryGetProperty("found_obols", out _) ? (int)TbdNumber.Read(support, "found_obols") : 0,
            approvals,
            String(root, "reluctant_key"),
            root.TryGetProperty("talk_keys", out var talks)
                ? talks.EnumerateArray().Select(t => t.GetString() ?? "").ToList()
                : new List<string>());
        var errors = companion.Validate().ToList();
        return errors.Count == 0 ? companion : throw new FormatException($"Companheiro '{id}': {string.Join(" ", errors)}");
    }

    public static CompanionRules RulesFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var gains = new Dictionary<BondSource, int>();
        if (root.TryGetProperty("bond_gain", out var gain))
        {
            foreach (var entry in gain.EnumerateObject().Where(p => !p.Name.StartsWith('_')))
            {
                gains[EnumOf<BondSource>("companheiro", entry.Name)] = (int)TbdNumber.Read(gain, entry.Name);
            }
        }

        var rules = new CompanionRules(
            (int)TbdNumber.Read(root, "bond_max"),
            (int)TbdNumber.Read(root, "fast_bond"),
            (int)TbdNumber.Read(root, "interval_turns"),
            (int)TbdNumber.Read(root, "fast_interval_turns"),
            TbdNumber.Read(root, "reluctant_factor"),
            gains);
        var errors = rules.Validate().ToList();
        return errors.Count == 0 ? rules : throw new FormatException(string.Join(" ", errors));
    }

    private static T EnumOf<T>(string id, string text) where T : struct, Enum
    {
        foreach (var value in Enum.GetValues<T>())
        {
            if (ConditionParser.SnakeCase(value) == text)
            {
                return value;
            }
        }

        throw new FormatException($"Companheiro '{id}': '{text}' desconhecido (use {string.Join(", ", Enum.GetValues<T>().Select(v => ConditionParser.SnakeCase(v)))}).");
    }

    private static string String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new FormatException($"Campo de texto '{property}' ausente.");
}
