namespace Lete.Core.Combat;

/// <summary>
/// O quanto o jogador sabe de uma sombra (GDD 4.2): o nome e a fixação. Aparece como 3 óbolos que se
/// enchem: um pelo nome, um pela fixação e o terceiro quando os dois são conhecidos. Escutar e Oferenda revelam.
/// </summary>
public sealed class ShadeUnderstanding
{
    public const int MaxPoints = 3;

    public bool NameRevealed { get; private set; }
    public bool FixationRevealed { get; private set; }

    public int Points => (NameRevealed ? 1 : 0) + (FixationRevealed ? 1 : 0) + (IsComplete ? 1 : 0);

    /// <summary>Compreensão completa abre a via Libertar (GDD 4.5).</summary>
    public bool IsComplete => NameRevealed && FixationRevealed;

    public void RevealName() => NameRevealed = true;

    public void RevealFixation() => FixationRevealed = true;
}
