namespace Lete.Core.State;

/// <summary>
/// Estado único e serializável do mundo (ARQUITETURA.md, 4.1).
/// No M0 só tem o mínimo; os demais campos entram no M2 junto com o save.
/// </summary>
public sealed class WorldState
{
    public Gender Gender { get; set; } = Gender.Masculine;

    public HashSet<string> Flags { get; } = new(StringComparer.Ordinal);

    /// <summary>Entradas do Diário, como chaves de localização, na ordem em que foram escritas.</summary>
    public List<string> JournalEntries { get; } = new();
}
