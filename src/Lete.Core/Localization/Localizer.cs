using System.Text.Json;
using Lete.Core.State;

namespace Lete.Core.Localization;

/// <summary>
/// Tabela de textos de um idioma (chave → texto). Nenhum texto fica fixo no código.
/// Chave ausente aparece como <c>[chave]</c>, para ser vista e corrigida em desenvolvimento.
/// </summary>
public sealed class Localizer
{
    private readonly IReadOnlyDictionary<string, string> _strings;

    public Localizer(IReadOnlyDictionary<string, string> strings) => _strings = strings;

    public static Localizer FromJson(string json)
    {
        var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? throw new FormatException("Arquivo de idioma vazio.");
        return new Localizer(strings);
    }

    public bool Has(string key) => _strings.ContainsKey(key);

    public string Get(string key, Gender gender) =>
        _strings.TryGetValue(key, out var text) ? GenderMarkup.Resolve(text, gender) : $"[{key}]";
}
