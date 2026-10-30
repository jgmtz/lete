using System.Text.Json;
using Lete.Core.Combat;
using Lete.Core.Combat.Gestures;
using Lete.Core.State;
using Lete.Core.Systems;

namespace Lete.Core.Data;

/// <summary>Bolsa de teste (<c>content/battle/bolsa_*.json</c>) e as regras do modo de dificuldade usadas no combate.</summary>
public static class BagData
{
    /// <summary>As provisões do jogo (<c>content/items/provisoes.json</c>, campo <c>provisions</c>).</summary>
    public static IReadOnlyList<ProvisionDefinition> ProvisionsFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return Required(doc.RootElement, "provisions").EnumerateArray().Select(p => new ProvisionDefinition(
            String(p, "id"),
            String(p, "name_key"),
            String(p, "description_key"),
            TbdNumber.Read(p, "breath_restore"),
            (int)TbdNumber.Read(p, "mark_gain"))).ToList();
    }

    /// <summary>
    /// Bolsa de teste (<c>content/items/bolsa_teste.json</c>): soma <c>obols</c>, <c>offerings</c> (tipo → quantidade) e
    /// <c>provisions</c> (id → quantidade) à Bolsa do estado. Só o teste de batalha e o F1 a usam.
    /// </summary>
    public static void FillFromJson(string json, InventoryState inventory)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        inventory.Obols += (int)TbdNumber.Read(root, "obols");
        foreach (var (field, into) in new[] { ("offerings", inventory.Offerings), ("provisions", inventory.Provisions) })
        {
            var counts = Required(root, field);
            foreach (var entry in counts.EnumerateObject())
            {
                into[entry.Name] = into.GetValueOrDefault(entry.Name) + (int)TbdNumber.Read(counts, entry.Name);
            }
        }
    }

    /// <summary>Regras das oferendas (<c>content/rules/oferendas.json</c>): <c>kinds</c>, <c>capacity</c>, <c>blood</c>.</summary>
    public static OfferingRules OfferingRulesFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var rules = new OfferingRules(
            Required(root, "kinds").EnumerateArray().Select(k => k.GetString()!).ToList(),
            (int)TbdNumber.Read(root, "capacity"),
            String(root, "blood"));
        var errors = rules.Validate().ToList();
        return errors.Count == 0 ? rules : throw new FormatException(string.Join(" ", errors));
    }

    /// <summary>Regras da cura (<c>content/rules/cura.json</c>): <c>hearth_breath</c> e <c>hospitality_breath</c>.</summary>
    public static HealingRules HealingRulesFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var rules = new HealingRules(TbdNumber.Read(doc.RootElement, "hearth_breath"), TbdNumber.Read(doc.RootElement, "hospitality_breath"));
        var errors = rules.Validate().ToList();
        return errors.Count == 0 ? rules : throw new FormatException(string.Join(" ", errors));
    }

    /// <summary>Assistência de gestos do modo (<c>assist</c>), validada contra as janelas do mesmo modo.</summary>
    public static AssistSettings AssistFromDifficulty(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var assist = Required(doc.RootElement, "assist");
        var settings = new AssistSettings(TbdNumber.Read(assist, "window_widening"), TbdNumber.Read(assist, "auto_score"));
        var errors = settings.Validate(GestureData.WindowsFromDifficulty(json)).ToList();
        return errors.Count == 0 ? settings : throw new FormatException(string.Join(" ", errors));
    }

    /// <summary>Quantos degraus a Maré sobe numa derrota (GDD 4.9: 0 Asfódelo, 1 Estige, 2 Tártaro).</summary>
    public static int DefeatTideSteps(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return (int)TbdNumber.Read(doc.RootElement, "defeat_tide_steps");
    }

    /// <summary>Fugir: uma Esquiva com saída (<c>exit</c>, em frações do quadro).</summary>
    public static (DodgeSettings Settings, IDodgePattern Pattern, EscapeExit Exit) EscapeFromJson(string json)
    {
        var (settings, pattern) = GestureData.DodgeFromJson(json);
        using var doc = JsonDocument.Parse(json);
        var e = Required(doc.RootElement, "exit");
        var exit = new EscapeExit(TbdNumber.Read(e, "x"), TbdNumber.Read(e, "y"), TbdNumber.Read(e, "w"), TbdNumber.Read(e, "h"));
        if (exit.X < 0f || exit.Y < 0f || exit.X + exit.Width > 1f || exit.Y + exit.Height > 1f || exit.Width <= 0f || exit.Height <= 0f)
        {
            throw new FormatException("A saída da fuga precisa ficar dentro do quadro.");
        }

        return (settings, pattern, exit);
    }

    private static JsonElement Required(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value : throw new FormatException($"Campo '{property}' ausente.");

    private static string String(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new FormatException($"Campo de texto '{property}' ausente.");
}
