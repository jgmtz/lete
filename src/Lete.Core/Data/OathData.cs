using System.Text.Json;
using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Systems;

namespace Lete.Core.Data;

/// <summary>
/// Lê um juramento (<c>content/oaths/&lt;id&gt;.json</c>): <c>id</c>, as chaves de texto (<c>name_key</c>,
/// <c>promise_key</c>, <c>blessing_key</c>, <c>heel_key</c>), <c>breaks_on</c> (vias), <c>kept_when</c> e
/// <c>expires_when</c> (condições, opcionais) e os efeitos <c>blessing</c>/<c>heel</c> {<c>lucidity_max</c>,
/// <c>echoes_blocked</c>}; e as regras de <c>content/rules/juramentos.json</c>.
/// </summary>
public static class OathData
{
    private static readonly HashSet<string> EffectFields = new(StringComparer.Ordinal) { "lucidity_max", "echoes_blocked" };

    public static Oath FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var id = String(root, "id");
        var breaksOn = root.TryGetProperty("breaks_on", out var list)
            ? list.EnumerateArray().Select(v => ViaOf(id, v.GetString() ?? "")).ToList()
            : new List<Via>();

        return new Oath(
            id,
            String(root, "name_key"),
            String(root, "promise_key"),
            String(root, "blessing_key"),
            String(root, "heel_key"),
            breaksOn,
            root.TryGetProperty("kept_when", out var kept) ? ConditionParser.Parse(kept, $"{id}.kept_when") : null,
            root.TryGetProperty("expires_when", out var expires) ? ConditionParser.Parse(expires, $"{id}.expires_when") : null,
            Effects(id, root, "blessing"),
            Effects(id, root, "heel"));
    }

    public static OathRules RulesFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var rules = new OathRules((int)TbdNumber.Read(doc.RootElement, "max_active"));
        var errors = rules.Validate().ToList();
        return errors.Count == 0 ? rules : throw new FormatException(string.Join(" ", errors));
    }

    private static OathEffects Effects(string id, JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var effects))
        {
            return OathEffects.None;
        }

        var unknown = effects.EnumerateObject().Select(p => p.Name).FirstOrDefault(n => !EffectFields.Contains(n) && !n.StartsWith('_'));
        if (unknown is not null)
        {
            throw new FormatException($"Juramento '{id}': efeito '{unknown}' desconhecido em '{property}' (use {string.Join(", ", EffectFields)}).");
        }

        return new OathEffects(
            effects.TryGetProperty("lucidity_max", out _) ? TbdNumber.Read(effects, "lucidity_max") : 0f,
            effects.TryGetProperty("echoes_blocked", out var blocked) && blocked.GetBoolean());
    }

    private static Via ViaOf(string id, string text)
    {
        foreach (var via in Enum.GetValues<Via>())
        {
            if (ConditionParser.SnakeCase(via) == text)
            {
                return via;
            }
        }

        throw new FormatException($"Juramento '{id}': via '{text}' desconhecida em 'breaks_on'.");
    }

    private static string String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new FormatException($"Campo de texto '{property}' ausente.");
}
