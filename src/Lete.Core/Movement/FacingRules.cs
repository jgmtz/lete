namespace Lete.Core.Movement;

public static class FacingRules
{
    /// <summary>
    /// Escolhe a direção a partir da entrada em coordenadas de tela (x para a direita, y para baixo).
    /// Um eixo zerado mantém o lado atual daquele eixo, então andar só para cima não vira o personagem.
    /// </summary>
    public static Facing Resolve(float x, float y, Facing current)
    {
        var north = y < 0 || (y == 0 && IsBackView(current));
        var east = x > 0 || (x == 0 && !IsMirrored(current));

        return (north, east) switch
        {
            (true, true) => Facing.NorthEast,
            (true, false) => Facing.NorthWest,
            (false, true) => Facing.SouthEast,
            (false, false) => Facing.SouthWest,
        };
    }

    /// <summary>Vista de costas (o quadro desenhado de nordeste).</summary>
    public static bool IsBackView(Facing facing) => facing is Facing.NorthEast or Facing.NorthWest;

    /// <summary>Usa o quadro desenhado virado horizontalmente.</summary>
    public static bool IsMirrored(Facing facing) => facing is Facing.SouthWest or Facing.NorthWest;
}
