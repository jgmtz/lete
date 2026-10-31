using System.Text.RegularExpressions;

namespace Lete.Core.Localization;

/// <summary>
/// Confere os arquivos de idioma (ARQUITETURA 7): toda chave entre chaves é ou um lugar de valor (<c>{0}</c>, do
/// <c>string.Format</c>) ou uma marcação de gênero com exatamente duas formas (<c>{o|a}</c>), sem chaves soltas nem
/// aninhadas. Um idioma traduzido precisa ter as mesmas chaves do padrão, com os mesmos lugares de valor.
/// </summary>
public static partial class LocaleValidator
{
    [GeneratedRegex(@"\{([^{}]*)\}")]
    private static partial Regex Braces();

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex Placeholder();

    /// <summary>Problemas de um idioma: <c>chave: mensagem</c>.</summary>
    public static IEnumerable<string> Validate(IReadOnlyDictionary<string, string> strings)
    {
        foreach (var (key, text) in strings.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            foreach (var problem in Problems(text))
            {
                yield return $"{key}: {problem}";
            }
        }
    }

    /// <summary>O que está errado num texto (vazio se nada).</summary>
    public static IEnumerable<string> Problems(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return "texto vazio";
            yield break;
        }

        var rest = Braces().Replace(text, "");
        if (rest.Contains('{') || rest.Contains('}'))
        {
            yield return "chave '{' ou '}' solta ou aninhada";
        }

        foreach (Match match in Braces().Matches(text))
        {
            var inside = match.Groups[1].Value;
            if (Placeholder().IsMatch(inside))
            {
                continue;
            }

            if (inside.Count(c => c == '|') != 1)
            {
                yield return $"'{match.Value}' não é lugar de valor ({{0}}) nem marcação de gênero ({{o|a}})";
            }
        }
    }

    /// <summary>Lugares de valor (<c>{0}</c>, <c>{1}</c>...) de um texto, em ordem.</summary>
    public static IReadOnlyList<int> Placeholders(string text) => Braces().Matches(text)
        .Select(m => m.Groups[1].Value)
        .Where(v => Placeholder().IsMatch(v))
        .Select(int.Parse)
        .Distinct()
        .OrderBy(i => i)
        .ToList();

    /// <summary>
    /// Um idioma traduzido contra o padrão: chaves que faltam, que sobram e textos com lugares de valor diferentes.
    /// </summary>
    public static IEnumerable<string> Compare(IReadOnlyDictionary<string, string> standard, IReadOnlyDictionary<string, string> translated)
    {
        foreach (var key in standard.Keys.Where(k => !translated.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal))
        {
            yield return $"{key}: falta a tradução";
        }

        foreach (var key in translated.Keys.Where(k => !standard.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal))
        {
            yield return $"{key}: chave que o idioma padrão não tem";
        }

        foreach (var (key, text) in translated.Where(p => standard.ContainsKey(p.Key)).OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (!Placeholders(standard[key]).SequenceEqual(Placeholders(text)))
            {
                yield return $"{key}: lugares de valor diferentes do padrão";
            }
        }
    }
}
