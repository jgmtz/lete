using System.Text.RegularExpressions;
using Lete.Core.State;

namespace Lete.Core.Localization;

/// <summary>
/// Resolve a marcação de gênero <c>{masculino|feminino}</c>, por exemplo <c>cansad{o|a}</c>
/// (ARQUITETURA.md, seção 7).
/// </summary>
public static partial class GenderMarkup
{
    [GeneratedRegex(@"\{([^{}|]*)\|([^{}|]*)\}")]
    private static partial Regex Pattern();

    public static string Resolve(string text, Gender gender) =>
        Pattern().Replace(text, m => gender == Gender.Feminine ? m.Groups[2].Value : m.Groups[1].Value);
}
