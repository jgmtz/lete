using System;

namespace Lete.Game.Combat.Gestures;

/// <summary>
/// Interface comum dos cinco gestos (ARQUITETURA.md 5): cada um começa com <see cref="Begin"/> e
/// termina publicando o valor de 0 a 1 (proximidade do ideal). Quem chama transforma o valor em
/// perfeito/bom/falho com os limiares do modo de dificuldade.
/// </summary>
public interface IGesture
{
    event Action<float>? Completed;

    bool IsRunning { get; }

    void Begin();
}
