using System.Text.Json;
using Lete.Core.Presentation;
using Lete.Core.Systems;

namespace Lete.Core.Data;

/// <summary>Carrega os números da Marca (<c>content/rules/marca.json</c>).</summary>
public static class MarkData
{
    public static MarkRules FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var rules = new MarkRules(
            (int)TbdNumber.Read(root, "mid_from"),
            (int)TbdNumber.Read(root, "high_from"),
            (int)TbdNumber.Read(root, "faced_to_return_face"),
            (int)TbdNumber.Read(root, "perjury"));

        var errors = rules.Validate().ToList();
        return errors.Count == 0 ? rules : throw new FormatException(string.Join(" ", errors));
    }

    /// <summary>A perda de cor do sprite por faixa, do mesmo arquivo.</summary>
    public static MarkLook LookFromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var look = new MarkLook(TbdNumber.Read(root, "desaturation_mid"), TbdNumber.Read(root, "desaturation_high"));

        var errors = look.Validate().ToList();
        return errors.Count == 0 ? look : throw new FormatException(string.Join(" ", errors));
    }
}
