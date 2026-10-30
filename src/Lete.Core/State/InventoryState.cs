namespace Lete.Core.State;

/// <summary>A Bolsa no <see cref="WorldState"/>: óbolos, oferendas por tipo e provisões por id.</summary>
public sealed class InventoryState
{
    public int Obols { get; set; }

    public Dictionary<string, int> Offerings { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> Provisions { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Fôlego e Lucidez entre as lutas (GDD 4.8: só voltam nas fontes de cura; decisão do João, 30/out/2026). Guarda o
/// quanto falta para o máximo, não o valor: o máximo muda com a Leveza e os juramentos, e zero é "cheio".
/// </summary>
public sealed class VitalsState
{
    public float BreathSpent { get; set; }

    public float LuciditySpent { get; set; }

    /// <summary>Fontes de cura limitadas (Fogo do Lar, hospitalidade) → degrau da Maré em que foram usadas.</summary>
    public Dictionary<string, int> HealUses { get; } = new(StringComparer.Ordinal);
}
