namespace Lete.Core.Combat;

/// <summary>
/// Ordem de quem age (placeholder do M1: só alterna entre os participantes; a máquina de turnos vem depois).
/// </summary>
public sealed class TurnOrder
{
    private readonly IReadOnlyList<string> _ids;

    public TurnOrder(IReadOnlyList<string> ids)
    {
        if (ids.Count == 0)
        {
            throw new ArgumentException("A ordem de turno precisa de pelo menos um participante.", nameof(ids));
        }

        _ids = ids;
    }

    public int Index { get; private set; }

    public string Current => _ids[Index];

    public void Advance() => Index = (Index + 1) % _ids.Count;
}
