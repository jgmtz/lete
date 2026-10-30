namespace Lete.Core.State;

/// <summary>
/// O rosto no fim do jogo pela regra C (<c>docs/PROTAGONISTA_SOMBRA.md</c> 7): devolvido (os traços verdadeiros) ou
/// coberto pela romã. <see cref="None"/> até ser calculado.
/// </summary>
public enum FinalFace
{
    None,
    Returned,
    Covered,
}
