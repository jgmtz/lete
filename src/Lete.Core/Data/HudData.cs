using System.Text.Json;
using Lete.Core.Combat;

namespace Lete.Core.Data;

/// <summary>Carrega os layouts do HUD de combate (<c>content/battle/hud_layouts.json</c>).</summary>
public static class HudData
{
    public static HudLayoutSet FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var set = new HudLayoutSet(
            Rect(Required(root, "gesture_area")),
            Required(root, "layouts").EnumerateArray()
                .Select(l => new HudLayout(
                    Required(l, "id").GetString() ?? throw new FormatException("Layout sem id."),
                    Rect(Required(l, "menu")),
                    Rect(Required(l, "description"))))
                .ToList());

        var errors = set.Validate();
        if (errors.Count > 0)
        {
            throw new FormatException(string.Join(" ", errors));
        }

        return set;
    }

    private static ScreenRect Rect(JsonElement e) =>
        new(TbdNumber.Read(e, "x"), TbdNumber.Read(e, "y"), TbdNumber.Read(e, "w"), TbdNumber.Read(e, "h"));

    private static JsonElement Required(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value : throw new FormatException($"Campo '{property}' ausente.");
}
