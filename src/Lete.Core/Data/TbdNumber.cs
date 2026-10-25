using System.Text.Json;

namespace Lete.Core.Data;

/// <summary>
/// Lê um número dos arquivos de conteúdo, que pode vir puro (<c>0.5</c>) ou como placeholder de
/// balanceamento ainda não decidido (<c>{ "_tbd": true, "value": 0.5 }</c>, ver DADOS.md e PENDENCIAS.md).
/// </summary>
public static class TbdNumber
{
    public static float Read(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            throw new FormatException($"Campo '{property}' ausente.");
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.GetSingle();
        }

        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("value", out var inner) &&
            inner.ValueKind == JsonValueKind.Number)
        {
            return inner.GetSingle();
        }

        throw new FormatException($"Campo '{property}' precisa ser um número ou {{ \"_tbd\": true, \"value\": <número> }}.");
    }
}
