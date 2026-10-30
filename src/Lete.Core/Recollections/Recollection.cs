using Lete.Core.Combat;
using Lete.Core.Conditions;

namespace Lete.Core.Recollections;

/// <summary>Estágio de uma Lembrança no Mosaico (GDD 2.1), na ordem em que avança.</summary>
public enum RecollectionStage
{
    Fragment,
    Recalled,
    Full,
}

/// <summary>Confiança de uma Lembrança: confirmada, incerta (só fontes não confiáveis) ou falsa (contradita).</summary>
public enum RecollectionCertainty
{
    Certain,
    Uncertain,
    False,
}

/// <summary>A quem a Lembrança pertence: comum a todos, de uma origem, ou Pesada (fila fixa, GDD 2.2).</summary>
public enum RecollectionScope
{
    Common,
    Origin,
    Heavy,
}

/// <summary>Como uma fonte chega ao jogador (GDD 2.1, "Como obter").</summary>
public enum SourceType
{
    /// <summary>Libertar a sombra <see cref="RecollectionSource.Shade"/> (vem pelo evento <c>ShadeFreed</c>).</summary>
    FreeShade,

    /// <summary>Partilhar oferenda com a sombra (vem pelo evento <c>OfferingShared</c>).</summary>
    SharedOffering,

    /// <summary>Lugar-âncora, sonho, Mnemósine, fala: quem conduz a cena chama <c>Mosaic.Hear</c>.</summary>
    Anchor,
    Dream,
    Mnemosyne,
    Dialogue,
}

/// <summary>
/// Uma fonte que conta o episódio. <see cref="Reliable"/> falso: sombra com fixação, a paz de Elísios, sonhos
/// (decisão do João, 29/out/2026: a incerteza vem da fonte). <see cref="Contradicts"/>: conta outra versão; se a
/// Lembrança ainda for incerta, ela vira falsa.
/// </summary>
public sealed record RecollectionSource(
    string Id,
    SourceType Type,
    string? Shade,
    string? OfferingKind,
    bool Reliable,
    bool Contradicts);

/// <summary>
/// Uma Lembrança do conteúdo (<c>content/recollections/&lt;id&gt;.json</c>, DADOS.md 3). <see cref="Skill"/>: a Memória
/// (habilidade de combate, <c>content/memories</c>) que ela dá, com 3 níveis pelo estágio (GDD 2.1). <see cref="FullBy"/>:
/// atos (no lugar-âncora, com item ou oferenda, ou missão) que levam a Recordação à Memória plena. Só a Pesada tem
/// <see cref="Heavy"/>; nela, <see cref="Skill"/> é a Culpa, dada só ao Encarar.
/// </summary>
public sealed record Recollection(
    string Id,
    RecollectionKind Kind,
    RecollectionScope Scope,
    string? OriginId,
    string TitleKey,
    IReadOnlyDictionary<RecollectionStage, string> StageTextKeys,
    string? Skill,
    IReadOnlyList<RecollectionSource> Sources,
    IReadOnlyList<string> FullBy,
    int MosaicX,
    int MosaicY,
    HeavyMemory? Heavy = null);

/// <summary>
/// O que só a Memória Pesada tem (GDD 2.2): o lugar na fila fixa (<see cref="Order"/>, de 1 em diante), a condição
/// para aflorar, os textos da cena (o aviso de Hécate ou do companheiro, o aflorar, a fala ao Deixar; a verdade é o
/// texto do estágio pleno) e a Leveza que Deixar dá.
/// </summary>
public sealed record HeavyMemory(int Order, Condition? Gate, string WarningKey, string SurfaceKey, string LeaveKey, Lightness Lightness);

/// <summary>
/// Leveza (GDD 2.2): Lucidez máxima a mais e proteções contra ser esquecido (ao zerar a Lucidez, gasta uma proteção
/// em vez de esquecer a Memória; renovam na Trívia; decisão do João, 30/out/2026).
/// </summary>
public sealed record Lightness(float LucidityMax, int Protections)
{
    public static readonly Lightness None = new(0f, 0);

    public Lightness Plus(Lightness other) => new(LucidityMax + other.LucidityMax, Protections + other.Protections);
}
