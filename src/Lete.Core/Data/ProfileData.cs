using System.Text.Json;
using Lete.Core.Systems;

namespace Lete.Core.Data;

/// <summary>Carrega os números do Perfil (<c>content/rules/perfil.json</c>).</summary>
public static class ProfileData
{
    public static ProfileRules FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var rules = new ProfileRules(
            (int)TbdNumber.Read(root, "recent_window"),
            (int)TbdNumber.Read(root, "min_encounters"),
            (int)TbdNumber.Read(root, "min_memory_uses"));

        var errors = rules.Validate().ToList();
        return errors.Count == 0 ? rules : throw new FormatException(string.Join(" ", errors));
    }
}
