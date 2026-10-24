using System.Numerics;

namespace Lete.Core.Movement;

public static class WalkRules
{
    /// <summary>
    /// Limita a entrada (coordenadas de tela, y para baixo) a comprimento 1,
    /// para que a diagonal não ande mais rápido que os eixos. Analógico parcial fica como está.
    /// </summary>
    public static Vector2 LimitLength(Vector2 input) =>
        input.LengthSquared() > 1f ? Vector2.Normalize(input) : input;

    /// <summary>
    /// Escolhe a vista pelo eixo dominante da entrada. Na diagonal exata (teclado)
    /// vale a horizontal; sem entrada, mantém a direção atual (o parado olha para onde andava).
    /// </summary>
    public static ScreenDirection Resolve(Vector2 input, ScreenDirection current)
    {
        if (input == Vector2.Zero)
        {
            return current;
        }

        if (MathF.Abs(input.X) >= MathF.Abs(input.Y))
        {
            return input.X > 0 ? ScreenDirection.Right : ScreenDirection.Left;
        }

        return input.Y > 0 ? ScreenDirection.Down : ScreenDirection.Up;
    }
}
