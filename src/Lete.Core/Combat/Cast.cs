namespace Lete.Core.Combat;

/// <summary>Papel de alguém do elenco.</summary>
public enum CastKind
{
    Protagonist,
    Companion,
    Shade,
}

/// <summary>
/// Uma pessoa do elenco, num lugar só (<c>content/characters/elenco.json</c>): nome, papel, gênero, altura no mundo,
/// arte de batalha de frente e de costas (nome do arquivo, sem pasta nem extensão), retrato, pasta dos sprites de
/// mapa, tinta (sombras) e a animação parada desenhada. Batalhas e telas citam só o <see cref="Id"/>.
/// <paramref name="Selectable"/>: aparece na escolha de personagem (provisório até os 6 protagonistas existirem).
/// <paramref name="FrontFacesRight"/>: a arte de frente olha para a direita (o normal do 3/4 é olhar para a esquerda);
/// a escolha pelo reflexo espelha quem precisa para que todos olhem para o mesmo lado.
/// </summary>
public sealed record CastMember(
    string Id,
    string NameKey,
    CastKind Kind,
    string? Gender,
    float Height,
    string? FrontArt,
    string? BackArt,
    string? Portrait,
    string? MapFolder,
    string? Tint,
    IdleAnimation? Idle,
    bool Selectable,
    bool FrontFacesRight = false)
{
    /// <summary>A arte de batalha para a pose pedida.</summary>
    public string ArtFor(bool back) =>
        (back ? BackArt : FrontArt) ?? throw new InvalidOperationException($"'{Id}' não tem arte de batalha de {(back ? "costas" : "frente")}.");

    /// <summary>Como participante de batalha, na pose pedida.</summary>
    public BattleActorArt AsActor(bool back) =>
        new(Id, ArtFor(back), Height, FlipH: false, Tint, Portrait, Kind == CastKind.Shade ? NameKey : null, Idle);
}
