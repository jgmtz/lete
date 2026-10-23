namespace Lete.Core.Movement;

/// <summary>
/// Para onde o sprite olha. Só nordeste e sudeste são desenhados;
/// noroeste e sudoeste saem por espelhamento (ARTE.md, 2.3).
/// </summary>
public enum Facing
{
    SouthEast,
    SouthWest,
    NorthEast,
    NorthWest,
}
