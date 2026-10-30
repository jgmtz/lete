using Lete.Core.Items;

namespace Lete.Core.State;

/// <summary>Os itens no <see cref="WorldState"/>: o que o jogador tem (id → índice do estágio), o equipado e os conjuntos reunidos.</summary>
public sealed class ItemsState
{
    public Dictionary<string, int> Owned { get; } = new(StringComparer.Ordinal);

    public Dictionary<EquipSlot, string> Equipped { get; } = new();

    public HashSet<string> CompletedSets { get; } = new(StringComparer.Ordinal);
}
