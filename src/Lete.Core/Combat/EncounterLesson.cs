namespace Lete.Core.Combat;

/// <summary>
/// A lição de um encontro do tutorial: o que o jogador ainda precisa fazer (cada coisa uma vez, em qualquer ordem, com
/// qualquer nota: o gesto falho também cumpre a ação, GDD 4.4). Cumprida, a sombra recua.
/// </summary>
public sealed class EncounterLesson
{
    private readonly HashSet<string> _left;

    public EncounterLesson(IEnumerable<string> required) => _left = new HashSet<string>(required, StringComparer.Ordinal);

    public IReadOnlyCollection<string> Left => _left;

    public bool IsComplete => _left.Count == 0;

    /// <summary>O jogador fez <paramref name="action"/>; verdadeiro se ela fazia parte do que faltava.</summary>
    public bool Record(string action) => _left.Remove(action);
}
