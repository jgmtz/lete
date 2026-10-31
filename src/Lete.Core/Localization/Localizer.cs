using System.Text.Json;
using Lete.Core.State;

namespace Lete.Core.Localization;

/// <summary>
/// Tabela de textos de um idioma (chave → texto). Nenhum texto fica fixo no código.
/// Um idioma traduzido pode ter um <paramref name="fallback"/> (o padrão, pt_BR): a chave que faltar nele vem de lá.
/// Chave ausente em todos aparece como <c>[chave]</c>, para ser vista e corrigida em desenvolvimento.
/// </summary>
public sealed class Localizer
{
    private readonly IReadOnlyDictionary<string, string> _strings;
    private readonly Localizer? _fallback;

    public Localizer(IReadOnlyDictionary<string, string> strings, Localizer? fallback = null)
    {
        _strings = strings;
        _fallback = fallback;
    }

    public static Localizer FromJson(string json, Localizer? fallback = null) => new(Read(json), fallback);

    /// <summary>Lê um arquivo de idioma (objeto plano chave → texto).</summary>
    public static IReadOnlyDictionary<string, string> Read(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? throw new FormatException("Arquivo de idioma vazio.");

    public IReadOnlyDictionary<string, string> Strings => _strings;

    public bool Has(string key) => _strings.ContainsKey(key) || (_fallback?.Has(key) ?? false);

    public IEnumerable<string> Keys => _fallback is null ? _strings.Keys : _strings.Keys.Union(_fallback.Keys);

    public string Get(string key, Gender gender) =>
        _strings.TryGetValue(key, out var text) ? GenderMarkup.Resolve(text, gender)
        : _fallback is not null ? _fallback.Get(key, gender)
        : $"[{key}]";
}
