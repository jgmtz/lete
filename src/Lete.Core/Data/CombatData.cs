using System.Text.Json;
using Lete.Core.Combat;

namespace Lete.Core.Data;

/// <summary>Quem faz o papel do protagonista numa batalha e os máximos de Fôlego e Lucidez.</summary>
public sealed record ProtagonistStats(string Id, float Breath, float Lucidity);

/// <summary>Carrega números de combate dos arquivos de <c>content/</c> (todos podem ser <c>_tbd</c>).</summary>
public static class CombatData
{
    /// <summary>Firmeza máxima de uma sombra (<c>content/shades/&lt;id&gt;.json</c>, campo <c>resolve</c>).</summary>
    public static float ShadeResolveFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return TbdNumber.Read(doc.RootElement, "resolve");
    }

    /// <summary>Firmeza que um ataque tira por resultado (campo <c>resolve_damage</c> com perfect/good/failed).</summary>
    public static GradeValues ResolveDamageFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return ReadGradeValues(doc.RootElement, "resolve_damage");
    }

    /// <summary>Ciclo de golpes da sombra (campo <c>attacks</c> de <c>content/shades/&lt;id&gt;.json</c>).</summary>
    public static IReadOnlyList<ShadeAttack> ShadeAttacksFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("attacks", out var attacks) || attacks.GetArrayLength() == 0)
        {
            throw new FormatException("Campo 'attacks' ausente ou vazio.");
        }

        return ReadAttacks(attacks);
    }

    /// <summary>Uma lista de golpes (o ciclo da sombra ou o de uma fase).</summary>
    internal static IReadOnlyList<ShadeAttack> ReadAttacks(JsonElement attacks)
    {
        return attacks.EnumerateArray()
            .Select(a => new ShadeAttack(
                String(a, "kind") switch
                {
                    "direct" => ShadeAttackKind.Direct,
                    "pattern" => ShadeAttackKind.Pattern,
                    var other => throw new FormatException($"Tipo de golpe '{other}' desconhecido (use direct ou pattern)."),
                },
                String(a, "gesture"),
                String(a, "caption_key"),
                ReadGradeValues(a, "breath_damage")))
            .ToList();
    }

    /// <summary>Campo <c>protagonist</c> de <c>content/battle/&lt;id&gt;.json</c>.</summary>
    public static ProtagonistStats ProtagonistFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("protagonist", out var p))
        {
            throw new FormatException("Campo 'protagonist' ausente.");
        }

        return new ProtagonistStats(String(p, "id"), TbdNumber.Read(p, "breath"), TbdNumber.Read(p, "lucidity"));
    }

    /// <summary>Companheiro da batalha (campo opcional <c>companion</c> de <c>content/battle/&lt;id&gt;.json</c>).</summary>
    public static string? CompanionIdFromJson(string json) => OptionalString(json, "companion");

    /// <summary>Sombra da batalha (campo opcional <c>shade</c>: arquivo em <c>content/shades/</c>).</summary>
    public static string? ShadeIdFromJson(string json) => OptionalString(json, "shade");

    private static string? OptionalString(string json, string property)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static GradeValues ReadGradeValues(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var values))
        {
            throw new FormatException($"Campo '{property}' ausente.");
        }

        return new GradeValues(
            TbdNumber.Read(values, "perfect"),
            TbdNumber.Read(values, "good"),
            TbdNumber.Read(values, "failed"));
    }

    private static string String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new FormatException($"Campo de texto '{property}' ausente.");
}
