namespace Lete.Core.State;

/// <summary>
/// Uma origem do protagonista-sombra (<c>content/characters/origens.json</c>, decisão de 26/out/2026): o que aparece
/// na carta da escolha (símbolo, nome, ofício, ambição, quem foi) e os companheiros ligados a ela. A origem não muda o
/// sprite nem dá poder; o vínculo com os companheiros dá só história (fragmentos de origem).
/// </summary>
public sealed record Origin(
    string Id,
    string NameKey,
    string CraftKey,
    string AmbitionKey,
    string WhoKey,
    string Symbol,
    IReadOnlyList<string> Companions);
