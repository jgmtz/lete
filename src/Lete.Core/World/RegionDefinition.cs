namespace Lete.Core.World;

/// <summary>
/// Uma região do Além (<c>content/regions/&lt;id&gt;.json</c>): o nome, a cena do Godot que a desenha e o ponto de
/// chegada padrão (um marcador da cena), usado quando ninguém diz por onde o jogador entrou.
/// </summary>
public sealed record RegionDefinition(string Id, string NameKey, string Scene, string Entry);
