namespace Lete.Core.State;

/// <summary>
/// Uma origem do protagonista-sombra (<c>content/characters/origens.json</c>, decisão de 26/out/2026): o que aparece
/// na carta da escolha (símbolo, nome, ofício, ambição, quem foi), os companheiros e os espíritos nomeados ligados a ela
/// e um fragmento por pessoa ligada. A origem não muda o sprite nem dá poder; as ligações dão só história.
/// </summary>
public sealed record Origin(
    string Id,
    string NameKey,
    string CraftKey,
    string AmbitionKey,
    string WhoKey,
    string Symbol,
    IReadOnlyList<string> Companions,
    IReadOnlyList<string> Spirits,
    IReadOnlyList<OriginFragment> Fragments);

/// <summary>Um fragmento de origem: o id (<c>&lt;origem&gt;_&lt;pessoa&gt;</c>), com quem se ganha e o texto do Diário.</summary>
public sealed record OriginFragment(string Id, string With, string TextKey);
