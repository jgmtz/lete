using System.Text.Json;
using Lete.Core.Combat;
using Lete.Core.Combat.Gestures;

namespace Lete.Core.Data;

/// <summary>Bolsa de teste (<c>content/battle/bolsa_*.json</c>) e as regras do modo de dificuldade usadas no combate.</summary>
public static class BagData
{
    /// <summary>Campos <c>obols</c>, <c>offerings</c> (tipo → quantidade) e <c>provisions</c>.</summary>
    public static BattleBag FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var offerings = Required(root, "offerings").EnumerateObject()
            .ToDictionary(o => o.Name, o => (int)TbdNumber.Read(root.GetProperty("offerings"), o.Name));

        var provisions = new List<ProvisionDefinition>();
        var counts = new Dictionary<string, int>();
        foreach (var p in Required(root, "provisions").EnumerateArray())
        {
            var definition = new ProvisionDefinition(
                String(p, "id"),
                String(p, "name_key"),
                String(p, "description_key"),
                TbdNumber.Read(p, "breath_restore"),
                (int)TbdNumber.Read(p, "mark_gain"));
            provisions.Add(definition);
            counts[definition.Id] = (int)TbdNumber.Read(p, "count");
        }

        return new BattleBag((int)TbdNumber.Read(root, "obols"), offerings, provisions, counts);
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
