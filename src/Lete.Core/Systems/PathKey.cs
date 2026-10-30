using Lete.Core.Conditions;

namespace Lete.Core.Systems;

/// <summary>
/// Uma chave do Caminho (GDD 2.4, <c>content/path/&lt;id&gt;.json</c>): uma por passagem (rio ou ponto do Caminho),
/// com várias formas de obter, cada uma uma condição (decisão do João, 30/out/2026). <see cref="JournalKey"/>: a
/// frase que entra no Diário quando o jogador a obtém.
/// </summary>
public sealed record PathKey(string Id, string JournalKey, IReadOnlyList<PathWay> Ways);

/// <summary>Uma forma de obter a chave (óbolo, favor de Hermes, origem Pescador...).</summary>
public sealed record PathWay(string Id, Condition Condition);
