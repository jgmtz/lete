using Lete.Core.Combat;
using Lete.Core.Conditions;
using Lete.Core.Events;

namespace Lete.Core.Items;

/// <summary>Categorias de item (GDD 6.1). As relíquias não se equipam: ficam no alforje.</summary>
public enum ItemCategory
{
    Instrument,
    Garment,
    Amulet,
    Votive,
    Utensil,
    Relic,
}

/// <summary>Espaços de equipamento (GDD 6.1): instrumento, veste, amuleto e dois ex-votos.</summary>
public enum EquipSlot
{
    Instrument,
    Garment,
    Amulet,
    Votive1,
    Votive2,
}

/// <summary>
/// Um item único (<c>content/items/&lt;id&gt;.json</c> com <c>category</c>; GDD 6): dono e história, Afinidade (um tipo
/// de Lembrança), estágios Gasto → Reconhecido → Desperto e, se fizer parte, o conjunto do dono. A relíquia traz a
/// chave do Caminho que abre (<see cref="PathKey"/>, só informativa: a chave cita o item numa forma <c>item_owned</c>).
/// </summary>
public sealed record Item(
    string Id,
    ItemCategory Category,
    string NameKey,
    string StoryKey,
    RecollectionKind Affinity,
    string? SetId,
    IReadOnlyList<ItemStageDefinition> Stages,
    string? PathKey);

/// <summary>
/// Um estágio do item: os modificadores que valem nele, a condição para chegar nele (o primeiro não tem) e a linha de
/// história que entra no Diário quando o item evolui (decisão do João, 30/out/2026: evolui sozinho quando a condição vale).
/// </summary>
public sealed record ItemStageDefinition(ItemStage Stage, Condition? When, string? StoryKey, ItemModifiers Modifiers);

/// <summary>
/// Conjunto de dono (GDD 6.2): peças que eram da mesma sombra. Reunir todas (ter todas) conta a história inteira
/// (<see cref="StoryKey"/> no Diário) e ativa <see cref="Modifiers"/>.
/// </summary>
public sealed record ItemSet(string Id, string NameKey, string StoryKey, IReadOnlyList<string> Pieces, ItemModifiers Modifiers);

/// <summary>
/// Modificadores declarativos (DADOS.md 4), somados por todo o equipamento, conjuntos completos e relíquias. Por enquanto
/// só os simples (decisão do João, 30/out/2026): janela do aparo, quadro da Esquiva, Fôlego e Lucidez máximos. Os
/// efeitos especiais (anel duplo do remo, névoa) entram depois.
/// </summary>
public sealed record ItemModifiers(float ParryWindow, float DodgeBox, float BreathMax, float LucidityMax)
{
    public static readonly ItemModifiers None = new(0f, 0f, 0f, 0f);

    public ItemModifiers Plus(ItemModifiers other) =>
        new(ParryWindow + other.ParryWindow, DodgeBox + other.DodgeBox, BreathMax + other.BreathMax, LucidityMax + other.LucidityMax);
}
